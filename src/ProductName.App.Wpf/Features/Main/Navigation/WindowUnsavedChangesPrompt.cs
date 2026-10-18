using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Dialogs;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;
using System.Windows;

namespace ProductName.App.Wpf.Features.Main.Navigation;

/// <summary>Shows the styled dialog and returns the user's choice to navigation.</summary>
public sealed class WindowUnsavedChangesPrompt : IUnsavedChangesPrompt
{
    public Task<UnsavedChangesDecision> AskAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new UnsavedChangesDialogViewModel();
        var dialog = new UnsavedChangesDialog(viewModel);
        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);

        if (owner is not null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        dialog.ShowDialog();
        return Task.FromResult(viewModel.Decision ?? UnsavedChangesDecision.Cancel);
    }
}