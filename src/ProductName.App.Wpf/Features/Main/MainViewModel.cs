using ProductName.App.Wpf.Base;
using ProductName.AppCore.Authentication.Interface;

namespace ProductName.App.Wpf.Features;

public sealed class MainViewModel : BaseViewModel
{
    public string Header { get; }

    public MainViewModel(IAuthenticationWorkflow workflow) => Header =
        $"{workflow.CurrentSession?.Account?.CompanyName} — {workflow.CurrentSession?.Account?.DisplayName}";
}