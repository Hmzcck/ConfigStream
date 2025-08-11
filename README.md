# ConfigStream - Dynamic Configuration Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![MongoDB](https://img.shields.io/badge/MongoDB-8.0-green.svg)](https://www.mongodb.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A powerful, scalable dynamic configuration management system built with .NET 8 that allows you to manage application configurations centrally without requiring deployments or restarts.

## 🚀 Features

### Core Functionality
- ✅ **Dynamic Configuration Management** - Update configurations without application restarts
- ✅ **Multi-Application Support** - Manage configurations for multiple applications from one place
- ✅ **Type Safety** - Support for String, Number, Boolean, and JSON configuration types
- ✅ **Real-time Updates** - Configurable refresh intervals for automatic configuration updates
- ✅ **Offline Resilience** - File-based fallback when database is unavailable

### Architecture
- ✅ **Layered Caching** - MongoDB → File Cache for high availability
- ✅ **Modular Design** - Separate packages for Core, MongoDB integrations
- ✅ **Async/Await** - Full asynchronous support throughout the system
- ✅ **Comprehensive Logging** - Structured logging with configurable levels
- ✅ **RESTful API** - Complete REST API for external integrations

### Web Interface
- ✅ **Simple UI** - Clean, responsive web interface for configuration management
- ✅ **Live Search & Filtering** - Filter configurations by application or name
- ✅ **CRUD Operations** - Create, read, update, and delete configurations via UI
- ✅ **Configuration Testing** - Built-in configuration reader testing functionality

## 📁 Project Structure

```
ConfigStream/
├── src/
│   ├── ConfigStream.Core/           # Core interfaces and services
│   │   ├── Interfaces/             # Contract definitions
│   │   ├── Models/                 # Data models
│   │   ├── Services/              # Core business logic
│   │   └── Logging/               # Centralized logging
│   ├── ConfigStream.MongoDb/       # MongoDB integration
│   │   ├── Services/              # MongoDB storage implementation
│   │   └── Extensions/            # DI registration extensions
│   └── ConfigStream.Mvc.Web/      # Web application & API
│       ├── Controllers/           # API controllers
│       ├── Views/                 # MVC views
│       └── wwwroot/               # Static assets
├── docker/                        # Docker initialization scripts
├── Dockerfile                     # Application container
├── docker-compose.yml            # Multi-service orchestration
└── README.md                      # This file
```

## 🎯 Quick Start

### Option 1: Docker Compose (Recommended)

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd ConfigStream
   ```

2. **Start the entire system**:
   ```bash
   docker-compose up -d
   ```

3. **Access the application**:
   - **Web Interface**: http://localhost:8080
   - **Health Check**: http://localhost:8080/health
   - **API Base**: http://localhost:8080/api/configuration

### Option 2: Manual Development Setup

**Prerequisites:**
- .NET 8 SDK
- MongoDB (local or cloud)

**Steps:**
1. **Clone and restore packages**:
   ```bash
   git clone <repository-url>
   cd ConfigStream
   dotnet restore
   ```

2. **Update connection strings** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "MongoDB": "mongodb://localhost:27017"
     }
   }
   ```

3. **Run the application**:
   ```bash
   cd src/ConfigStream.Mvc.Web
   dotnet run
   ```

4. **Access**: http://localhost:5095

## 📊 Sample Data

The system comes with pre-loaded sample configurations:

| Application | Configuration | Type | Value |
|------------|---------------|------|-------|
| ConfigurationLibrary.Mvc.Web | SiteName | String | "ConfigStream Demo Site" |
| ConfigurationLibrary.Mvc.Web | MaxConnections | Number | 100 |
| ConfigurationLibrary.Mvc.Web | IsFeatureEnabled | Boolean | true |
| SERVICE-A | DatabaseTimeout | Number | 30 |
| SERVICE-A | LogLevel | String | "Information" |
| SERVICE-B | CacheEnabled | Boolean | true |
| SERVICE-B | RetryCount | Number | 3 |

## 🔧 Usage Examples

### Basic Configuration Reader Usage

```csharp
// Initialize the configuration reader
var configReader = new ConfigurationReader(
    applicationName: "MyApplication",
    connectionString: "mongodb://localhost:27017",
    refreshTimerIntervalInMs: 30000 // 30 seconds
);

// Get configuration values with type safety
string siteName = configReader.GetValue<string>("SiteName");
int maxConnections = configReader.GetValue<int>("MaxConnections");
bool featureEnabled = configReader.GetValue<bool>("IsFeatureEnabled");

// Async version (recommended)
string asyncValue = await configReader.GetValueAsync<string>("AsyncKey");
```

### Dependency Injection Setup

```csharp
// In Program.cs
builder.Services.AddMongoDbStorage(
    builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017"
);

builder.Services.AddSingleton<IConfigurationReader>(sp =>
    new ConfigurationReader(
        applicationName: "MyApplication",
        connectionString: builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017",
        refreshTimerIntervalInMs: 30000
    )
);
```

### REST API Usage

```bash
# Get all applications
curl GET http://localhost:8080/api/configuration/applications

# Get all configurations for an application
curl GET "http://localhost:8080/api/configuration?applicationName=SERVICE-A"

# Get all configurations from all applications
curl GET http://localhost:8080/api/configuration

# Get specific configuration
curl GET http://localhost:8080/api/configuration/SERVICE-A/LogLevel

# Create new configuration
curl -X POST http://localhost:8080/api/configuration \
  -H "Content-Type: application/json" \
  -d '{
    "name": "NewSetting",
    "applicationName": "MyApp",
    "value": "test",
    "type": 1,
    "isActive": 1
  }'

# Test configuration reader
curl GET http://localhost:8080/api/configuration/test-reader/SiteName
```

## 🎨 Web Interface Features

Navigate to `/Home/Configurations` for the management interface:

- **📋 Configuration Management**: View, create, edit, and delete configurations
- **🏢 Multi-Application Support**: Filter configurations by application or view all
- **🔍 Real-time Search**: Filter configurations by name instantly
- **🧪 Configuration Testing**: Test configuration reader functionality with detailed feedback
- **📱 Responsive Design**: Works on desktop, tablet, and mobile devices

**Key Interface Components:**
- **Application Filter Dropdown**: Select specific application or "All Applications"
- **Name Search Box**: Type-ahead filtering by configuration name
- **Configuration Table**: Sortable table with Name, Type, Value, IsActive, Application columns
- **Action Buttons**: Edit and Delete buttons for each configuration
- **Add New Button**: Modal form for creating new configurations
- **Test Reader Section**: Test configuration retrieval with application-specific searching

## 🔒 Configuration Model

```csharp
public class ConfigurationItem
{
    public string Id { get; set; }              // Unique identifier
    public string Name { get; set; }            // Configuration key
    public string ApplicationName { get; set; }  // Target application
    public string Value { get; set; }           // Configuration value (stored as string)
    public ConfigurationType Type { get; set; }  // Data type for conversion
    public int IsActive { get; set; }           // 1 = active, 0 = inactive
}

public enum ConfigurationType
{
    String = 1,    // Text values
    Number = 2,    // Integer/decimal values  
    JSON = 3,      // Complex JSON objects
    Boolean = 4    // true/false values
}
```

## ⚡ Performance & Reliability Features

- **🔄 Background Refresh**: Configurable timer updates configurations without blocking
- **📁 File Cache Fallback**: System continues working when database is unavailable
- **⚡ Async Operations**: Full async/await support for non-blocking operations
- **🔧 Connection Resilience**: Automatic retry and graceful degradation
- **📊 Efficient Indexing**: MongoDB compound indexes for optimal query performance

## 📈 Monitoring & Observability

- **📝 Comprehensive Logging**: Structured logging throughout the application
- **❤️ Health Checks**: Built-in health check endpoints (`/health`)
- **🔍 Debug Support**: Detailed logging for troubleshooting
- **📊 Operation Tracking**: Log configuration retrievals, updates, and errors

## 🐳 Docker Deployment

**Quick Start:**
```bash
docker-compose up -d
```

**Includes:**
- ConfigStream Web Application (Port 8080)
- MongoDB Database (Port 27017) 
- Pre-loaded sample data
- Persistent data volumes
- Health checks

## 🔧 Environment Configuration

### Connection Strings

| Setting | Description | Default | Docker |
|---------|-------------|---------|---------|
| `ConnectionStrings:MongoDB` | MongoDB connection string | `mongodb://localhost:27017` | `mongodb://admin:password123@mongodb:27017/DynamicConfiguration?authSource=admin` |

### Logging Levels

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ConfigStream": "Debug"
    }
  }
}
```

## 🧪 Testing the System

### 1. Web Interface Testing
1. Navigate to http://localhost:8080/Home/Configurations
2. Try filtering by different applications
3. Create, edit, and delete configurations
4. Test the configuration reader with different keys

### 2. API Testing
```bash
# Health check
curl http://localhost:8080/health

# Get all applications  
curl http://localhost:8080/api/configuration/applications

# Test reader functionality
curl http://localhost:8080/api/configuration/test-reader/SiteName
```

### 3. Configuration Reader Testing
```csharp
var reader = new ConfigurationReader("SERVICE-A", connectionString, 30000);
var timeout = reader.GetValue<int>("DatabaseTimeout"); // Should return 30
var logLevel = reader.GetValue<string>("LogLevel");     // Should return "Information"
```

## 📝 API Reference

| Method | Endpoint | Description | Example |
|--------|----------|-------------|---------|
| `GET` | `/api/configuration` | Get all configurations (optionally by app) | `?applicationName=SERVICE-A` |
| `GET` | `/api/configuration/{app}/{name}` | Get specific configuration | `/SERVICE-A/LogLevel` |
| `POST` | `/api/configuration` | Create new configuration | JSON body with config data |
| `PUT` | `/api/configuration` | Update existing configuration | JSON body with config data |
| `DELETE` | `/api/configuration/{app}/{name}` | Delete configuration | `/SERVICE-A/OldSetting` |
| `GET` | `/api/configuration/applications` | Get list of all applications | Returns string array |
| `GET` | `/api/configuration/test-reader/{key}` | Test configuration reader | `?applicationName=SERVICE-A` |
| `GET` | `/health` | Application health check | Returns JSON status |

### Development Guidelines
- Follow C# coding conventions
- Add comprehensive logging
- Include unit tests for new features
- Update documentation for API changes
- Ensure Docker build succeeds

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **ASP.NET Core 8** - Web framework
- **MongoDB** - Primary data storage
- **Docker** - Containerization
- **Bootstrap** - UI framework