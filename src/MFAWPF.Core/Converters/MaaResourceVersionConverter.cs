using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MFAWPF.Core.Converters;

public class MaaResourceVersionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string) || objectType == typeof(MaaInterface.MaaResourceVersion);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        var res = token.ToString();
        if (res.Contains('{') || res.Contains('}'))
        {
            var version = token.ToObject<MaaInterface.MaaResourceVersion>(serializer);
            return version?.Version;
        }
        return res;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        switch (value)
        {
            case string strValue:
                writer.WriteValue(strValue);
                break;
            case MaaInterface.MaaResourceVersion mrv:
                writer.WriteValue(mrv.Version);
                break;
        }
    }
}