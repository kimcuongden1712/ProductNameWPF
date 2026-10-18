namespace ProductName.App.Wpf.Features.Main.Navigation.Interface;

// Screens implement this contract when they can contain unsaved data.
public interface IUnsavedChangesScreen
{
    bool HasUnsavedChanges { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void DiscardChanges();
}
