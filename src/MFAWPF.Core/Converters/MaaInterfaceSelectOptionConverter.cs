using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Core.Converters;

public class MaaInterfaceSelectOptionConverter : JsonConverter
{
    private readonly bool _serializeAsStringArray;

    public MaaInterfaceSelectOptionConverter(bool serializeAsStringArray)
    {
        _serializeAsStringArray = serializeAsStringArray;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(List<MaaInterface.MaaInterfaceSelectOption>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Array => HandleArrayToken(token, serializer),
            JTokenType.String => HandleStringToken(token, serializer),
            JTokenType.None => null,
            _ => HandleInvalidToken(objectType)
        };
    }

    private static object? HandleArrayToken(JToken token, JsonSerializer serializer)
    {
        var firstElement = token.First;
        if (firstElement?.Type == JTokenType.String)
        {
            return token.Select(item => new MaaInterface.MaaInterfaceSelectOption
            {
                Name = item.ToString(),
                Index = 0
            }).ToList();
        }

        return firstElement?.Type == JTokenType.Object 
            ? token.ToObject<List<MaaInterface.MaaInterfaceSelectOption>>(serializer) 
            : null;
    }

    private static object? HandleStringToken(JToken token, JsonSerializer serializer)
    {
        string? oName = token.ToObject<string>(serializer);
        return new List<MaaInterface.MaaInterfaceSelectOption>
        {
            new() { Name = oName ?? "", Index = 0 }
        };
    }

    private static object? HandleInvalidToken(Type objectType)
    {
        Console.WriteLine($"Invalid JSON format for MaaInterfaceSelectOptionConverter. Unexpected type {objectType}.");
        return null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not List<MaaInterface.MaaInterfaceSelectOption> selectOptions)
            return;

        var array = new JArray();
        foreach (var option in selectOptions)
        {
            if (_serializeAsStringArray)
            {
                array.Add(option.Name);
            }
            else
            {
                array.Add(new JObject
                {
                    ["name"] = option.Name,
                    ["index"] = option.Index ?? 0
                });
            }
        }
        array.WriteTo(writer);
    }
}