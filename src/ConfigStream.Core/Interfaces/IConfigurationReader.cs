namespace ConfigStream.Core.Interfaces;

public interface IConfigurationReader
{
    T? GetValue<T>(string key);
}