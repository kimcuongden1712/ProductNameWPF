using ProductName.App.Wpf.Base;
using ProductName.AppCore.Authentication.Interface;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features;

public sealed class MenuSettingViewModel : BaseViewModel
{
    private readonly IAuthenticationWorkflow _workflow;
    public string AccountName => _workflow.CurrentSession?.Account?.DisplayName ?? string.Empty;
    public ICommand OpenMainCommand { get; }
    public ICommand SignOutCommand { get; }

    public event EventHandler? SignOutCompleted;

    public event EventHandler? OpenMainRequested;

    public MenuSettingViewModel(IAuthenticationWorkflow workflow)
    {
        _workflow = workflow;
        OpenMainCommand = new RelayCommand(_ => OpenMainRequested?.Invoke(this, EventArgs.Empty));
        SignOutCommand = new AsyncCommand(SignOutAsync, () => true, _ => { });
    }

    private async Task SignOutAsync()
    {
        await _workflow.SignOutAsync(CancellationToken.None);
        SignOutCompleted?.Invoke(this, EventArgs.Empty);
    }
}