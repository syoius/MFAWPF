using System.Globalization;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class MultiBoolAndOrConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValues = values.OfType<bool>().ToArray();

        if (parameter is string operation && operation.Equals("Or", StringComparison.OrdinalIgnoreCase))
        {
            return boolValues.Any(v => v); // 逻辑或
        }

        return boolValues.All(v => v); // 逻辑与
    }

    public IList<object?> ConvertBack(object? value, IList<Type> targetTypes, object? parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}