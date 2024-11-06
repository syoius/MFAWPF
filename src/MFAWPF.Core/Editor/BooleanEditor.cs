using Avalonia.Controls;

namespace MFAWPF.Core.Editor;

public class BooleanEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        return new CheckBox
        {
            IsThreeState = true
        };
    }
} 