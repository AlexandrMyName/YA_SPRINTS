using Booking.Desktop.Client.Services;
using Booking.Desktop.Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Refit;
using Sprints.Desktop.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

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

    public string CountLabel => $"{Items?.Count ?? 0} броней";
    public bool HasSelection => Selected != null;

    public BookingsViewModel(
        IBookingsApi api, IEventsApi eventsApi,
        IDialogService dialogs, ILogger<BookingsViewModel> logger)
    {
        _api = api;
        _eventsApi = eventsApi;
        _dialogs = dialogs;
        _logger = logger;

        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    partial void OnSelectedChanged(BookingCardVm? value)
        => OnPropertyChanged(nameof(HasSelection));

    partial void OnItemsChanging(ObservableCollection<BookingCardVm>? oldValue, ObservableCollection<BookingCardVm> newValue)
    {
        if (oldValue != null) oldValue.CollectionChanged -= OnItemsCollectionChanged;
    }

    partial void OnItemsChanged(ObservableCollection<BookingCardVm> value)
    {
        if (value != null) value.CollectionChanged += OnItemsCollectionChanged;
        OnPropertyChanged(nameof(CountLabel));
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(CountLabel));

    // ─────────────────────────────────────────────
    // Load
    // ─────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;

        try
        {
            var paged = await _api.GetBookingsAsync(1, 100);
            Items = new ObservableCollection<BookingCardVm>(
                paged.Items.Select(x => new BookingCardVm(x)));
            Selected = null;
        }
        catch (Exception ex)
        {
            Error = "Не удалось загрузить брони: " + ex.Message;
            _logger.LogError(ex, "Load bookings failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─────────────────────────────────────────────
    // Create
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateAsync()
    {
        Error = null;

        // Шаг 1: события
        List<EventInfoDto> events;
        try
        {
            events = await LoadAllEventsAsync();
        }
        catch (ApiException apiEx)
        {
            Error = $"Ошибка загрузки событий ({(int)apiEx.StatusCode}): {apiEx.Content ?? apiEx.Message}";
            _logger.LogError(apiEx, "Load events failed");
            return;
        }
        catch (Exception ex)
        {
            Error = "Не удалось загрузить события: " + ex.Message;
            _logger.LogError(ex, "Load events failed");
            return;
        }

        if (events.Count == 0)
        {
            Error = "Нет доступных событий — создайте событие сначала.";
            return;
        }

        // Шаг 2: диалог
        Guid? eventId;
        try
        {
            eventId = await _dialogs.ShowBookingDialogAsync(events);
        }
        catch (Exception ex)
        {
            Error = "Не удалось открыть диалог: " + ex.Message;
            _logger.LogError(ex, "ShowBookingDialog failed");
            return;
        }
        if (eventId == null) return;

        // Шаг 3: снимок существующих ID ДО создания
        var existingIds = Items.Select(x => x.Source.Id).ToHashSet();

        // Шаг 4: создать
        try
        {
            await _api.CreateBookingAsync(eventId.Value);
            _logger.LogInformation("Create booking: API accepted");
        }
        catch (ApiException apiEx)
        {
            Error = $"Не удалось создать бронь ({(int)apiEx.StatusCode}): {apiEx.Content ?? apiEx.Message}";
            _logger.LogError(apiEx, "CreateBookingAsync failed");
            return;
        }
        catch (Exception ex)
        {
            Error = "Не удалось создать бронь: " + ex.Message;
            _logger.LogError(ex, "CreateBookingAsync failed");
            return;
        }

        // Шаг 5: перезагрузить + поллинг конкретной брони
        await LoadAsync();

        var newBooking = Items.FirstOrDefault(x => !existingIds.Contains(x.Source.Id));
        if (newBooking == null)
        {
            _logger.LogWarning("Create booking: new booking not found in list");
            return;
        }

        await PollUntilProcessedAsync(newBooking.Source.Id);
    }

    /// <summary>
    /// Поллит конкретную бронь, пока не будет уверенности в финальном статусе.
    /// 
    /// Правила:
    ///   • Rejected — терминальный, выходим сразу.
    ///   • Confirmed — может стать Rejected (Events не нашёл мест),
    ///     поэтому ждём grace-период (6с). Если за это время Rejected не пришёл — считаем финалом.
    ///   • Pending — продолжаем.
    /// </summary>
    private async Task PollUntilProcessedAsync(
        Guid bookingId,
        int maxWaitMs = 15000,
        int delayMs = 500,
        int confirmedGraceMs = 6000)
    {
        var start = DateTime.UtcNow;
        DateTime? firstConfirmedAt = null;

        while ((DateTime.UtcNow - start).TotalMilliseconds < maxWaitMs)
        {
            await Task.Delay(delayMs);
            await LoadAsync();

            var booking = Items.FirstOrDefault(x => x.Source.Id == bookingId);
            if (booking == null)
            {
                _logger.LogWarning("Poll: booking {Id} disappeared", bookingId);
                return;
            }

            var status = booking.Source.Status;

            // Rejected — точно финал
            if (status ==   BookingStatus.Rejected)
            {
                _logger.LogInformation("Poll: booking {Id} → Rejected", bookingId);
                return;
            }

            // Confirmed — ждём grace (может прийти Rejected)
            if (status == BookingStatus.Confirmed)
            {
                firstConfirmedAt ??= DateTime.UtcNow;
                if ((DateTime.UtcNow - firstConfirmedAt.Value).TotalMilliseconds >= confirmedGraceMs)
                {
                    _logger.LogInformation("Poll: booking {Id} → Confirmed (grace expired)", bookingId);
                    return;
                }
            }
            else
            {
                // Pending — ждём дальше
                firstConfirmedAt = null;
            }
        }

        _logger.LogWarning("Poll: booking {Id} — timeout, last status unknown", bookingId);
        Error = "Бронь всё ещё обрабатывается. Нажмите «Обновить» позже.";
    }

    /// <summary>Пагинированный загруз всех событий (сервер ограничивает PageSize=100).</summary>
    private async Task<List<EventInfoDto>> LoadAllEventsAsync(int pageSize = 100)
    {
        var all = new List<EventInfoDto>();
        var page = 1;

        while (page <= 100)
        {
            var paged = await _eventsApi.GetEventsAsync(page, pageSize);
            var items = paged?.Items;
            if (items == null || items.Count == 0) break;

            all.AddRange(items);

            if (items.Count < pageSize) break; 
            if (paged!.TotalCount > 0 && all.Count >= paged.TotalCount) break;

            page++;
        }

        return all;
    }

    // ─────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────

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
        catch (ApiException apiEx)
        {
            Error = $"Не удалось отменить ({(int)apiEx.StatusCode}): {apiEx.Content ?? apiEx.Message}";
            _logger.LogError(apiEx, "CancelBooking failed");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _logger.LogError(ex, "CancelBooking failed");
        }
    }

    // ─────────────────────────────────────────────
    // Selection
    // ─────────────────────────────────────────────

    public void SelectCard(BookingCardVm vm)
    {
        if (Selected != null) Selected.IsSelected = false;
        Selected = vm;
        if (vm != null) vm.IsSelected = true;
    }
}