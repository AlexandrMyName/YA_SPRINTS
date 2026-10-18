using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Booking.Desktop.Client;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog() { InitializeComponent(); }

    public ConfirmDialog(string title, string message) : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
    private void Ok_Click(object? sender, RoutedEventArgs e) => Close(true);
}