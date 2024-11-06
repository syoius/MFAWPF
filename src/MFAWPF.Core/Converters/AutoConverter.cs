using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Core.Converters;

public class AutoConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Array => token.ToObject<List<object>>(),
            JTokenType.Integer => token.ToObject<int>(),
            JTokenType.Float => token.ToObject<double>(),
            JTokenType.String => token.ToObject<string>(),
            JTokenType.Boolean => token.ToObject<bool>(),
            JTokenType.Null => null,
            _ => token.ToObject<object>()
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var token = JToken.FromObject(value ?? new object());
        token.WriteTo(writer);
    }
} 