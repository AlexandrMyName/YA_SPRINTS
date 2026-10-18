using Avalonia.Styling;
using Booking.Desktop.Client.Services;
using Booking.Desktop.Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Sprints.Desktop.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sprints.Desktop.ViewModels;

public partial class BookingsViewModel : ObservableObject
{
    private readonly IBookingsApi _api;
    private readonly IEventsApi _eventsApi;
    private readonly IDialogService _dialogs;
    private readonly ILogger<BookingsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<BookingCardVm> _items = new();
    [ObservableProperty] private BookingCardVm? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;

    public string CountLabel => $"{Items.Count} броней";
    public bool HasSelection => Selected != null;

    public BookingsViewModel(
        IBookingsApi api, IEventsApi eventsApi,
        IDialogService dialogs, ILogger<BookingsViewModel> logger)
    {
        _api = api; _eventsApi = eventsApi; _dialogs = dialogs; _logger = logger;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true; Error = null;
        try
        {
            var paged = await _api.GetBookingsAsync(1, 100);
            Items = new ObservableCollection<BookingCardVm>(paged.Items.Select(x => new BookingCardVm(x)));
            Selected = null;
            OnPropertyChanged(nameof(CountLabel));
            OnPropertyChanged(nameof(HasSelection));
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Load bookings"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        // Загружаем список событий для диалога выбора
        List<EventInfoDto> events;
        try
        {
            var paged = await _eventsApi.GetEventsAsync(1, 200);
            events = paged.Items;
        }
        catch (Exception ex)
        {
            Error = "Не удалось загрузить события: " + ex.Message;
            return;
        }

        if (events.Count == 0)
        {
            Error = "Нет доступных событий — создайте событие сначала.";
            return;
        }

        var eventId = await _dialogs.ShowBookingDialogAsync(events);
        if (eventId == null) return;

        try
        {
            await _api.CreateBookingAsync(eventId.Value);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _logger.LogError(ex, "Create booking");
        }
    }

    [RelayCommand]
    private Task EditAsync() => Task.CompletedTask;  // брони не редактируются

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Selected == null) return;

        var ok = await _dialogs.ConfirmAsync(
            "Отменить бронь?",
            $"Бронь {Selected.ShortId} будет отменена.");
        if (!ok) return;

        try
        {
            await _api.CancelBookingAsync(Selected.Source.Id);
            await LoadAsync();
        }
        catch (Exception ex) { Error = ex.Message; _logger.LogError(ex, "Cancel booking"); }
    }

    public void SelectCard(BookingCardVm vm)
    {
        if (Selected != null) Selected.IsSelected = false;
        Selected = vm;
        vm.IsSelected = true;
        OnPropertyChanged(nameof(HasSelection));
    }
}