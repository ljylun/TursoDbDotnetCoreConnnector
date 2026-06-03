namespace TursoEFCoreDemo.Configuration;

/// <summary>
/// Configuration settings for Turso database connection.
/// </summary>
public class TursoSettings
{
    /// <summary>
    /// The section name in configuration files.
    /// </summary>
    public const string SectionName = "TursoSettings";

    /// <summary>
    /// The URL of the Turso database.
    /// </summary>
    public string DatabaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The authentication token for Turso database access.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// The connection string template with placeholders.
    /// </summary>
    public string ConnectionStringTemplate { get; set; } = "{DatabaseUrl}/v2/pipeline;{AuthToken}";

    /// <summary>
    /// Builds the connection string from the template and settings.
    /// </summary>
    /// <returns>The formatted connection string.</returns>
    public string BuildConnectionString()
    {
        return ConnectionStringTemplate
            .Replace("{DatabaseUrl}", DatabaseUrl)
            .Replace("{AuthToken}", AuthToken);
    }
}