using System.Collections;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Data;
using MFAWPF.Core.Controls;
using MFAWPF.Core.Converters;
using MFAWPF.Core.ViewModels;

namespace MFAWPF.Core.Editor;

public class SingleIntListOrAutoEditor : PropertyEditorBase
{
    public override Control CreateControl(PropertyItem propertyItem)
    {
        var ctrl = new AutoCompleteBox
        {
            IsReadOnly = propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
            DisplayMemberPath = GetDisplayMemberPath(propertyItem),
            ClearButtonEnabled = true,
            UseFloatingWatermark = true
        };

        return ctrl;
    }

    public static List<string> AutoProperty()
    {
        return ["Roi", "Begin", "End", "Target"];
    }

    private IEnumerable? GetItemsSource(PropertyItem propertyItem)
    {
        if (AutoProperty().Contains(propertyItem.PropertyName))
        {
            var originalDataList = MainViewModel.Instance?.TaskItems;
            if (originalDataList != null)
            {
                var newDataList = new ObservableCollection<TaskItemViewModel>(originalDataList);
                if (propertyItem.PropertyName != "Roi")
                    newDataList.Add(new TaskItemViewModel { Name = "True" });
                return newDataList;
            }
            return null;
        }
        return new ObservableCollection<string>();
    }

    private string GetDisplayMemberPath(PropertyItem propertyItem)
    {
        return AutoProperty().Contains(propertyItem.PropertyName) ? "Name" : string.Empty;
    }

    protected override IValueConverter GetConverter()
    {
        return new SingleIntListOrAutoConverter();
    }
}