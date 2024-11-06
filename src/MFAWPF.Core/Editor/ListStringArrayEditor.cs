using Avalonia.Data.Converters;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class ListStringArrayEditor : ListStringEditor
{
    protected override IValueConverter GetConverter()
    {
        return new ListStringArrayConverter();
    }
} 