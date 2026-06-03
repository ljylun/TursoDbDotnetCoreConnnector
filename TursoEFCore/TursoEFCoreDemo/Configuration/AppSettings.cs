namespace TursoEFCoreDemo.Configuration;

/// <summary>
/// General application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The section name in configuration files.
    /// </summary>
    public const string SectionName = "AppSettings";

    /// <summary>
    /// The current environment name.
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Whether to enable detailed error messages.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = true;

    /// <summary>
    /// Whether to enable sensitive data logging.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = true;
}