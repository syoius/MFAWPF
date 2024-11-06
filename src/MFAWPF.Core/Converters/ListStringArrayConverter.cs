using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class ListStringArrayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string[]> ls)
        {
            return new ObservableCollection<CustomValue<string>>(
                ls.Select(array => new CustomValue<string>($"[{string.Join(",", array)}]")).ToList()
            );
        }

        return new ObservableCollection<CustomValue<string>>();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> collection)
        {
            var result = collection.Select(customValue =>
            {
                var trimmed = customValue.Value?.Trim('[', ']');
                var splitArray = trimmed?.Split(",") ?? null;
                return splitArray?.Length == 2 ? splitArray : null;
            }).ToList();

            if (result.Any(array => array == null))
            {
                return BindingOperations.DoNothing;
            }

            return result;
        }

        return null;
    }
} 