using System.Windows;

namespace ProductName.App.Wpf.Features.Main;

// Connects the view to the ViewModel supplied by dependency injection.
public partial class MainView : Window
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
