using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConfigStream.MongoDb;

public class MongoConfigurationStorage : IConfigurationStorage
{
    private readonly IMongoCollection<ConfigurationItem> _collection;
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoConfigurationStorage(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        try
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase("DynamicConfiguration");
            _collection = _database.GetCollection<ConfigurationItem>("configurations");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to MongoDB: {ex.Message}", ex);
        }
    }

    public async Task<ConfigurationItem?> GetAsync(string applicationName, string key)
    {
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

    public async Task<bool> DeleteAsync(string applicationName, string key)
    {
        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, key)
            );

            DeleteResult result = await _collection.DeleteOneAsync(filter);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to delete configuration '{key}' for application '{applicationName}': {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllAsync(string applicationName)
    {
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
        if (item == null)
            throw new ArgumentNullException(nameof(item));

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
            else
            {
                item.Id = ObjectId.GenerateNewId().ToString();
                await _collection.InsertOneAsync(item);
                return item;
            }
        }
        catch (MongoException ex)
        {
            throw new InvalidOperationException(
                $"Failed to save configuration '{item.Name}' for application '{item.ApplicationName}': {ex.Message}",
                ex);
        }
    }
}
