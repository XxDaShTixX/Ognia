using System.Globalization;
using System.Windows.Data;

namespace Ognia.Frontend.Converters;

public class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TimeSpan ts ? ts.TotalSeconds : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double seconds ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
}