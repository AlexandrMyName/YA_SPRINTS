using Avalonia;
using Booking.Desktop.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Sprints.Desktop.Api;
using Sprints.Desktop.ViewModels;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;  


namespace Booking.Desktop.Client
{
    internal static class Program
    {
        public static IServiceProvider Services { get; private set; } = null!;

        [STAThread]
        public static void Main(string[] args)
        {
            var hostBuilder = Host.CreateApplicationBuilder(args);
            hostBuilder.Configuration.AddJsonFile("appsettings.json", optional: false);
            ConfigureServices(hostBuilder.Services, hostBuilder.Configuration);

            var host = hostBuilder.Build();
            Services = host.Services;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            // Токены и refresh  
            services.AddSingleton<ITokenStore, FileTokenStore>();
            services.AddSingleton<IDialogService, DialogService>();


            services.AddSingleton<AuthRefresher>();
            services.AddTransient<AuthDelegatingHandler>();
 
            // Общие настройки JSON для Refit  
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }  // enum ↔ int/string
            };
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
            };

            var usersBase = config["Api:UsersBaseUrl"]!;
            var eventsBase = config["Api:EventsBaseUrl"]!;
            var bookingsBase = config["Api:BookingsBaseUrl"]!;

            //  Auth API (БЕЗ AuthDelegatingHandler — иначе рекурсия при refresh)  
            services.AddRefitClient<IAuthApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(usersBase));

            // Остальные API — с автообновлением токена  
            services.AddRefitClient<IUsersApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(usersBase))
                .AddHttpMessageHandler<AuthDelegatingHandler>();

            services.AddRefitClient<IEventsApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(eventsBase))
                .AddHttpMessageHandler<AuthDelegatingHandler>();

            services.AddRefitClient<IBookingsApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(bookingsBase))
                .AddHttpMessageHandler<AuthDelegatingHandler>();

            // ViewModels  
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<EventsViewModel>();
            services.AddTransient<BookingsViewModel>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
