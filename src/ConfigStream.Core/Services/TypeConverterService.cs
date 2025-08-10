using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using System.Text.Json;

namespace ConfigStream.Core.Services;

public class TypeConverterService : ITypeConverterService
{
    public T Convert<T>(string value, ConfigurationType type)
    {
        return (T)Convert(value, type, typeof(T));
    }

    public object Convert(string value, ConfigurationType type, Type targetType)
    {
        return type switch
        {
            ConfigurationType.String => value,
            ConfigurationType.Number => ConvertToNumber(value, targetType),
            ConfigurationType.Boolean => bool.Parse(value),
            ConfigurationType.Json => JsonSerializer.Deserialize(value, targetType)!,
            _ => throw new NotSupportedException($"Unsupperted type: {type}")
        };
    }

    private static object ConvertToNumber(string value, Type targetType)
    {
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(long)) return long.Parse(value);
        if (targetType == typeof(double)) return double.Parse(value);
        if (targetType == typeof(decimal)) return decimal.Parse(value);
        if (targetType == typeof(float)) return float.Parse(value);

        throw new InvalidCastException($"Cannot convert to {targetType}");
    }
}
