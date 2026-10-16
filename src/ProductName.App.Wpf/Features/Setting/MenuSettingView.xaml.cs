using System.Windows;

namespace ProductName.App.Wpf.Features;

public partial class MenuSettingView : Window
{
    public MenuSettingView(MenuSettingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}