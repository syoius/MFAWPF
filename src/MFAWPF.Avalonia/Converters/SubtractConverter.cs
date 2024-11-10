using System.Globalization;
using Avalonia.Data.Converters;

namespace MFAWPF.Avalonia.Converters;

public class SubtractConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double mainValue && parameter is double subtractValue)
        {
            return mainValue - subtractValue;
        }

        // 如果参数是字符串,尝试解析为double
        if (value is double val && parameter is string paramStr)
        {
            if (double.TryParse(paramStr, out double subtractVal))
            {
                return val - subtractVal;
            }
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 通常不需要反向转换
        throw new NotImplementedException();
    }
}
