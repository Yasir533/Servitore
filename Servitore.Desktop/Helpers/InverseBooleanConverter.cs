using System.Globalization;
using System.Windows.Data;

namespace Servitore.Desktop.Helpers;

/// <summary>
/// Returns the logical inverse of a bool. Used to disable controls while IsBusy is true.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
