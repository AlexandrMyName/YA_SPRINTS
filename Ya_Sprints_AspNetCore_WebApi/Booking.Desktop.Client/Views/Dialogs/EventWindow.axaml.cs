using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Sprints.Desktop.Api;
using System;
using System.Globalization;

namespace Booking.Desktop.Client;

public partial class EventWindow : Window
{
    public EventInfoDto? Result { get; private set; }

    public EventWindow() { InitializeComponent(); }

    public EventWindow(EventInfoDto? existing) : this()
    {
        if (existing != null)
        {
            HeaderText.Text = "Редактировать событие";
            TitleBox.Text = existing.Title;
            DescBox.Text = existing.Description ?? "";
            StartBox.Text = existing.StartAt.ToString("yyyy-MM-dd HH:mm");
            EndBox.Text = existing.EndAt.ToString("yyyy-MM-dd HH:mm");
            SeatsBox.Text = existing.TotalSeats.ToString();
            // сохраняем availableSeats
            Result = existing;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {

        ErrorText.IsVisible = false;

        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        { ShowError("Название обязательно"); return; }

        if (!DateTime.TryParseExact(StartBox.Text, "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var start))
        { ShowError("Неверный формат начала (yyyy-MM-dd HH:mm)"); return; }

        if (!DateTime.TryParseExact(EndBox.Text, "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var end))
        { ShowError("Неверный формат окончания (yyyy-MM-dd HH:mm)"); return; }

        if (end <= start)
        { ShowError("Окончание должно быть позже начала"); return; }

        if (!int.TryParse(SeatsBox.Text, out var seats) || seats <= 0)
        { ShowError("Мест должно быть больше 0"); return; }

        var baseDto = Result ?? new EventInfoDto();
        Result = new EventInfoDto
        {
            Id = baseDto.Id,
            Title = TitleBox.Text!.Trim(),
            Description = DescBox.Text?.Trim(),
            StartAt = start,
            EndAt = end,
            TotalSeats = seats,
            AvailableSeats = baseDto.AvailableSeats > 0 ? baseDto.AvailableSeats : seats
        };

        Close(true);
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.IsVisible = true;
    }
}