using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using ProductName.App.Wpf.Features.Main;
using ProductName.AppCore.Authentication;
using ProductName.Infrastructure;

namespace ProductName.App.Wpf
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _host = Host.CreateDefaultBuilder(e.Args)
                    .UseContentRoot(AppContext.BaseDirectory)
                    .ConfigureServices((context, services) =>
                    {
                        services.AddProductNameInfrastructure(context.Configuration);
                        services.AddSingleton<IAuthenticationWorkflow, AuthenticationWorkflow>();
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<MainWindow>();
                    })
                    .Build();

                await _host.StartAsync();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Application startup failed: {0}", ex);
                MessageBox.Show(
                    $"The application could not start.{Environment.NewLine}{ex.Message}",
                    "Startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _host?.Dispose();
                _host = null;
                Shutdown(-1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_host is not null)
                {
                    await _host.StopAsync();
                }
            }
            finally
            {
                _host?.Dispose();
                _host = null;
                base.OnExit(e);
            }
        }
    }
}
