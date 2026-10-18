using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Desktop.ViewModels;

namespace Booking.Desktop.Client
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = Program.Services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = new MainWindow { DataContext = vm };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}