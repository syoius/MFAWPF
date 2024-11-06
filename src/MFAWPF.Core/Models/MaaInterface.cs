using Newtonsoft.Json;
using MFAWPF.Core.Converters;
using MFAWPF.Core.Models.Tasks;
using MFAWPF.Core.Services;

namespace MFAWPF.Core.Models;

public class MaaInterface
{
    public class MaaInterfaceOptionCase
    {
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("pipeline_override")] public Dictionary<string, TaskModel>? PipelineOverride { get; set; }

        public override string ToString()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(PipelineOverride, settings);
        }
    }

    public class MaaInterfaceOption
    {
        [JsonIgnore] public string Name { get; set; } = string.Empty;
        [JsonProperty("cases")] public List<MaaInterfaceOptionCase>? Cases { get; set; }
    }

    public class MaaInterfaceSelectOption
    {
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("index")] public int? Index { get; set; }

        public override string ToString() => Name ?? string.Empty;
    }

    public class CustomExecutor
    {
        [JsonIgnore] public string? Name { get; set; }
        [JsonProperty("exec_path")] public string? ExecPath { get; set; }

        [JsonConverter(typeof(SingleOrListConverter))]
        [JsonProperty("exec_param")]
        public List<string>? ExecParam { get; set; }
    }

    public class MaaCustomResource
    {
        [JsonProperty("name")] public string? Name { get; set; }

        [JsonConverter(typeof(SingleOrListConverter))]
        [JsonProperty("path")]
        public List<string>? Path { get; set; }
    }

    public class MaaResourceVersion
    {
        [JsonProperty("name")] public string? Name { get; set; }
        [JsonProperty("version")] public string? Version { get; set; }
        [JsonProperty("url")] public string? Url { get; set; }

        public override string ToString() => Version ?? string.Empty;
    }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("version")]
    [JsonConverter(typeof(MaaResourceVersionConverter))]
    public string? Version { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("custom_title")]
    public string? CustomTitle { get; set; }

    [JsonProperty("default_controller")]
    public string? DefaultController { get; set; }

    [JsonProperty("lock_controller")]
    public bool? LockController { get; set; }

    [JsonProperty("resource")]
    public List<MaaCustomResource>? Resource { get; set; }
    
    [JsonProperty("task")]
    public List<TaskInterfaceItem>? Task { get; set; }
    
    [JsonProperty("recognition")]
    public Dictionary<string, CustomExecutor>? Recognition { get; set; }
    
    [JsonProperty("action")]
    public Dictionary<string, CustomExecutor>? Action { get; set; }
    
    [JsonProperty("option")]
    public Dictionary<string, MaaInterfaceOption>? Option { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    private static MaaInterface? _instance;
    public static event EventHandler? InstanceChanged;

    [JsonIgnore] 
    public Dictionary<string, List<string>> Resources { get; } = new();

    public static string ReplacePlaceholder(string? input, string replacement)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : input.Replace("{PROJECT_DIR}", replacement);
    }

    public static List<string> ReplacePlaceholder(List<string>? inputs, string replacement)
    {
        if (inputs == null) return new List<string>();
        return inputs.ConvertAll(input => ReplacePlaceholder(input, replacement));
    }

    public static MaaInterface? Instance
    {
        get => _instance;
        set
        {
            _instance = value;
            if (value == null) return;

            _instance?.Resources.Clear();

            if (value.Resource != null)
            {
                foreach (var customResource in value.Resource)
                {
                    var paths = ReplacePlaceholder(customResource.Path ?? new List<string>(),
                        AppDomain.CurrentDomain.BaseDirectory);
                    if (_instance != null)
                        _instance.Resources[customResource.Name ?? string.Empty] = paths;
                }
            }

            // 通知界面更新
            InstanceChanged?.Invoke(null, EventArgs.Empty);
        }
    }

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
} 