using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sprints.Desktop.Api;
using System;
using System.Threading.Tasks;

namespace Sprints.Desktop.ViewModels;

public enum AppTab { Users, Events, Bookings }

public partial class ShellViewModel : ObservableObject
{
    public UsersViewModel Users { get; }
    public EventsViewModel Events { get; }
    public BookingsViewModel Bookings { get; }

    [ObservableProperty] private AppTab _currentTab = AppTab.Events;
    [ObservableProperty] private string _currentUserName = "";
    [ObservableProperty] private string _currentRoleLabel = "";
    [ObservableProperty] private string _statusMessage = "Готово";
    [ObservableProperty] private string _currentVersion = "Sprints Desktop v1.0";

    public bool IsUsersTab => CurrentTab == AppTab.Users;
    public bool IsEventsTab => CurrentTab == AppTab.Events;
    public bool IsBookingsTab => CurrentTab == AppTab.Bookings;

    public object CurrentContent => CurrentTab switch
    {
        AppTab.Users => Users,
        AppTab.Events => Events,
        AppTab.Bookings => Bookings,
        _ => Events
    };

    public event Action? LogoutRequested;

    public ShellViewModel(UsersViewModel users, EventsViewModel events, BookingsViewModel bookings)
    {
        Users = users;
        Events = events;
        Bookings = bookings;
    }

    public void Configure(TokenData tokens)
    {
        CurrentUserName = tokens.Login;
        CurrentRoleLabel = tokens.IsAdmin ? "Admin" : "User";
        Users.IsAdmin = tokens.IsAdmin;

        // Стартовая вкладка — Events
        CurrentTab = AppTab.Events;
        OnTabChanged();
    }

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        await Task.WhenAll(
            Users.LoadAsync(),
            Events.LoadAsync(),
            Bookings.LoadAsync());
    }

    [RelayCommand]
    private async Task SwitchTabAsync(string? tab)
    {
        if (Enum.TryParse<AppTab>(tab, out var t) && t != CurrentTab)
        {
            CurrentTab = t;
            OnTabChanged();
            await Task.CompletedTask;
        }
    }

    partial void OnCurrentTabChanged(AppTab value)
    {
        OnPropertyChanged(nameof(IsUsersTab));
        OnPropertyChanged(nameof(IsEventsTab));
        OnPropertyChanged(nameof(IsBookingsTab));
        OnPropertyChanged(nameof(CurrentContent));
    }

    private void OnTabChanged() => StatusMessage = $"Вкладка: {CurrentTab}";

    // ─── Делегирующие команды — вызывают команду активной вкладки ───



    [RelayCommand]
    private async Task CreateAsync()
    {
        switch (CurrentTab)
        {
            case AppTab.Events: await Events.CreateCommand.ExecuteAsync(null); break;
            case AppTab.Bookings: await Bookings.CreateCommand.ExecuteAsync(null); break;
            case AppTab.Users: StatusMessage = "Пользователей создают через /auth/register"; break;
        }
    }

    [RelayCommand]
    private async Task EditSelectedAsync()
    {
        switch (CurrentTab)
        {
            case AppTab.Events: await Events.EditCommand.ExecuteAsync(null); break;
            case AppTab.Users: await Users.EditCommand.ExecuteAsync(null); break; 
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        switch (CurrentTab)
        {
            case AppTab.Events: await Events.DeleteCommand.ExecuteAsync(null); break;
            case AppTab.Bookings: await Bookings.DeleteCommand.ExecuteAsync(null); break;
            case AppTab.Users: StatusMessage = "Удаление пользователей не поддерживается"; break;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        switch (CurrentTab)
        {
            case AppTab.Users: await Users.LoadCommand.ExecuteAsync(null); break;
            case AppTab.Events: await Events.LoadCommand.ExecuteAsync(null); break;
            case AppTab.Bookings: await Bookings.LoadCommand.ExecuteAsync(null); break;
        }
    }

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke();
}