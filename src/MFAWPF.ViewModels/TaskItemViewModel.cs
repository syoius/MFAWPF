using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Core.Models;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public partial class TaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "未命名";

    private TaskModel? _task;

    public TaskModel? Task
    {
        get => _task;
        set
        {
            if (value != null)
                Name = value.Name;
            SetProperty(ref _task, value);
        }
    }

    partial void OnNameChanged(string value)
    {
        if (Task != null)
            Task.Name = value;
    }

    public override string ToString()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        Dictionary<string, TaskModel> taskModels = new();
        if (Task != null)
            taskModels.Add(Name, Task);
        return JsonConvert.SerializeObject(taskModels, settings);
    }
}