namespace ProductName.App.Wpf
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using ProductName.App.Wpf.Features;
    using ProductName.App.Wpf.Features.Login;
    using ProductName.AppCore.Authentication;
    using ProductName.AppCore.Authentication.Interface;
    using ProductName.AppCore.Model;
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
                        services.AddTransient<MenuSettingViewModel>();
                        services.AddTransient<MenuSettingView>();
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<MainView>();
                    })
                    .Build();

                await _host.StartAsync();

                var workflow = _host.Services.GetRequiredService<IAuthenticationWorkflow>();
                workflow.SessionInvalidated += (_, _) => Dispatcher.Invoke(() =>
                {
                    var login = CreateLogin();
                    MainWindow = login;
                    login.Show();
                    foreach (Window window in Windows.Cast<Window>().Where(window => window != login).ToArray()) window.Close();
                });
                var restore = await workflow.TryRestoreSessionAsync(CancellationToken.None);
                if (restore == RestoreSessionStatus.Restored)
                    ShowMenu();
                else
                {
                    var login = CreateLogin();
                    if (restore == RestoreSessionStatus.Unavailable)
                        (login.DataContext as LoginViewModel)?
                            .ShowMessage("Cannot connect to the server. Please check your network connection and try again.");
                    MainWindow = login;
                    login.Show();
                }
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

        private LoginView CreateLogin()
        {
            var login = _host!.Services.GetRequiredService<LoginView>();
            if (login.DataContext is LoginViewModel vm)
            {
                vm.SignInCompleted += (_, _) => ShowMenu();
                vm.RetryProfileRequested += async (_, _) =>
                {
                    try
                    {
                        await _host.Services.GetRequiredService<IAuthenticationWorkflow>().RetryProfileLoadAsync(CancellationToken.None);
                        ShowMenu();
                    }
                    catch (Exception ex)
                    {
                        vm.ShowMessage(ex.Message);
                    }
                };
            }
            return login;
        }

        private void ShowMenu()
        {
            var menu = _host!.Services.GetRequiredService<MenuSettingView>();
            if (menu.DataContext is MenuSettingViewModel vm)
            {
                vm.SignOutCompleted += (_, _) =>
                {
                    var login = CreateLogin();
                    MainWindow = login;
                    login.Show();
                    menu.Close();
                };
                vm.OpenMainRequested += (_, _) =>
                {
                    var main = _host.Services.GetRequiredService<MainView>();
                    MainWindow = main;
                    main.Show();
                    menu.Close();
                };
            }
            var previous = MainWindow;
            MainWindow = menu;
            menu.Show();
            previous?.Close();
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