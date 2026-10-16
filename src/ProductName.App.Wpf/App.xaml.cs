namespace ProductName.App.Wpf
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using ProductName.App.Wpf.Features.Login;
    using ProductName.AppCore.Authentication;
    using ProductName.AppCore.Authentication.Interface;
    using ProductName.Infrastructure;
    using System.Diagnostics;
    using System.Windows;

    /// <summary>
    /// Defines the <see cref="App" />
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Defines the _host
        /// </summary>
        private IHost? _host;

        /// <summary>
        /// The OnStartup
        /// </summary>
        /// <param name="e">The e<see cref="StartupEventArgs"/></param>
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
                        services.AddTransient<LoginViewModel>();
                        services.AddTransient<LoginView>();
                    })
                    .Build();

                await _host.StartAsync();

                LoginView loginView = _host.Services.GetRequiredService<LoginView>();
                MainWindow = loginView;
                loginView.Show();
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

        /// <summary>
        /// The OnExit
        /// </summary>
        /// <param name="e">The e<see cref="ExitEventArgs"/></param>
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
