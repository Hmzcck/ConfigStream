namespace ConfigStream.Core.Interfaces;

public interface IConfigurationReader
{
    T? GetValue<T>(string key);
    Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);
}