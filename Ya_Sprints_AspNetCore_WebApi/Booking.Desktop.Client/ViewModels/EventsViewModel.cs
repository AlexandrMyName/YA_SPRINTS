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


namespace Sprints.Desktop.ViewModels;

public partial class EventsViewModel : ObservableObject
{
    private readonly IEventsApi _api;
    private readonly IDialogService _dialogs;
    private readonly ILogger<EventsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<EventCardVm> _items = new();
    [ObservableProperty] private ObservableCollection<EventCardVm> _topItems = new();
    [ObservableProperty] private EventCardVm? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _isAdmin;

    public string CountLabel => $"{Items.Count} событий";
    public bool HasSelection => Selected != null;

    // Computed-свойства для секции топа
    public bool HasTop => TopItems.Count > 0;
    public string TopCountLabel => $"Топ-{TopItems.Count}";

    public EventsViewModel(IEventsApi api, IDialogService dialogs, ILogger<EventsViewModel> logger)
    {
        _api = api; _dialogs = dialogs; _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true; Error = null;

        // Параллельная загрузка: основной список и топ независимы.
        // Каждый метод ловит свои исключения, поэтому Task.WhenAll не бросит.
        await Task.WhenAll(LoadItemsAsync(), LoadTopAsync());

        IsLoading = false;
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            var paged = await _api.GetEventsAsync(1, 100);
            Items = new ObservableCollection<EventCardVm>(paged.Items.Select(x => new EventCardVm(x)));
            Selected = null;
            OnPropertyChanged(nameof(CountLabel));
            OnPropertyChanged(nameof(HasSelection));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _logger.LogError(ex, "Load events");
        }
    }

    private async Task LoadTopAsync()
    {
        // Топ — не критичная часть: ошибки логируем, но Error не выставляем,
        // чтобы основной экран продолжал работать.
        try
        {
            var top = await _api.GetTopEventsAsync(10);
            TopItems = new ObservableCollection<EventCardVm>(top.Select(x => new EventCardVm(x)));
            OnPropertyChanged(nameof(HasTop));
            OnPropertyChanged(nameof(TopCountLabel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load top events");
        }
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