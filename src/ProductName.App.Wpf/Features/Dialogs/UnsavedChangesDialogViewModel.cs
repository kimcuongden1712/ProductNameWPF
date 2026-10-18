using ProductName.App.Wpf.Base;
using ProductName.App.Wpf.Common.Enums;
using System.Windows.Input;

namespace ProductName.App.Wpf.Features.Dialogs;

// Holds the user's choice; the Window uses it to close the modal dialog.
public sealed class UnsavedChangesDialogViewModel
{
    #region properties
    public UnsavedChangesDecision? Decision { get; private set; }
    #endregion
    #region commands & events
    public ICommand SaveCommand { get; }
    public ICommand DiscardCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<UnsavedChangesDecision>? DecisionSelected;
    #endregion
    public UnsavedChangesDialogViewModel()
    {
        SaveCommand = new RelayCommand(_ => Select(UnsavedChangesDecision.Save));
        DiscardCommand = new RelayCommand(_ => Select(UnsavedChangesDecision.Discard));
        CancelCommand = new RelayCommand(_ => Select(UnsavedChangesDecision.Cancel));
    }
    #region methods
    private void Select(UnsavedChangesDecision decision)
    {
        Decision = decision;
        DecisionSelected?.Invoke(decision);
    }
    #endregion
}
