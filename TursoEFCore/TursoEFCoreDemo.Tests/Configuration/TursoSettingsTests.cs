using Xunit;
using TursoEFCoreDemo.Configuration;

namespace TursoEFCoreDemo.Tests.Configuration;

/// <summary>
/// Unit tests for the TursoSettings class.
/// </summary>
public class TursoSettingsTests
{
    [Fact]
    public void BuildConnectionString_WithValidSettings_ReturnsCorrectConnectionString()
    {
        // Arrange
        var settings = new TursoSettings
        {
            DatabaseUrl = "https://test.turso.io",
            AuthToken = "test-token",
            ConnectionStringTemplate = "{DatabaseUrl}/v2/pipeline;{AuthToken}"
        };

        // Act
        var result = settings.BuildConnectionString();

        // Assert
        Assert.Equal("https://test.turso.io/v2/pipeline;test-token", result);
    }

    [Fact]
    public void BuildConnectionString_WithCustomTemplate_ReturnsCorrectConnectionString()
    {
        // Arrange
        var settings = new TursoSettings
        {
            DatabaseUrl = "https://test.turso.io",
            AuthToken = "test-token",
            ConnectionStringTemplate = "url={DatabaseUrl};token={AuthToken};mode=remote"
        };

        // Act
        var result = settings.BuildConnectionString();

        // Assert
        Assert.Equal("url=https://test.turso.io;token=test-token;mode=remote", result);
    }

    [Fact]
    public void BuildConnectionString_WithEmptyUrl_ReturnsConnectionStringWithEmptyUrl()
    {
        // Arrange
        var settings = new TursoSettings
        {
            DatabaseUrl = "",
            AuthToken = "test-token",
            ConnectionStringTemplate = "{DatabaseUrl}/v2/pipeline;{AuthToken}"
        };

        // Act
        var result = settings.BuildConnectionString();

        // Assert
        Assert.Equal("/v2/pipeline;test-token", result);
    }

    [Fact]
    public void BuildConnectionString_WithEmptyToken_ReturnsConnectionStringWithEmptyToken()
    {
        // Arrange
        var settings = new TursoSettings
        {
            DatabaseUrl = "https://test.turso.io",
            AuthToken = "",
            ConnectionStringTemplate = "{DatabaseUrl}/v2/pipeline;{AuthToken}"
        };

        // Act
        var result = settings.BuildConnectionString();

        // Assert
        Assert.Equal("https://test.turso.io/v2/pipeline;", result);
    }

    [Fact]
    public void SectionName_ReturnsCorrectValue()
    {
        // Act & Assert
        Assert.Equal("TursoSettings", TursoSettings.SectionName);
    }
}