// MongoDB initialization script
// This runs when the container starts for the first time

// Switch to the DynamicConfiguration database
db = db.getSiblingDB('DynamicConfiguration');

// Create a user for the application
db.createUser({
  user: 'configstream',
  pwd: 'configstream123',
  roles: [
    {
      role: 'readWrite',
      db: 'DynamicConfiguration'
    }
  ]
});

// Create collections with indexes
db.createCollection('configurations');

// Create indexes for better performance
db.configurations.createIndex(
  { "applicationName": 1, "name": 1, "isActive": 1 },
  { name: "AppName_Name_IsActive" }
);

db.configurations.createIndex(
  { "applicationName": 1, "name": "text" }
);

// Insert sample data for testing
db.configurations.insertMany([
  {
    _id: ObjectId(),
    name: "SiteName",
    applicationName: "ConfigurationLibrary.Mvc.Web",
    value: "ConfigStream Demo Site",
    type: 1, // String
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "MaxConnections",
    applicationName: "ConfigurationLibrary.Mvc.Web", 
    value: "100",
    type: 2, // Number
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "IsFeatureEnabled",
    applicationName: "ConfigurationLibrary.Mvc.Web",
    value: "true", 
    type: 4, // Boolean
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "DatabaseTimeout",
    applicationName: "SERVICE-A",
    value: "30",
    type: 2, // Number
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "LogLevel",
    applicationName: "SERVICE-A",
    value: "Information",
    type: 1, // String
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "CacheEnabled",
    applicationName: "SERVICE-B",
    value: "true",
    type: 4, // Boolean
    isActive: 1
  },
  {
    _id: ObjectId(),
    name: "RetryCount",
    applicationName: "SERVICE-B",
    value: "3",
    type: 2, // Number
    isActive: 1
  }
]);

print('MongoDB initialization completed successfully!');