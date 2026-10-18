namespace ProductName.App.Wpf
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using ProductName.App.Wpf.Features;
    using ProductName.App.Wpf.Features.Login;
    using ProductName.App.Wpf.Features.Main;
    using ProductName.App.Wpf.Features.Main.Navigation;
    using ProductName.App.Wpf.Features.Main.Navigation.Interface;
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
                        services.AddSingleton<IUnsavedChangesPrompt, WindowUnsavedChangesPrompt>();
                        // Keep dialog styling and navigation logic behind the prompt interface.
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<MainView>();
                    })
                    .Build();

                await _host.StartAsync();
                var workflow = _host.Services.GetRequiredService<IAuthenticationWorkflow>();
                StartApp(workflow);
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

        #region private methods

        /// <summary>
        /// Initializes the application by restoring a user session if available or displaying the login window if not.
        /// </summary>
        /// <remarks>If the session cannot be restored, the method displays the login window and notifies
        /// the user if the server is unavailable. The method also handles session invalidation by returning the user to
        /// the login window.</remarks>
        /// <param name="workflow">The authentication workflow used to manage session restoration and handle session invalidation events.
        /// Cannot be null.</param>
        private async void StartApp(IAuthenticationWorkflow workflow)
        {
            workflow.SessionInvalidated += (_, _) => Dispatcher.Invoke(() =>
            {
                var login = CreateLogin();
                MainWindow = login;
                login.Show();
                foreach (Window window in Windows.Cast<Window>().Where(window => window != login).ToArray())
                    window.Close();
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

        /// <summary>
        /// Creates and configures a new instance of the login view with its associated event handlers.
        /// </summary>
        /// <remarks>The returned login view has event handlers attached for sign-in completion. These handlers manage navigation and error display as appropriate.</remarks>
        /// <returns>A configured instance of the login view ready for user interaction.</returns>
        private LoginView CreateLogin()
        {
            var login = _host!.Services.GetRequiredService<LoginView>();
            if (login.DataContext is LoginViewModel vm)
            {
                vm.SignInCompleted += (_, _) => ShowMenu();
            }
            return login;
        }

        /// <summary>
        /// Displays the menu settings window and manages transitions between the menu, main, and login views based on
        /// user actions.
        /// </summary>
        /// <remarks>This method sets the application's main window to the menu settings view and attaches
        /// event handlers to respond to sign-out and main view requests. When the user signs out or requests to open
        /// the main view, the corresponding window is displayed and the menu window is closed. If the previous main
        /// window was not the menu settings view, it is closed to ensure only one main window is active at a
        /// time.</remarks>
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
            if (previous is not MenuSettingView)
            {
                previous?.Close();
            }
        }

        #endregion private methods
    }
}
