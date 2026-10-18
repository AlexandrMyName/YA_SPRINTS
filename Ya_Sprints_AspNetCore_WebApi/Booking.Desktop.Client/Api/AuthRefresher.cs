using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sprints.Desktop.Api;

/// <summary>
/// Singleton-координатор refresh-токенов. 
/// Гарантирует, что при параллельных 401 будет выполнен ТОЛЬКО ОДИН запрос /auth/refresh.
/// Остальные ждут и получают уже готовый новый токен.
/// </summary>
public sealed class AuthRefresher
{

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IAuthApi _authApi;
    private readonly ITokenStore _store;
    private readonly ILogger<AuthRefresher> _logger;


    public AuthRefresher(IAuthApi authApi, ITokenStore store, ILogger<AuthRefresher> logger)
    {
        _authApi = authApi;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Обновляет access-токен.
    /// </summary>
    /// <param name="staleAccessToken">Токен, с которым был сделан упавший запрос.
    /// Если в хранилище уже другой — значит кто-то успел обновить, просто вернём его.</param>
    public async Task<TokenData?> RefreshAsync(string staleAccessToken, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var current = await _store.LoadAsync(ct);
            if (current == null) return null;

            // Кто-то уже обновил, пока мы ждали
            if (!string.IsNullOrEmpty(current.AccessToken) &&
                current.AccessToken != staleAccessToken)
            {
                _logger.LogDebug("Token already refreshed by another request");
                return current;
            }

            if (string.IsNullOrEmpty(current.RefreshToken))
            {
                _logger.LogWarning("No refresh token, cannot refresh");
                await _store.ClearAsync(ct);
                return null;
            }

            try
            {
                var response = await _authApi.RefreshAsync(
                    new RefreshRequest { RefreshToken = current.RefreshToken }, ct);

                var updated = new TokenData
                {
                    UserId = current.UserId,
                    AccessToken = response.AccessToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresAtUtc = response.ExpiresAtUtc
                };
                await _store.SaveAsync(updated, ct);

                _logger.LogInformation(
                    "Token refreshed successfully, new expiration: {ExpiresAt}",
                    response.ExpiresAtUtc);
                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token failed, clearing store");
                await _store.ClearAsync(ct);
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}