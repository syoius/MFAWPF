using Avalonia.Controls;

namespace MFAWPF.Core.Editor;

public abstract class PropertyEditorBase
{
    public abstract Control CreateControl(PropertyItem propertyItem);
} 