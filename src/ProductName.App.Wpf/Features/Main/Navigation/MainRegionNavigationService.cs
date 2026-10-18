using ProductName.App.Wpf.Base;
using ProductName.App.Wpf.Common.Enums;
using ProductName.App.Wpf.Features.Main.Navigation.Interface;

namespace ProductName.App.Wpf.Features.Main.Navigation;

/// <summary>Switches the main content while keeping each screen instance for the current window session.</summary>
public sealed class MainRegionNavigationService(IUnsavedChangesPrompt unsavedChangesPrompt)
{
    // Keep one instance of each screen so its state survives navigation.
    private readonly Dictionary<string, BaseViewModel> _screens = new(StringComparer.Ordinal);

    public BaseViewModel? CurrentScreen { get; private set; }

    public event EventHandler? CurrentScreenChanged;

    public void ActivateInitialScreen(string screenId)
    {
        if (CurrentScreen is not null)
            throw new InvalidOperationException("The initial screen has already been activated.");

        if (!_screens.TryGetValue(screenId, out var screen))
            throw new InvalidOperationException($"Screen '{screenId}' is not registered.");

        CurrentScreen = screen;
        CurrentScreenChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Register(string screenId, BaseViewModel screen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);
        ArgumentNullException.ThrowIfNull(screen);

        if (!_screens.TryAdd(screenId, screen))
            throw new InvalidOperationException($"Screen '{screenId}' is already registered.");
    }

    public async Task<bool> NavigateToAsync(string screenId, CancellationToken cancellationToken)
    {
        if (!_screens.TryGetValue(screenId, out var destination))
            throw new InvalidOperationException($"Screen '{screenId}' is not registered.");

        if (ReferenceEquals(CurrentScreen, destination))
            return true;

        // Do not replace the current screen until the user handles its changes.
        if (CurrentScreen is IUnsavedChangesScreen { HasUnsavedChanges: true } currentWithChanges)
        {
            var decision = await unsavedChangesPrompt.AskAsync(cancellationToken);
            switch (decision)
            {
                case UnsavedChangesDecision.Save:
                    await currentWithChanges.SaveChangesAsync(cancellationToken);
                    break;

                case UnsavedChangesDecision.Discard:
                    currentWithChanges.DiscardChanges();
                    break;

                default:
                    return false;
            }
        }

        CurrentScreen = destination;
        CurrentScreenChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }
}