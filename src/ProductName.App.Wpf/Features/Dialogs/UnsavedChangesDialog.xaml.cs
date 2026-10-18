using ProductName.App.Wpf.Common.Enums;
using System.Windows;

namespace ProductName.App.Wpf.Features.Dialogs;

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialogViewModel ViewModel { get; }

    public UnsavedChangesDialog(UnsavedChangesDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.DecisionSelected += OnDecisionSelected;
    }

    private void OnDecisionSelected(UnsavedChangesDecision decision)
    {
        // Setting DialogResult closes the modal window.
        DialogResult = decision != UnsavedChangesDecision.Cancel;
    }
}
