# ToursDb

[中文版 ReadMe](Readme-ZhCn.md)

A demonstration project showcasing **Entity Framework Core** integration with **Turso** (distributed SQLite database) via HTTP API. This project implements a custom ADO.NET data provider that routes all SQL execution to Turso's Hrana protocol over HTTP, enabling seamless EF Core operations on a serverless SQLite database.

## Table of Contents

- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Development Guidelines](#development-guidelines)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Project Overview

ToursDb is a .NET 8 console application that demonstrates how to use Entity Framework Core with Turso, a distributed SQLite database service. The project implements a custom data provider that translates EF Core operations into HTTP requests to Turso's Hrana protocol.

### Key Features

- **EF Core Integration**: Full Entity Framework Core support with Turso backend
- **Custom ADO.NET Provider**: Implements `DbConnection`, `DbCommand`, `DbDataReader`, and `DbTransaction` for Turso HTTP
- **Hrana Protocol**: Uses Turso's HTTP-based Hrana protocol (v2 pipeline) for database operations
- **CRUD Operations**: Complete Create, Read, Update, Delete demonstration
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Runtime and framework |
| C# | 12.0 | Primary programming language |
| Entity Framework Core | 8.0.11 | ORM for database operations |
| Microsoft.Data.Sqlite | 8.0.11 | SQLite ADO.NET provider (base for custom implementation) |

### Infrastructure

| Technology | Version | Purpose |
|------------|---------|---------|
| Turso | N/A | Distributed SQLite database service |
| Hrana Protocol | v2 | HTTP-based database protocol |
| libSQL | N/A | SQLite fork used by Turso |

### Tooling

| Technology | Version | Purpose |
|------------|---------|---------|
| MSBuild | 17.x | Build system |
| NuGet | 6.x | Package management |
| Visual Studio | 2022+ | IDE (optional) |
| .NET CLI | 8.0 | Command-line tools |

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.11 | Core EF functionality |
| Microsoft.EntityFrameworkCore.Relational | 8.0.11 | Relational database support |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.11 | SQLite provider for EF Core |
| Microsoft.Data.Sqlite.Core | 8.0.11 | SQLite ADO.NET provider |
| Microsoft.Extensions.Configuration | 8.0.0 | Configuration framework |
| Microsoft.Extensions.Configuration.Json | 8.0.1 | JSON configuration provider |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 8.0.0 | Environment variable configuration |
| Microsoft.Extensions.Http | 8.0.1 | HTTP client factory |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Dependency injection container |
| Microsoft.Extensions.Logging.Console | 8.0.1 | Console logging provider |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | Logging abstractions |

## Prerequisites

### Required

- **.NET SDK**: 8.0.100 or later
- **Turso Account**: Free account at [turso.tech](https://turso.tech)
- **Turso CLI**: For database management (optional but recommended)

### Verify Installation

```bash
# Check .NET version
dotnet --version

# Check Turso CLI (if installed)
turso --version
```

## Project Structure

```
ToursDb/
├── Readme.md                    # This file
├── Readme-ZhCn.md              # Chinese documentation
├── ToursDb.slnx                # Solution file
├── ToursDb.App/                # Main application project
│   ├── Program.cs              # Application entry point
│   ├── ToursDb.App.csproj      # Project file
│   ├── appsettings.json        # Configuration file
│   ├── Data/
│   │   ├── ToursDbContext.cs                    # EF Core DbContext
│   │   └── TursoDbContextOptionsExtensions.cs   # EF Core configuration extensions
│   └── Models/
│       └── Tour.cs             # Tour entity model
└── ToursDb.Data/               # Custom Turso data provider library
    ├── ToursDb.Data.csproj     # Project file
    ├── TursoHttpClient.cs      # HTTP client for Turso API
    ├── TursoConnection.cs      # ADO.NET DbConnection implementation
    ├── TursoCommand.cs         # ADO.NET DbCommand implementation
    ├── TursoDataReader.cs      # ADO.NET DbDataReader implementation
    ├── TursoTransaction.cs     # ADO.NET DbTransaction implementation
    ├── TursoDbParameter.cs     # ADO.NET DbParameter implementation
    ├── TursoParameterCollection.cs  # ADO.NET DbParameterCollection
    ├── TursoSqliteConnection.cs     # SqliteConnection subclass for EF Core
    └── TursoSqliteCommand.cs        # SqliteCommand subclass for EF Core
```

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/ToursDb.git
cd ToursDb
```

### 2. Create a Turso Database

```bash
# Install Turso CLI (if not already installed)
curl -sSfL https://get.tur.so/install.sh | bash

# Login to Turso
turso auth login

# Create a new database
turso db create my-tours-db

# Get the database URL
turso db show my-tours-db --http-url

# Generate an auth token
turso db tokens create my-tours-db
```

### 3. Configure the Application

Update `ToursDb.App/appsettings.json`:

```json
{
  "Turso": {
    "DatabaseUrl": "https://your-database-url.turso.io",
    "PipelineVersion": "v2"
  }
}
```

Set the auth token as an environment variable:

**Windows (PowerShell):**
```powershell
$env:TURSO_AUTH_TOKEN = "your-auth-token-here"
```

**Windows (CMD):**
```cmd
set TURSO_AUTH_TOKEN=your-auth-token-here
```

**macOS/Linux:**
```bash
export TURSO_AUTH_TOKEN="your-auth-token-here"
```

### 4. Build the Project

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

### 5. Run the Application

```bash
# Run the application
dotnet run --project ToursDb.App
```

### Expected Output

```
=== ToursDb - EF Core + Turso (HTTP API) ===

Ensuring database schema...
Schema ready.

--- CREATE: Adding tours ---
Created 4 tours.

--- READ: All tours ---
[1] Paris City Break | Paris, France | $1299.99 | 5 days | Active: True | Created: 2024-01-15
[2] Tokyo Adventure | Tokyo, Japan | $2499.50 | 10 days | Active: True | Created: 2024-01-15
...

--- READ: Active tours under $2000 ---
[1] Paris City Break | Paris, France | $1299.99 | 5 days | Active: True | Created: 2024-01-15
...

--- UPDATE: Modifying a tour ---
Updated tour [1] - New price: $1499.99
Verified: [1] Paris City Break | Paris, France | $1499.99 | 5 days | Active: True | Created: 2024-01-15

--- DELETE: Removing a tour ---
Deleted tour [4] Northern Lights Iceland

--- FINAL STATE: Remaining tours ---
...

Total tours in database: 3

=== Demo complete! ===
```

## Configuration

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `TURSO_AUTH_TOKEN` | Yes | Authentication token for Turso database |
| `TURSO_DATABASE_URL` | No | Override the database URL from appsettings.json |

### appsettings.json

```json
{
  "Turso": {
    "DatabaseUrl": "https://your-database-url.turso.io",
    "PipelineVersion": "v2"
  }
}
```

### Configuration Priority

1. Environment variables (highest priority)
2. `appsettings.json`
3. Default values (lowest priority)

## Development Guidelines

### Code Style

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types (enabled in project)
- Use implicit usings (enabled in project)

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal
```

### Adding New Migrations

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project ToursDb.App

# Apply migrations
dotnet ef database update --project ToursDb.App
```

## Troubleshooting

### Common Issues

#### 1. Missing Authentication Token

**Error:**
```
ERROR: Turso database URL and auth token are required.
```

**Solution:**
Set the `TURSO_AUTH_TOKEN` environment variable:
```bash
export TURSO_AUTH_TOKEN="your-token-here"
```

#### 2. Database Connection Failed

**Error:**
```
Turso API error: 401 Unauthorized
```

**Solution:**
- Verify your auth token is valid
- Check if the database URL is correct
- Ensure the database exists: `turso db list`

#### 3. Build Errors

**Error:**
```
error NETSDK1004: Assets file not found
```

**Solution:**
Restore NuGet packages:
```bash
dotnet restore
```

#### 4. EF Core Logging

To enable detailed EF Core logging, the application is configured with:
```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
optionsBuilder.EnableSensitiveDataLogging();
```

This will output all SQL statements to the console.

### Getting Help

- [Turso Documentation](https://docs.turso.tech)
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ADO.NET Documentation](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/)

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

**Note**: This is a demonstration project. For production use, consider adding:
- Proper error handling and retry logic
- Connection pooling
- Health checks
- Structured logging (e.g., Serilog)
- Unit and integration tests
