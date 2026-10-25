using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Booking.Desktop.Client.ViewModels;
using Sprints.Desktop.ViewModels;

namespace Booking.Desktop.Client;

public partial class BookingsView : UserControl
{
    public BookingsView() => InitializeComponent();

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is BookingCardVm vm
            && DataContext is BookingsViewModel parent)
        {
            parent.SelectCard(vm);
        }
    }
}