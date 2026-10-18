using Avalonia.Styling;
using Booking.Desktop.Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sprints.Desktop.Api;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sprints.Desktop.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUsersApi _api;
    private readonly ILogger<UsersViewModel> _logger;

    [ObservableProperty] private ObservableCollection<UserCardVm> _items = new();
    [ObservableProperty] private UserCardVm? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _isAdmin;

    public string CountLabel => $"{Items.Count} записей";
    public bool HasSelection => Selected != null;

    public UsersViewModel(IUsersApi api, ILogger<UsersViewModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true; Error = null;
        try
        {
            var paged = await _api.GetUsersAsync(1, 100);
            Items = new ObservableCollection<UserCardVm>(paged.Items.Select(x => new UserCardVm(x)));
            Selected = null;
            OnPropertyChanged(nameof(CountLabel));
            OnPropertyChanged(nameof(HasSelection));
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Load users"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PromoteAsync()
    {
        if (Selected == null) return;
        try
        {
            await _api.PromoteAsync(Selected.Login);
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; }
    }

    [RelayCommand]
    private async Task DemoteAsync()
    {
        if (Selected == null) return;
        try
        {
            await _api.DemoteAsync(Selected.Login);
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; }
    }

    [RelayCommand]
    private Task EditAsync() => Task.CompletedTask;  // пользователи не редактируются через UI

    public void SelectCard(UserCardVm vm)
    {
        if (Selected != null) Selected.IsSelected = false;
        Selected = vm;
        vm.IsSelected = true;
        OnPropertyChanged(nameof(HasSelection));
    }
}