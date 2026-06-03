using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace TursoEFCoreDemo.Configuration;

/// <summary>
/// Extension methods for configuration setup.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Turso configuration settings to the configuration builder.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddTursoConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Use the directory containing the assembly as the base path
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

        configurationBuilder
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return configurationBuilder;
    }

    /// <summary>
    /// Gets the Turso settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The Turso settings.</returns>
    public static TursoSettings GetTursoSettings(this IConfiguration configuration)
    {
        var tursoSettings = new TursoSettings();
        configuration.GetSection(TursoSettings.SectionName).Bind(tursoSettings);

        // Override DatabaseUrl from environment variable if available
        var envDbUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL");
        if (!string.IsNullOrEmpty(envDbUrl))
        {
            tursoSettings.DatabaseUrl = envDbUrl;
        }

        // Override AuthToken from environment variable if not set in configuration
        if (string.IsNullOrEmpty(tursoSettings.AuthToken))
        {
            tursoSettings.AuthToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? string.Empty;
        }

        return tursoSettings;
    }

    /// <summary>
    /// Gets the application settings from configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The application settings.</returns>
    public static AppSettings GetAppSettings(this IConfiguration configuration)
    {
        var appSettings = new AppSettings();
        configuration.GetSection(AppSettings.SectionName).Bind(appSettings);
        return appSettings;
    }
}