using MFAWPF.Core.Converters;
using Newtonsoft.Json;

namespace MFAWPF.Core.Models;

public class Attribute
{
    public string? Key { get; set; }
    [JsonConverter(typeof(AutoConverter))] 
    public object? Value { get; set; }

    public Attribute(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public Attribute()
    {
    }

    private static string ConvertListToString(List<List<int>> listOfLists)
    {
        var formattedLists = listOfLists
            .Select(innerList => $"[{string.Join(",", innerList)}]");
        return string.Join(",", formattedLists);
    }

    public override string ToString()
    {
        return Value switch
        {
            List<List<int>> lli => $"\"{Key}\" : [{ConvertListToString(lli)}]",
            List<int> li => $"\"{Key}\" : [{string.Join(",", li)}]",
            List<string> ls => $"\"{Key}\" : [{string.Join(",", ls)}]",
            string s => $"\"{Key}\" : \"{s}\"",
            _ => $"\"{Key}\" : {Value}"
        };
    }

    public string GetKey() => Key ?? string.Empty;

    public string GetValue()
    {
        return Value switch
        {
            List<List<int>> lli => ConvertListToString(lli),
            List<int> li => string.Join(",", li),
            List<string> ls => string.Join(",", ls),
            string s => s,
            _ => Value?.ToString() ?? string.Empty
        };
    }

    public static bool operator ==(Attribute? a1, object? a2)
    {
        if (a2 is not Attribute attribute) return false;
        if (ReferenceEquals(a1, null)) return false;
        return a1.Key?.Equals(attribute.Key) == true && Equals(a1.Value, attribute.Value);
    }

    public static bool operator !=(Attribute? a1, object? a2)
    {
        return !(a1 == a2);
    }

    public override bool Equals(object? obj)
    {
        return this == obj;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }
} 