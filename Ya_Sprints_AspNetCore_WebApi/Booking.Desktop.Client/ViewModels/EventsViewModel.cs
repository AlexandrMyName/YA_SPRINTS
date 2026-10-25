using Avalonia.Styling;
using Booking.Desktop.Client.Services;
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

public partial class EventsViewModel : ObservableObject
{
    private readonly IEventsApi _api;
    private readonly IDialogService _dialogs;
    private readonly ILogger<EventsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<EventCardVm> _items = new();
    [ObservableProperty] private EventCardVm? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _isAdmin;

    public string CountLabel => $"{Items.Count} событий";
    public bool HasSelection => Selected != null;

    public EventsViewModel(IEventsApi api, IDialogService dialogs, ILogger<EventsViewModel> logger)
    {
        _api = api; _dialogs = dialogs; _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true; Error = null;
        try
        {
            var paged = await _api.GetEventsAsync(1, 100);
            Items = new ObservableCollection<EventCardVm>(paged.Items.Select(x => new EventCardVm(x)));
            Selected = null;
            OnPropertyChanged(nameof(CountLabel));
            OnPropertyChanged(nameof(HasSelection));
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Load events"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var result = await _dialogs.ShowEventDialogAsync(null);
        if (result == null) return;

        try
        {
            await _api.CreateEventAsync(new CreateEventDto
            {
                Id = Guid.Empty,
                Title = result.Title,
                Description = result.Description,
                StartAt = result.StartAt,
                EndAt = result.EndAt,
                TotalSeats = result.TotalSeats
            });
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Create event"); }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (Selected == null) return;

        var existing = Selected.Source;
        var result = await _dialogs.ShowEventDialogAsync(existing);
        if (result == null) return;

        try
        {
            await _api.UpdateEventAsync(existing.Id, new EventInfoDto
            {
                Id = existing.Id,
                Title = result.Title,
                Description = result.Description,
                StartAt = result.StartAt,
                EndAt = result.EndAt,
                TotalSeats = result.TotalSeats,
                AvailableSeats = result.AvailableSeats
            });
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Update event"); }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Selected == null) return;

        var ok = await _dialogs.ConfirmAsync(
            "Удалить событие?",
            $"«{Selected.Title}» будет удалено. Действие необратимо.");
        if (!ok) return;

        try
        {
            await _api.DeleteEventAsync(Selected.Source.Id);
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Delete event"); }
    }

    public void SelectCard(EventCardVm vm)
    {
        if (Selected != null) Selected.IsSelected = false;
        Selected = vm;
        vm.IsSelected = true;
        OnPropertyChanged(nameof(HasSelection));
    }
}