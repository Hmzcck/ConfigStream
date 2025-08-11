using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConfigStream.MongoDb;

public class MongoConfigurationStorage : IConfigurationStorage, IDisposable
{
    private readonly IMongoCollection<ConfigurationItem> _collection;
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoConfigurationStorage(string connectionString, string databaseName = "DynamicConfiguration")
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

        try
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<ConfigurationItem>("configurations");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to MongoDB: {ex.Message}", ex);
        }

        _ = Task.Run(CreateIndexes);
    }

    public async Task<ConfigurationItem?> GetAsync(string applicationName, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, key),
                Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, 1)
            );

            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve configuration '{key}' for application '{applicationName}': {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllAsync(string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, 1)
            );

            IAsyncCursor<ConfigurationItem> cursor = await _collection.FindAsync(filter);
            return await cursor.ToListAsync();
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve configurations for application '{applicationName}': {ex.Message}", ex);
        }
    }


    public async Task<ConfigurationItem?> SetAsync(ConfigurationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, item.ApplicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, item.Name)
            );

            ConfigurationItem existingRecord = await _collection.Find(filter).FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                item.Id = existingRecord.Id;
                ReplaceOneResult result = await _collection.ReplaceOneAsync(filter, item);
                return result.IsAcknowledged ? item : null;
            }

            item.Id = ObjectId.GenerateNewId().ToString();
            await _collection.InsertOneAsync(item);
            return item;
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to save configuration '{item.Name}' for application '{item.ApplicationName}': {ex.Message}",
                ex);
        }
    }

    public async Task<bool> DeleteAsync(string applicationName, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, key)
            );

            UpdateDefinition<ConfigurationItem> update = Builders<ConfigurationItem>.Update
                .Set(x => x.IsActive, 0);

            UpdateResult result = await _collection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to delete configuration '{key}' for application '{applicationName}': {ex.Message}", ex);
        }
    }

    private async Task CreateIndexes()
    {
        IndexKeysDefinition<ConfigurationItem> indexKeys = Builders<ConfigurationItem>.IndexKeys
            .Ascending(x => x.ApplicationName)
            .Ascending(x => x.Name)
            .Ascending(x => x.IsActive);

        CreateIndexOptions indexOptions = new() { Name = "AppName_Name_IsActive", Background = true };

        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ConfigurationItem>(indexKeys, indexOptions));

        IndexKeysDefinition<ConfigurationItem> prefixIndexKeys = Builders<ConfigurationItem>.IndexKeys
            .Ascending(x => x.ApplicationName)
            .Text(x => x.Name);

        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ConfigurationItem>(prefixIndexKeys));
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
