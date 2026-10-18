using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Sprints.Desktop.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booking.Desktop.Client.Services;

public interface IDialogService
{
    Task<EventInfoDto?> ShowEventDialogAsync(EventInfoDto? existing);
    Task<Guid?> ShowBookingDialogAsync(IReadOnlyList<EventInfoDto> events);
    Task<bool> ConfirmAsync(string title, string message);
}

public sealed class DialogService : IDialogService
{
    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<EventInfoDto?> ShowEventDialogAsync(EventInfoDto? existing)
    {
        var owner = MainWindow;
        if (owner == null) return null;

        var dlg = new EventWindow(existing);
        var ok = await dlg.ShowDialog<bool>(owner);
        return ok ? dlg.Result : null;
    }

    public async Task<Guid?> ShowBookingDialogAsync(IReadOnlyList<EventInfoDto> events)
    {
        var owner = MainWindow;
        if (owner == null) return null;

        var dlg = new BookingWindow(events);
        var ok = await dlg.ShowDialog<bool>(owner);
        return ok ? dlg.SelectedEventId : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var owner = MainWindow;
        if (owner == null) return false;

        var dlg = new ConfirmDialog(title, message);
        return await dlg.ShowDialog<bool>(owner);
    }
}