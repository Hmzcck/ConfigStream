using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

public interface ITypeConverterService
{
    T Convert<T>(string value, ConfigurationType type);
    object Convert(string value, ConfigurationType type, Type targetType);
}
