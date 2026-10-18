using ProductName.App.Wpf.Base;
using ProductName.AppCore.Authentication;
using ProductName.AppCore.Authentication.Interface;
using System.Diagnostics;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Login;

public sealed class LoginViewModel : BaseViewModel
{
    #region fields
    public event EventHandler? SignInCompleted;
    //public event EventHandler? RetryProfileRequested;
    private readonly AsyncCommand _signInCommand;
    //private readonly RelayCommand _retryProfileCommand;
    private readonly IAuthenticationWorkflow _authenticationWorkflow;
    private string _customerCode = string.Empty;
    private string _password = string.Empty;
    private string _message = string.Empty;
    #endregion

    #region properties
    public string CustomerCode
    {
        get => _customerCode;
        set
        {
            if (SetProperty(ref _customerCode, value))
                _signInCommand.NotifyCanExecuteChanged();
        }
    }
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                _signInCommand.NotifyCanExecuteChanged();
        }
    }
    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }
    #endregion

    #region commands & events
    public ICommand SignInCommand => _signInCommand;
    //public ICommand RetryProfileCommand => _retryProfileCommand;
    #endregion

    public LoginViewModel(IAuthenticationWorkflow authenticationWorkflow)
    {
        _authenticationWorkflow = authenticationWorkflow;
        _signInCommand = new AsyncCommand(SignInAsync, CanSignIn, HandleSignInError);
        //_retryProfileCommand = new RelayCommand(_ => RetryProfileRequested?.Invoke(this, EventArgs.Empty));
    }
    #region private methods
    private async Task SignInAsync()
    {
        Message = string.Empty;

        if (string.IsNullOrWhiteSpace(CustomerCode))
        {
            Message = "Customer ID is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Message = "Password is required.";
            return;
        }

        try
        {
            var session = await _authenticationWorkflow.SignInAsync(
                CustomerCode,
                Password,
                CancellationToken.None);

            SignInCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (AuthenticationException ex)
        {
            Message = ex.Message;
        }
    }

    public void ShowMessage(string message) => Message = message;
    //public void RetryProfileLoad() => RetryProfileRequested?.Invoke(this, EventArgs.Empty);
    private bool CanSignIn() => !string.IsNullOrWhiteSpace(CustomerCode) && !string.IsNullOrWhiteSpace(Password);

    private void HandleSignInError(Exception exception)
    {
        Trace.TraceError("Sign-in command failed: {0}", exception);
        Message = "Sign-in is not available right now. Please try again later.";
    }

    #endregion
}
