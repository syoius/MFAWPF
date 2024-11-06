using System.Globalization;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class SubtractConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double originalWidth && parameter is string parameterString &&
            double.TryParse(parameterString, out double subtractValue))
        {
            return originalWidth - subtractValue;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported in SubtractConverter");
    }
}