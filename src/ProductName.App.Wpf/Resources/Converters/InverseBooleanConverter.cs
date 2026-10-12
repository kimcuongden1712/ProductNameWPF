using System.Globalization;
using System.Windows.Data;

namespace ProductName.App.Wpf.Resources.Converters;

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : Binding.DoNothing;
}
