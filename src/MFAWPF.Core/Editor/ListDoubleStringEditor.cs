using Avalonia.Data.Converters;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class ListDoubleStringEditor : ListStringEditor
{
    protected override IValueConverter GetConverter()
    {
        return new ListDoubleStringConverter();
    }
} 