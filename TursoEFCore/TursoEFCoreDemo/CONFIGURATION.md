# Configuration Management Guide

This document describes the configuration management system for the TursoEFCoreDemo application.

## Overview

The application uses a structured configuration system based on the .NET Configuration framework with environment-specific settings. This approach provides:

- **Centralized configuration** through JSON files
- **Environment-specific settings** for Development, Production, etc.
- **Secure secret management** through environment variables
- **Type-safe configuration** through strongly-typed settings classes
- **Hot-reload support** for configuration changes

## Configuration Files

### appsettings.json
The main configuration file containing default settings for all environments.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  },
  "TursoSettings": {
    "DatabaseUrl": "https://interviewmaster-megadotnet.aws-ap-northeast-1.turso.io",
    "AuthToken": "",
    "ConnectionStringTemplate": "{DatabaseUrl}/v2/pipeline;{AuthToken}"
  },
  "AppSettings": {
    "Environment": "Development",
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true
  }
}
```

### appsettings.Development.json
Development-specific settings that override the main configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AppSettings": {
    "Environment": "Development",
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true
  }
}
```

### appsettings.Production.json
Production-specific settings optimized for performance and security.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "AppSettings": {
    "Environment": "Production",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false
  }
}
```

## Configuration Sections

### TursoSettings

| Property | Type | Description | Required |
|----------|------|-------------|----------|
| `DatabaseUrl` | string | The URL of the Turso database | Yes |
| `AuthToken` | string | Authentication token for Turso database | Yes* |
| `ConnectionStringTemplate` | string | Template for building connection string | No |

*Note: `AuthToken` can be provided via the `TURSO_AUTH_TOKEN` environment variable as a fallback.

### AppSettings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Environment` | string | "Development" | Current environment name |
| `EnableDetailedErrors` | bool | true | Enable detailed error messages |
| `EnableSensitiveDataLogging` | bool | true | Enable sensitive data logging |

## Environment Variables

The following environment variables are supported:

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Sets the current environment | `Development`, `Production` |
| `TURSO_DATABASE_URL` | Turso database URL (overrides config file) | `https://your-db.turso.io` |
| `TURSO_AUTH_TOKEN` | Turso database authentication token | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` |

## Configuration Precedence

Configuration values are loaded in the following order (later sources override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific configuration)
3. Environment variables

## Usage Examples

### Basic Configuration Access

```csharp
// Build configuration
var configuration = new ConfigurationBuilder()
    .AddTursoConfiguration()
    .Build();

// Access Turso settings
var tursoSettings = configuration.GetTursoSettings();
Console.WriteLine($"Database URL: {tursoSettings.DatabaseUrl}");

// Access application settings
var appSettings = configuration.GetAppSettings();
Console.WriteLine($"Environment: {appSettings.Environment}");
```

### Using Configuration in DbContext

```csharp
public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var tursoSettings = _configuration.GetTursoSettings();
        var connectionString = tursoSettings.BuildConnectionString();
        optionsBuilder.UseLibSql(connectionString);
    }
}
```

### Environment-Specific Configuration

```csharp
// Set environment (typically done in launch settings or deployment)
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

// Configuration will automatically load appsettings.Production.json
var configuration = new ConfigurationBuilder()
    .AddTursoConfiguration()
    .Build();
```

## Security Best Practices

### Authentication Token Management

1. **Never commit secrets to source control**
2. Use environment variables for sensitive data in production
3. Use Azure Key Vault, AWS Secrets Manager, or similar services for production deployments
4. Rotate authentication tokens regularly

### Development Environment

For development, you can set the `TURSO_AUTH_TOKEN` environment variable:

**Windows (PowerShell):**
```powershell
$env:TURSO_AUTH_TOKEN = "your-dev-token-here"
```

**Windows (Command Prompt):**
```cmd
set TURSO_AUTH_TOKEN=your-dev-token-here
```

**Linux/macOS:**
```bash
export TURSO_AUTH_TOKEN="your-dev-token-here"
```

### Production Environment

For production deployments, use:

1. **Environment variables** set by your deployment platform
2. **Secret management services** (Azure Key Vault, AWS Secrets Manager, etc.)
3. **Container orchestration secrets** (Kubernetes secrets, Docker secrets)

## Connection String Format

The connection string is built using the template:

```
{DatabaseUrl}/v2/pipeline;{AuthToken}
```

Example:
```
https://interviewmaster-megadotnet.aws-ap-northeast-1.turso.io/v2/pipeline;eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Troubleshooting

### Common Issues

1. **Missing Authentication Token**
   ```
   InvalidOperationException: Turso authentication token is not configured.
   ```
   **Solution:** Set the `TURSO_AUTH_TOKEN` environment variable or add it to configuration.

2. **Missing Database URL**
   ```
   InvalidOperationException: Turso database URL is not configured.
   ```
   **Solution:** Verify `TursoSettings:DatabaseUrl` is set in configuration.

3. **Configuration File Not Found**
   ```
   FileNotFoundException: The configuration file 'appsettings.json' was not found.
   ```
   **Solution:** Ensure configuration files are copied to the output directory (set `CopyToOutputDirectory` to `PreserveNewest`).

### Debug Configuration

To debug configuration issues, enable detailed errors:

```json
{
  "AppSettings": {
    "EnableDetailedErrors": true
  }
}
```

## Testing

The configuration system includes comprehensive unit tests. Run tests with:

```bash
dotnet test
```

Test coverage includes:
- Configuration file loading
- Environment variable override
- Connection string building
- Default value handling
- Error conditions

## Migration from Hardcoded Configuration

If you're migrating from hardcoded configuration:

1. **Identify hardcoded values** in your codebase
2. **Add corresponding settings** to `appsettings.json`
3. **Update code** to use `IConfiguration` and strongly-typed settings
4. **Test thoroughly** in all environments
5. **Update deployment scripts** to set environment variables

### Before (Hardcoded)
```csharp
string dbUrl = "https://interviewmaster-megadotnet.aws-ap-northeast-1.turso.io";
string authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN");
string connectionString = $"{dbUrl}/v2/pipeline;{authToken}";
```

### After (Configuration-based)
```csharp
var tursoSettings = _configuration.GetTursoSettings();
var connectionString = tursoSettings.BuildConnectionString();
```