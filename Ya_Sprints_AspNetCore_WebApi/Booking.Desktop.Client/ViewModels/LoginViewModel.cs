using Booking.Desktop.Client.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sprints.Desktop.Api;
using System;
using System.Threading.Tasks;

namespace Sprints.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthApi _api;
    private readonly ITokenStore _store;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty] private string _login = "admin";
    [ObservableProperty] private string _password = "admin";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;

    public event Action<TokenData>? LoginSucceeded;

    public LoginViewModel(IAuthApi api, ITokenStore store, ILogger<LoginViewModel> logger)
    {
        _api = api;
        _store = store;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var response = await _api.LoginAsync(new LoginRequest
            {
                Login = Login,
                Password = Password
            });

            var data = new TokenData
            {
                UserId = response.Id,
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                ExpiresAtUtc = response.ExpiresAtUtc,
                Login = Login,
                IsAdmin = JwtUtils.IsAdmin(response.AccessToken)    
            };
            await _store.SaveAsync(data);

            _logger.LogInformation("Logged in as {Login}, token expires at {ExpiresAt}",
                Login, response.ExpiresAtUtc);

            LoginSucceeded?.Invoke(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login failed");
            ErrorMessage = ex.Message.Contains("401")
                ? "Неверный логин или пароль"
                : $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}