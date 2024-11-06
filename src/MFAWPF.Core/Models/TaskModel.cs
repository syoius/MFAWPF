using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Core.Converters;
using MFAWPF.Core.Editor;
using Newtonsoft.Json;

namespace MFAWPF.Core.Models;

public class TaskModel : ObservableObject
{
    // 保持所有私有字段不变
    private string _name = "未命名";
    private string? _recognition;
    // ... 其他字段保持不变 ...

    [Browsable(false)]
    [JsonIgnore]
    [JsonProperty("name")]
    public string Name
    {
        get => _name;
        set => SetNewProperty(ref _name, value);
    }

    [JsonProperty("recognition")]
    [Category("基础属性")]
    [Description(
        "识别算法类型。可选，默认 DirectHit 。\n可选的值：DirectHit | TemplateMatch | FeatureMatch | ColorMatch | OCR | NeuralNetworkClassify | NeuralNetworkDetect | Custom")]
    [Editor(typeof(StringComboBoxPropertyEditor), typeof(StringComboBoxPropertyEditor))]
    public string? Recognition
    {
        get => _recognition;
        set => SetNewProperty(ref _recognition, value);
    }

    // ... 其他属性保持类似的修改模式 ...

    [Editor(typeof(BooleanEditor))]
    public bool? Focus { get; set; }

    [Editor(typeof(ListStringEditor))]
    public List<string>? FocusTip { get; set; }

    [Editor(typeof(ListStringEditor))]
    public List<string>? FocusTipColor { get; set; }

    // 工具方法保持不变
    public TaskModel Set(object properties) { /* ... */ }
    public TaskModel Set(Dictionary<string, object?> properties) { /* ... */ }
    public TaskModel Set(params Attribute[] attributes) { /* ... */ }
    public void Merge(TaskModel other) { /* ... */ }
    public TaskModel Reset() { /* ... */ }
    protected bool SetNewProperty<T>(ref T field, T newValue, string? propertyName = null) { /* ... */ }
    public List<Attribute> ToAttributeList() { /* ... */ }
} 