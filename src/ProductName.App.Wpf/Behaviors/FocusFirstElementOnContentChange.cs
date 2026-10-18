using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProductName.App.Wpf.Behaviors;

/// <summary>Moves keyboard focus into newly displayed content without putting view logic in code-behind.</summary>
public static class FocusFirstElementOnContentChange
{
    #region fields
    private static readonly DependencyPropertyDescriptor ContentDescriptor =
        DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(FocusFirstElementOnContentChange),
        new PropertyMetadata(false, OnIsEnabledChanged));
    #endregion
    #region public methods
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    #endregion
    #region private methods
    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ContentControl contentControl)
            return;

        if ((bool)args.NewValue)
            ContentDescriptor.AddValueChanged(contentControl, OnContentChanged);
        else
            ContentDescriptor.RemoveValueChanged(contentControl, OnContentChanged);
    }

    private static void OnContentChanged(object? sender, EventArgs args)
    {
        if (sender is not ContentControl contentControl)
            return;

        // Wait until the new template is loaded before looking for a focus target.
        contentControl.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!contentControl.IsLoaded || !contentControl.IsVisible)
                return;

            var target = FindFirstFocusableElement(contentControl);
            if (target is not null)
                Keyboard.Focus(target);
        }));
    }

    private static FrameworkElement? FindFirstFocusableElement(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is FrameworkElement element &&
                element.Focusable && element.IsEnabled && element.IsVisible &&
                KeyboardNavigation.GetIsTabStop(element))
            {
                return element;
            }

            var nested = FindFirstFocusableElement(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
    #endregion
}