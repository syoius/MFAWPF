using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using MFAWPF.Core.Controls;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Editor;

public class ListStringEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        var listBox = new ListBox
        {
            MinHeight = 50,
            ItemsSource = GetItemsSource(propertyItem)
        };

        var panel = new StackPanel
        {
            Spacing = 4
        };

        var addButton = new Button
        {
            Content = "添加"
        };

        var removeButton = new Button
        {
            Content = "移除"
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children =
            {
                addButton,
                removeButton
            }
        };

        panel.Children.Add(listBox);
        panel.Children.Add(buttonPanel);

        return panel;
    }

    protected virtual IValueConverter GetConverter()
    {
        return new ListStringConverter();
    }

    private IEnumerable<string>? GetItemsSource(PropertyItem propertyItem)
    {
        return propertyItem.PropertyName switch
        {
            "Roi" or "Next" or "OnError" or "Interrupt"
                => MainViewModel.Instance?.TaskItems?.Select(x => x.Name),
            "FocusTipColor" => GetColorNames(),
            _ => new List<string>()
        };
    }

    private static IEnumerable<string> GetColorNames()
    {
        return typeof(Avalonia.Media.Colors)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(Avalonia.Media.Color))
            .Select(p => p.Name);
    }
} 