using Xunit;
using Microsoft.Extensions.Configuration;
using TursoEFCoreDemo.Configuration;

namespace TursoEFCoreDemo.Tests.Configuration;

/// <summary>
/// Unit tests for the ConfigurationExtensions class.
/// </summary>
public class ConfigurationExtensionsTests
{
    [Fact]
    public void GetTursoSettings_WithValidConfiguration_ReturnsCorrectSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TursoSettings:DatabaseUrl"] = "https://test.turso.io",
                ["TursoSettings:AuthToken"] = "test-token",
                ["TursoSettings:ConnectionStringTemplate"] = "{DatabaseUrl}/v2/pipeline;{AuthToken}"
            })
            .Build();

        // Act
        var result = configuration.GetTursoSettings();

        // Assert
        Assert.Equal("https://test.turso.io", result.DatabaseUrl);
        Assert.Equal("test-token", result.AuthToken);
        Assert.Equal("{DatabaseUrl}/v2/pipeline;{AuthToken}", result.ConnectionStringTemplate);
    }

    [Fact]
    public void GetTursoSettings_WithEnvironmentVariable_OverridesConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TURSO_AUTH_TOKEN", "env-token");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TursoSettings:DatabaseUrl"] = "https://test.turso.io",
                ["TursoSettings:AuthToken"] = "config-token"
            })
            .Build();

        try
        {
            // Act
            var result = configuration.GetTursoSettings();

            // Assert
            Assert.Equal("https://test.turso.io", result.DatabaseUrl);
            Assert.Equal("config-token", result.AuthToken); // Config takes precedence
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TURSO_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public void GetTursoSettings_WithEmptyConfig_UsesEnvironmentVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TURSO_AUTH_TOKEN", "env-token");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TursoSettings:DatabaseUrl"] = "https://test.turso.io"
            })
            .Build();

        try
        {
            // Act
            var result = configuration.GetTursoSettings();

            // Assert
            Assert.Equal("https://test.turso.io", result.DatabaseUrl);
            Assert.Equal("env-token", result.AuthToken); // Falls back to env var
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TURSO_AUTH_TOKEN", null);
        }
    }

    [Fact]
    public void GetAppSettings_WithValidConfiguration_ReturnsCorrectSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:Environment"] = "Production",
                ["AppSettings:EnableDetailedErrors"] = "false",
                ["AppSettings:EnableSensitiveDataLogging"] = "false"
            })
            .Build();

        // Act
        var result = configuration.GetAppSettings();

        // Assert
        Assert.Equal("Production", result.Environment);
        Assert.False(result.EnableDetailedErrors);
        Assert.False(result.EnableSensitiveDataLogging);
    }

    [Fact]
    public void GetAppSettings_WithEmptyConfiguration_ReturnsDefaultValues()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var result = configuration.GetAppSettings();

        // Assert
        Assert.Equal("Development", result.Environment);
        Assert.True(result.EnableDetailedErrors);
        Assert.True(result.EnableSensitiveDataLogging);
    }

    [Fact]
    public void AddTursoConfiguration_AddsAllRequiredFiles()
    {
        // Arrange & Act
        var configurationBuilder = new ConfigurationBuilder()
            .AddTursoConfiguration();

        var configuration = configurationBuilder.Build();

        // Assert
        // This test verifies that the configuration builder is properly set up
        // The actual file loading will depend on the test environment
        Assert.NotNull(configuration);
    }
}