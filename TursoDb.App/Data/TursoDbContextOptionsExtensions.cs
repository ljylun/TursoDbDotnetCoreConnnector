using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TursoDb.Data;

namespace TursoDb.App.Data;

/// <summary>
/// Extension methods for configuring EF Core to use Turso via HTTP.
/// </summary>
public static class TursoDbContextOptionsExtensions
{
    /// <summary>
    /// Configures the DbContext to connect to a Turso database via HTTP.
    /// Uses a TursoSqliteConnection (subclass of SqliteConnection) so EF Core's
    /// SQLite provider works seamlessly, but all SQL execution is routed to Turso HTTP.
    /// </summary>
    public static DbContextOptionsBuilder UseTurso(
        this DbContextOptionsBuilder optionsBuilder,
        string databaseUrl,
        string authToken,
        string pipelineVersion = "v2")
    {
        var options = new TursoOptions
        {
            DatabaseUrl = databaseUrl,
            AuthToken = authToken,
            PipelineVersion = pipelineVersion
        };

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<TursoHttpClient>();
        var httpClient = new TursoHttpClient(options, logger);
        var connection = new TursoSqliteConnection(httpClient);

        return optionsBuilder.UseTurso(connection);
    }

    /// <summary>
    /// Configures the DbContext to connect to a Turso database via HTTP using an existing connection.
    /// </summary>
    public static DbContextOptionsBuilder UseTurso(
        this DbContextOptionsBuilder optionsBuilder,
        TursoSqliteConnection connection)
    {
        return optionsBuilder.UseSqlite(connection);
    }
}
