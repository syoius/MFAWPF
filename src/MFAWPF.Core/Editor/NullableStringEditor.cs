using Avalonia.Controls;
using Avalonia.Data;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class NullableStringEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        var textBox = new TextBox
        {
            IsReadOnly = propertyItem.IsReadOnly,
            UseFloatingWatermark = true,
            ClearButtonEnabled = true
        };

        return textBox;
    }

    protected virtual IValueConverter GetConverter()
    {
        return new NullableStringConverter();
    }
}