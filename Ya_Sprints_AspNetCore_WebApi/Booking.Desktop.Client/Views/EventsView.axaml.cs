using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Booking.Desktop.Client.ViewModels;
using Sprints.Desktop.ViewModels;
using System.Linq;

namespace Booking.Desktop.Client;

public partial class EventsView : UserControl
{
    public EventsView() => InitializeComponent();

    private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: EventCardVm vm }) return;
        if (DataContext is not EventsViewModel viewModel) return;

        viewModel.SelectCard(vm);
    }

    private void Card_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: EventCardVm vm }) return;
        if (DataContext is not EventsViewModel viewModel) return;

        viewModel.SelectCard(vm);
    }

    private void TopCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: EventCardVm vm }) return;
        if (DataContext is not EventsViewModel viewModel) return;

        var match = viewModel.Items.FirstOrDefault(x => x.Source.Id == vm.Source.Id);
        if (match != null)
            viewModel.SelectCard(match);
    }
}