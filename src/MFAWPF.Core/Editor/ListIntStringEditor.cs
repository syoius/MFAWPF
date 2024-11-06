using Avalonia.Data.Converters;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class ListIntStringEditor : ListStringEditor
{
    protected override IValueConverter GetConverter()
    {
        return new ListIntStringConverter();
    }
} 