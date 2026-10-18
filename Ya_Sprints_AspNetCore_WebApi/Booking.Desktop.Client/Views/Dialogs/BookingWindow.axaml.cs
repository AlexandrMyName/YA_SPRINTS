using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Sprints.Desktop.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Booking.Desktop.Client;

public partial class BookingWindow : Window
{
    public Guid? SelectedEventId { get; private set; }

    public BookingWindow() { InitializeComponent(); }

    public BookingWindow(IReadOnlyList<EventInfoDto> events) : this()
    {
        EventsList.ItemsSource = events.Select(e => new
        {
            e.Id,
            e.Title,
            AvailabilityLabel = $"{e.StartAt.ToLocalTime():yyyy-MM-dd HH:mm} — свободно {e.AvailableSeats}/{e.TotalSeats}"
        }).ToList();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        ErrorText.IsVisible = false;

        if (EventsList.SelectedItem is null)
        {
            ErrorText.Text = "Выберите событие";
            ErrorText.IsVisible = true;
            return;
        }

        SelectedEventId = (Guid)EventsList.SelectedItem.GetType().GetProperty("Id")!.GetValue(EventsList.SelectedItem)!;
        Close(true);
    }
}