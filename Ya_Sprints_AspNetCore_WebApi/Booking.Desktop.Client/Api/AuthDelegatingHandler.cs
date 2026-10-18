using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sprints.Desktop.Api;

/// <summary>
/// HTTP-handler, который:
///   1. Прикладывает access-токен к каждому запросу.
///   2. При 401 → пытается обновить токен через AuthRefresher.
///   3. Повторяет исходный запрос с новым токеном (один раз).
/// 
/// AuthRefresher — Singleton с SemaphoreSlim, поэтому параллельные 401
/// не приведут к гонке refresh-ов.
/// </summary>
public sealed class AuthDelegatingHandler : DelegatingHandler
{

    private readonly ITokenStore _store;
    private readonly AuthRefresher _refresher;
    private readonly ILogger<AuthDelegatingHandler> _logger;


    public AuthDelegatingHandler(
        ITokenStore store,
        AuthRefresher refresher,
        ILogger<AuthDelegatingHandler> logger)
    {
        _store = store;
        _refresher = refresher;
        _logger = logger;
    }


    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {

        var tokens = await _store.LoadAsync(ct);
        var usedAccessToken = tokens?.AccessToken;

        if (!string.IsNullOrEmpty(usedAccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", usedAccessToken);
        }

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized || string.IsNullOrEmpty(usedAccessToken))
        {
            return response;
        }

        _logger.LogDebug("401 for {Method} {Uri}, attempting refresh",
            request.Method, request.RequestUri);

        var refreshed = await _refresher.RefreshAsync(usedAccessToken, ct);
        if (refreshed == null)
        {
            _logger.LogWarning("Refresh failed; returning 401 to caller");
            return response;
        }

        // HttpRequestMessage нельзя переотправить — клонируем
        var clone = await CloneAsync(request, ct);
        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);

        response.Dispose();
        return await base.SendAsync(clone, ct);
    }


    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original, CancellationToken ct)
    {

        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy
        };

        if (original.Content != null)
        {
            var buffer = new MemoryStream();
            await original.Content.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            clone.Content = new StreamContent(buffer);

            foreach (var h in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        foreach (var h in original.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        foreach (var opt in original.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(opt.Key), opt.Value);

        return clone;
    }
}