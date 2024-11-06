using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class SingleIntListConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;
        if (value is List<int> li)
        {
            return string.Join(",", li);
        }
        return value.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var strValue = value as string;
        if (string.IsNullOrWhiteSpace(strValue))
            return null;

        try
        {
            var result = strValue
                .Split(',')
                .Select(int.Parse)
                .ToList();
            return result;
        }
        catch
        {
            return BindingOperations.DoNothing;
        }
    }
}