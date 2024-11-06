using Avalonia.Data.Converters;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class SingleIntListEditor : NullableStringEditor
{
    protected override IValueConverter GetConverter()
    {
        return new SingleIntListConverter();
    }
}