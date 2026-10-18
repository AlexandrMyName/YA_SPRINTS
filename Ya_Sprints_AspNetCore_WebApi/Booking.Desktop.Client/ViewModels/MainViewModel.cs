using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Desktop.Api;
using System;

namespace Sprints.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ITokenStore _store;

    [ObservableProperty] private ObservableObject _currentView = null!;

    public MainViewModel(IServiceProvider services, ITokenStore store)
    {
        _services = services;
        _store = store;
        ShowLogin();
    }

    private void ShowLogin()
    {
        var vm = _services.GetRequiredService<LoginViewModel>();
        vm.LoginSucceeded += OnLoginSucceeded;
        CurrentView = vm;
    }

    private async void OnLoginSucceeded(TokenData tokens)
    {
        var shell = _services.GetRequiredService<ShellViewModel>();
        shell.Configure(tokens);
        shell.LogoutRequested += OnLogoutRequested;
        CurrentView = shell;

        await shell.LoadAllAsync();
    }

    private async void OnLogoutRequested()
    {
        await _store.ClearAsync();
        ShowLogin();
    }
}