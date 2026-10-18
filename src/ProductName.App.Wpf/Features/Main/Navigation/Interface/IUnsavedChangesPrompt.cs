using ProductName.App.Wpf.Common.Enums;

namespace ProductName.App.Wpf.Features.Main.Navigation.Interface;

// Keeps the navigation service independent from the prompt's UI.
public interface IUnsavedChangesPrompt
{
    Task<UnsavedChangesDecision> AskAsync(CancellationToken cancellationToken);
}
