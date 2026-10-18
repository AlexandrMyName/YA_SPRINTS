using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Booking.Desktop.Client.ViewModels;
using Sprints.Desktop.ViewModels;

namespace Booking.Desktop.Client;

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is UserCardVm vm
            && DataContext is UsersViewModel parent)
        {
            parent.SelectCard(vm);
        }
    }
}