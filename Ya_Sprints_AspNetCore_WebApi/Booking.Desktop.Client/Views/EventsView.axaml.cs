using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Booking.Desktop.Client.ViewModels;
using Sprints.Desktop.ViewModels;

namespace Booking.Desktop.Client;

public partial class EventsView : UserControl
{
    public EventsView() => InitializeComponent();

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is EventCardVm vm
            && DataContext is EventsViewModel parent)
        {
            parent.SelectCard(vm);
        }
    }

    private void Card_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control c && c.DataContext is EventCardVm vm
            && DataContext is EventsViewModel parent)
        {
            parent.SelectCard(vm);
            if (parent.EditCommand.CanExecute(null))
                parent.EditCommand.Execute(null);
        }
    }
}