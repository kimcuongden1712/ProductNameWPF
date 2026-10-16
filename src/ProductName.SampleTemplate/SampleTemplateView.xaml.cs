using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace NewTemplateProject;

public partial class SampleTemplateView : UserControl
{
    public ObservableCollection<SampleGridRow> DemoRows { get; } =
    [
        new() { Order = "A-102", Description = "Scheduled collection", Status = "Reserved" },
        new() { Order = "B-208", Description = "Application request", Status = "From app" },
        new() { Order = "C-311", Description = "Priority dispatch", Status = "Urgent" },
        new() { Order = "D-415", Description = "Next assignment", Status = "Next" }
    ];

    public SampleTemplateView()
    {
        InitializeComponent();
    }

    private void OpenDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Window.Dialog sample",
            Style = (Style)FindResource("Window.Dialog"),
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 420,
            Height = 220,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Children =
                {
                    new TextBlock
                    {
                        Text = "This window uses the shared dialog style.",
                        Style = (Style)FindResource("Text.SectionHeading"),
                        Margin = new Thickness(0, 0, 0, 16)
                    },
                    new Button
                    {
                        Content = "Close",
                        Style = (Style)FindResource("Button.Primary"),
                        Width = 100,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        IsCancel = true
                    }
                }
            }
        };

        dialog.ShowDialog();
    }
}

public sealed class SampleGridRow
{
    public string Order { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}
