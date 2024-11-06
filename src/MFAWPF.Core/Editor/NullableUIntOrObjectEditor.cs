using Avalonia.Data.Converters;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class NullableUIntOrObjectEditor : NullableStringEditor
{
    protected override IValueConverter GetConverter()
    {
        return new NullableUIntOrObjectConverter();
    }
}