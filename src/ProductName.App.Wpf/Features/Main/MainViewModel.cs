using Microsoft.Extensions.Logging;
using ProductName.App.Wpf.Base;
using ProductName.AppCore.Authentication;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Main
{
    public sealed class MainViewModel : BaseNotifyPropertyChanged
    {
        private readonly IAuthenticationWorkflow _authentication;
        private readonly ILogger<MainViewModel> _logger;
        private readonly AsyncCommand _signInCommand;
        private readonly AsyncCommand _retryCommand;
        private readonly AsyncCommand _openDispatchCommand;
        private readonly AsyncCommand _backToMenuCommand;
        private readonly AsyncCommand _signOutCommand;
        private string _customerCode = string.Empty;
        private string _password = string.Empty;
        private string _message = "Checking for a saved session...";
        private string _displayName = string.Empty;
        private string _companyName = string.Empty;
        private string _branchName = string.Empty;
        private bool _isBusy = true;
        private bool _isLoginScreen = true;
        private bool _isMenuScreen;
        private bool _isDispatchScreen;
        private bool _canRetry;

        public MainViewModel(
            IAuthenticationWorkflow authentication,
            ILogger<MainViewModel> logger)
        {
            _authentication = authentication;
            _logger = logger;

            _signInCommand = new AsyncCommand(
                SignInAsync,
                () => !IsBusy && !_authentication.HasPendingProfileLoad,
                HandleUnexpectedError);
            _retryCommand = new AsyncCommand(RetryAsync, () => !IsBusy && CanRetry, HandleUnexpectedError);
            _openDispatchCommand = new AsyncCommand(OpenDispatchAsync, () => !IsBusy && IsMenuScreen, HandleUnexpectedError);
            _backToMenuCommand = new AsyncCommand(BackToMenuAsync, () => !IsBusy && IsDispatchScreen, HandleUnexpectedError);
            _signOutCommand = new AsyncCommand(SignOutAsync, () => !IsBusy && (IsMenuScreen || IsDispatchScreen), HandleUnexpectedError);
            _authentication.SessionInvalidated += OnSessionInvalidated;
        }

        public string CustomerCode
        {
            get => _customerCode;
            set => SetProperty(ref _customerCode, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public string DisplayName
        {
            get => _displayName;
            private set => SetProperty(ref _displayName, value);
        }

        public string CompanyName
        {
            get => _companyName;
            private set => SetProperty(ref _companyName, value);
        }

        public string BranchName
        {
            get => _branchName;
            private set => SetProperty(ref _branchName, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        public bool IsLoginScreen
        {
            get => _isLoginScreen;
            private set
            {
                if (SetProperty(ref _isLoginScreen, value))
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        public bool IsMenuScreen
        {
            get => _isMenuScreen;
            private set
            {
                if (SetProperty(ref _isMenuScreen, value))
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        public bool IsDispatchScreen
        {
            get => _isDispatchScreen;
            private set
            {
                if (SetProperty(ref _isDispatchScreen, value))
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        public bool CanRetry
        {
            get => _canRetry;
            private set
            {
                if (SetProperty(ref _canRetry, value))
                {
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        public ICommand SignInCommand => _signInCommand;

        public ICommand RetryCommand => _retryCommand;

        public ICommand OpenDispatchCommand => _openDispatchCommand;

        public ICommand BackToMenuCommand => _backToMenuCommand;

        public ICommand SignOutCommand => _signOutCommand;

        public async Task InitializeAsync()
        {
            IsBusy = true;
            Message = "Checking for a saved session...";
            CanRetry = false;

            try
            {
                var result = await _authentication.TryRestoreSessionAsync(CancellationToken.None);
                if (result == RestoreSessionStatus.Restored && _authentication.CurrentSession is { } session)
                {
                    ShowMenu(session);
                    return;
                }

                ShowLogin(result == RestoreSessionStatus.InvalidToken
                    ? "Your saved session expired. Please sign in again."
                    : string.Empty);
            }
            catch (AuthenticationException ex)
            {
                ShowLogin(ex.Message);
                CanRetry = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore the saved session.");
                ShowLogin("The saved session could not be checked. Check the connection and try again.");
                CanRetry = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SignInAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomerCode) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Enter both your customer ID and password.";
                CanRetry = false;
                return;
            }

            IsBusy = true;
            Message = string.Empty;
            CanRetry = false;
            try
            {
                var session = await _authentication.SignInAsync(
                    CustomerCode,
                    Password,
                    CancellationToken.None);
                Password = string.Empty;
                ShowMenu(session);
            }
            catch (AuthenticationException ex)
            {
                Message = ex.Message;
                CanRetry = _authentication.HasPendingProfileLoad;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RetryAsync()
        {
            IsBusy = true;
            CanRetry = false;
            Message = "Retrying...";
            try
            {
                if (_authentication.HasPendingProfileLoad)
                {
                    ShowMenu(await _authentication.RetryProfileLoadAsync(CancellationToken.None));
                    Password = string.Empty;
                    return;
                }

                var result = await _authentication.TryRestoreSessionAsync(CancellationToken.None);
                if (result == RestoreSessionStatus.Restored && _authentication.CurrentSession is { } session)
                {
                    ShowMenu(session);
                    return;
                }

                ShowLogin(result == RestoreSessionStatus.InvalidToken
                    ? "Your saved session expired. Please sign in again."
                    : "No saved session was found. Sign in to continue.");
            }
            catch (AuthenticationException ex)
            {
                ShowLogin(ex.Message);
                CanRetry = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task OpenDispatchAsync()
        {
            var session = _authentication.CurrentSession;
            if (session is null)
            {
                ShowLogin("Your session is no longer available. Please sign in again.");
                return Task.CompletedTask;
            }

            if (session.Account.Branches.Count == 0)
            {
                Message = "Your account has no branch available for dispatch.";
                return Task.CompletedTask;
            }

            BranchName = session.Account.Branches[0].Name;
            Message = string.Empty;
            IsMenuScreen = false;
            IsDispatchScreen = true;
            return Task.CompletedTask;
        }

        private Task BackToMenuAsync()
        {
            Message = string.Empty;
            IsDispatchScreen = false;
            IsMenuScreen = true;
            return Task.CompletedTask;
        }

        private async Task SignOutAsync()
        {
            IsBusy = true;
            ShowLogin("Signing out...");
            try
            {
                await _authentication.SignOutAsync(CancellationToken.None);
                ShowLogin("You have signed out.");
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning(ex, "Remote sign-out did not complete; local session was cleared.");
                ShowLogin("You are signed out on this device, but the server could not confirm sign-out.");
            }
            finally
            {
                Password = string.Empty;
                IsBusy = false;
            }
        }

        private void ShowMenu(AuthenticationSession session)
        {
            DisplayName = session.Account.DisplayName;
            CompanyName = session.Account.CompanyName;
            BranchName = session.Account.Branches.FirstOrDefault()?.Name ?? "No branch";
            Message = string.Empty;
            CanRetry = false;
            IsLoginScreen = false;
            IsDispatchScreen = false;
            IsMenuScreen = true;
        }

        private void ShowLogin(string message)
        {
            Message = message;
            IsMenuScreen = false;
            IsDispatchScreen = false;
            IsLoginScreen = true;
        }

        private void HandleUnexpectedError(Exception exception)
        {
            _logger.LogError(exception, "An unexpected UI operation failed.");
            Message = "An unexpected error occurred. Please try again.";
        }

        private void OnSessionInvalidated(object? sender, EventArgs e)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null)
            {
                ShowLogin("Your session expired. Please sign in again.");
                return;
            }

            dispatcher.BeginInvoke(() =>
            {
                Password = string.Empty;
                ShowLogin("Your session expired. Please sign in again.");
            });
        }

        private void NotifyCommandsCanExecuteChanged()
        {
            _signInCommand?.NotifyCanExecuteChanged();
            _retryCommand?.NotifyCanExecuteChanged();
            _openDispatchCommand?.NotifyCanExecuteChanged();
            _backToMenuCommand?.NotifyCanExecuteChanged();
            _signOutCommand?.NotifyCanExecuteChanged();
        }
    }
}
