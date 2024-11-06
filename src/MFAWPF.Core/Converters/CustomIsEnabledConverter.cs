using System.Globalization;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class CustomIsEnabledConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 &&
            values[0] is bool isChecked &&
            values[1] is bool idle)
        {
            return (isChecked && idle) || !isChecked;
        }

        return false;
    }

    public IList<object?> ConvertBack(object? value, IList<Type> targetTypes, object? parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}