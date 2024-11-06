using CommunityToolkit.Mvvm.ComponentModel;

namespace MFAWPF.Core.Models;

public class CustomValue<T> : ObservableObject
{
    public CustomValue(T value)
    {
        Value = value;
    }

    private T? _value;

    public T? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}