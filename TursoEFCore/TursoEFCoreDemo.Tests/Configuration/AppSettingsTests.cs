using Xunit;
using TursoEFCoreDemo.Configuration;

namespace TursoEFCoreDemo.Tests.Configuration;

/// <summary>
/// Unit tests for the AppSettings class.
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange
        var settings = new AppSettings();

        // Act & Assert
        Assert.Equal("Development", settings.Environment);
        Assert.True(settings.EnableDetailedErrors);
        Assert.True(settings.EnableSensitiveDataLogging);
    }

    [Fact]
    public void SectionName_ReturnsCorrectValue()
    {
        // Act & Assert
        Assert.Equal("AppSettings", AppSettings.SectionName);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var settings = new AppSettings
        {
            Environment = "Production",
            EnableDetailedErrors = false,
            EnableSensitiveDataLogging = false
        };

        // Act & Assert
        Assert.Equal("Production", settings.Environment);
        Assert.False(settings.EnableDetailedErrors);
        Assert.False(settings.EnableSensitiveDataLogging);
    }
}