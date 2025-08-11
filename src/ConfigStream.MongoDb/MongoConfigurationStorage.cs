using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Logging;
using ConfigStream.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConfigStream.MongoDb;

public class MongoConfigurationStorage : IConfigurationStorage, IDisposable
{
    private static readonly ILogger<MongoConfigurationStorage> _logger =
        Logging.CreateLogger<MongoConfigurationStorage>();

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
            _logger.LogInformation("Initializing MongoDB connection for database '{DatabaseName}'", databaseName);
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
            _collection = _database.GetCollection<ConfigurationItem>("configurations");
            _logger.LogInformation("MongoDB connection initialized successfully for database '{DatabaseName}'",
                databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB database '{DatabaseName}'", databaseName);
            throw new InvalidOperationException($"Failed to connect to MongoDB: {ex.Message}", ex);
        }

        _ = Task.Run(CreateIndexes);
    }

    public async Task<ConfigurationItem?> GetAsync(string applicationName, string key,
        CancellationToken cancellationToken = default)
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

            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (result != null)
            {
                _logger.LogDebug("Configuration '{Key}' found for application '{ApplicationName}'", key,
                    applicationName);
            }
            else
            {
                _logger.LogDebug("Configuration '{Key}' not found for application '{ApplicationName}'", key,
                    applicationName);
            }

            return result;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB query failed for configuration '{Key}' in application '{ApplicationName}'",
                key, applicationName);
            throw new InvalidOperationException(
                $"Failed to retrieve configuration '{key}' for application '{applicationName}': {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllAsync(string applicationName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, 1)
            );

            IAsyncCursor<ConfigurationItem> cursor =
                await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            var result = await cursor.ToListAsync(cancellationToken);

            return result;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex,
                "MongoDB query failed when retrieving all configurations for application '{ApplicationName}'",
                applicationName);
            throw new InvalidOperationException(
                $"Failed to retrieve configurations for application '{applicationName}': {ex.Message}", ex);
        }
    }


    public async Task<ConfigurationItem?> SetAsync(ConfigurationItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, item.ApplicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, item.Name)
            );

            ConfigurationItem existingRecord = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (existingRecord != null)
            {
                item.Id = existingRecord.Id;
                ReplaceOneResult result =
                    await _collection.ReplaceOneAsync(filter, item, cancellationToken: cancellationToken);

                if (result.IsAcknowledged)
                {
                    _logger.LogInformation(
                        "Configuration '{Key}' updated successfully for application '{ApplicationName}'", item.Name,
                        item.ApplicationName);
                    return item;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to update configuration '{Key}' for application '{ApplicationName}' - operation not acknowledged",
                        item.Name, item.ApplicationName);
                    return null;
                }
            }

            item.Id = ObjectId.GenerateNewId().ToString();

            await _collection.InsertOneAsync(item, cancellationToken: cancellationToken);

            _logger.LogInformation("Configuration '{Key}' created successfully for application '{ApplicationName}'",
                item.Name, item.ApplicationName);
            return item;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex,
                "MongoDB operation failed when saving configuration '{Key}' for application '{ApplicationName}'",
                item.Name, item.ApplicationName);
            throw new InvalidOperationException(
                $"Failed to save configuration '{item.Name}' for application '{item.ApplicationName}': {ex.Message}",
                ex);
        }
    }

    public async Task<bool> DeleteAsync(string applicationName, string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            _logger.LogDebug("Deleting (deactivating) configuration '{Key}' for application '{ApplicationName}'", key,
                applicationName);

            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.And(
                Builders<ConfigurationItem>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationItem>.Filter.Eq(x => x.Name, key)
            );

            UpdateDefinition<ConfigurationItem> update = Builders<ConfigurationItem>.Update
                .Set(x => x.IsActive, 0);

            UpdateResult result =
                await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            var success = result.IsAcknowledged && result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogInformation(
                    "Configuration '{Key}' deactivated successfully for application '{ApplicationName}'", key,
                    applicationName);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to deactivate configuration '{Key}' for application '{ApplicationName}' - no records modified",
                    key, applicationName);
            }

            return success;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex,
                "MongoDB operation failed when deleting configuration '{Key}' for application '{ApplicationName}'", key,
                applicationName);
            throw new InvalidOperationException(
                $"Failed to delete configuration '{key}' for application '{applicationName}': {ex.Message}", ex);
        }
    }

    private async Task CreateIndexes()
    {
        try
        {
            // Primary index for queries
            IndexKeysDefinition<ConfigurationItem> indexKeys = Builders<ConfigurationItem>.IndexKeys
                .Ascending(x => x.ApplicationName)
                .Ascending(x => x.Name)
                .Ascending(x => x.IsActive);

            CreateIndexOptions indexOptions = new() { Name = "AppName_Name_IsActive", Background = true };

            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ConfigurationItem>(indexKeys, indexOptions));

            // Text search index
            IndexKeysDefinition<ConfigurationItem> prefixIndexKeys = Builders<ConfigurationItem>.IndexKeys
                .Ascending(x => x.ApplicationName)
                .Text(x => x.Name);

            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ConfigurationItem>(prefixIndexKeys));

            _logger.LogInformation("MongoDB indexes created successfully for configurations collection");
        }
        catch (MongoException ex)
        {
            _logger.LogWarning(ex,
                "Failed to create MongoDB indexes - operations will continue without optimized indexes");
        }
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            FilterDefinition<ConfigurationItem> filter = Builders<ConfigurationItem>.Filter.Eq(x => x.IsActive, 1);
            IAsyncCursor<ConfigurationItem> cursor =
                await _collection.FindAsync(filter, cancellationToken: cancellationToken);
            var result = await cursor.ToListAsync(cancellationToken);

            return result;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB query failed when retrieving all configurations from all applications");
            throw new InvalidOperationException(
                "Failed to retrieve all configurations from all applications: " + ex.Message, ex);
        }
    }

    public async Task<IEnumerable<string>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use MongoDB aggregation to get distinct application names
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("isActive", 1)),
                new BsonDocument("$group", new BsonDocument("_id", "$applicationName")),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var cursor = await _collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
            var results = await cursor.ToListAsync(cancellationToken);

            var applications = results.Select(doc => doc["_id"].AsString).ToList();

            return applications;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB query failed when retrieving applications list");
            throw new InvalidOperationException("Failed to retrieve applications list: " + ex.Message, ex);
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing MongoDB client connection");
        _client.Dispose();
    }
}
