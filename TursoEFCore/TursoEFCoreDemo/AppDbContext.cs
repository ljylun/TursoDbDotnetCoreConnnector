using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TursoEFCoreDemo.Configuration;
using TursoEFCoreDemo.Models;

namespace TursoEFCoreDemo;

/// <summary>
/// Database context for the Turso database.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets or sets the tours dataset.
    /// </summary>
    public DbSet<Tour> Tours { get; set; }

    /// <summary>
    /// Configures the database connection.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var tursoSettings = _configuration.GetTursoSettings();

        if (string.IsNullOrEmpty(tursoSettings.DatabaseUrl))
        {
            throw new InvalidOperationException(
                "Turso database URL is not configured. Please set TursoSettings:DatabaseUrl in appsettings.json or environment variable.");
        }

        if (string.IsNullOrEmpty(tursoSettings.AuthToken))
        {
            throw new InvalidOperationException(
                "Turso authentication token is not configured. Please set TURSO_AUTH_TOKEN environment variable or TursoSettings:AuthToken in configuration.");
        }

        var connectionString = tursoSettings.BuildConnectionString();
        var appSettings = _configuration.GetAppSettings();

        optionsBuilder.UseLibSql(connectionString);

        if (appSettings.EnableDetailedErrors)
        {
            optionsBuilder.EnableDetailedErrors();
        }

        if (appSettings.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}