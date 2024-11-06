using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MFAWPF.Core.Converters;

public class ListDoubleStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new ObservableCollection<CustomValue<string>>
            {
                new(d.ToString(CultureInfo.InvariantCulture))
            };
        }

        if (value is IEnumerable<double> doubles)
        {
            return new ObservableCollection<CustomValue<string>>(
                doubles.Select(dx => new CustomValue<string>(dx.ToString(CultureInfo.InvariantCulture))));
        }

        return new ObservableCollection<CustomValue<string>>();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CustomValue<string>> customValueList)
        {
            var list = customValueList.ToList();
            try
            {
                var result = list
                    .Select(cv => double.Parse(cv.Value ?? string.Empty))
                    .ToList();
                if (result.Count == 1)
                    return result[0];
                return result.Count > 0 ? result : null;
            }
            catch
            {
                return BindingOperations.DoNothing;
            }
        }
        return null;
    }
} 