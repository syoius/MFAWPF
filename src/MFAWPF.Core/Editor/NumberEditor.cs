using Avalonia.Controls;

namespace MFAWPF.Core.Editor;

public class NumberEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        return new NumericUpDown
        {
            Minimum = 0,
            Maximum = int.MaxValue,
            FormatString = "N0"
        };
    }
} 