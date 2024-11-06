using Avalonia.Controls;
using Avalonia.Data;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class StringComboBoxEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        var comboBox = new ComboBox
        {
            IsEditable = false,
            ItemsSource = GetItemsSource(propertyItem)
        };

        comboBox.SelectionChanged += (_, _) => 
        { 
            propertyItem.Value = comboBox.SelectedItem; 
        };

        return comboBox;
    }

    private IEnumerable<string> GetItemsSource(PropertyItem propertyItem)
    {
        return propertyItem.PropertyName switch
        {
            "Recognition" => new[]
            {
                "",
                "DirectHit",
                "TemplateMatch",
                "FeatureMatch",
                "ColorMatch",
                "OCR",
                "NeuralNetworkClassify",
                "NeuralNetworkDetect",
                "Custom"
            },
            "Action" => new[]
            {
                "",
                "DoNothing",
                "Click",
                "Swipe",
                "Key",
                "Text",
                "StartApp",
                "StopApp",
                "StopTask",
                "Custom"
            },
            "OrderBy" => new[]
            {
                "",
                "Horizontal",
                "Vertical",
                "Score",
                "Random",
                "Area",
                "Length"
            },
            "Detector" => new[]
            {
                "",
                "SIFT",
                "KAZE",
                "AKAZE",
                "BRISK",
                "ORB"
            },
            _ => new[] { "" }
        };
    }

    protected virtual IValueConverter GetConverter()
    {
        return new NullStringConverter();
    }
} 