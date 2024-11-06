using Newtonsoft.Json;
using MFAWPF.Core.Models;
using MFAWPF.Core.Converters;

namespace MFAWPF.Core.Models;

public class TaskInterfaceItem
{
    [JsonProperty("name")] 
    public string? Name { get; set; }

    [JsonProperty("entry")] 
    public string? Entry { get; set; }

    [JsonProperty("doc")] 
    public string? Document { get; set; }

    [JsonProperty("check")] 
    public bool? Check { get; set; }

    [JsonProperty("repeatable")] 
    public bool? Repeatable { get; set; }

    [JsonProperty("repeat_count")] 
    public int? RepeatCount { get; set; }

    [JsonProperty("option")] 
    public List<MaaInterface.MaaInterfaceSelectOption>? Option { get; set; }

    [JsonProperty("pipeline_override")] 
    public Dictionary<string, TaskModel>? PipelineOverride { get; set; }

    public override string ToString()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        return JsonConvert.SerializeObject(this, settings);
    }

    public TaskInterfaceItem Clone()
    {
        return JsonConvert.DeserializeObject<TaskInterfaceItem>(ToString()) ?? new TaskInterfaceItem();
    }
} 