using Microsoft.Data.Sqlite;

namespace TursoDb.Data;

/// <summary>
/// A SqliteConnection subclass that intercepts all command execution
/// and routes it to Turso via HTTP. This allows EF Core's SQLite provider
/// to work seamlessly with Turso's HTTP API.
/// </summary>
public class TursoSqliteConnection : SqliteConnection
{
    private readonly TursoHttpClient _httpClient;
    private bool _tursoOpened;

    public TursoSqliteConnection(TursoHttpClient httpClient, string connectionString = "Data Source=:memory:")
        : base(connectionString)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public override void Open()
    {
        // Don't actually open the SQLite connection — we use Turso HTTP instead.
        // But we need to set the state to Open so EF Core thinks the connection is ready.
        // We do this by briefly opening and closing the base connection to satisfy EF Core's state tracking,
        // then we intercept all command execution.
        base.Open();
        _tursoOpened = true;
    }

    public override async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        await base.OpenAsync(cancellationToken);
        _tursoOpened = true;
    }

    protected override SqliteCommand CreateDbCommand()
    {
        // Return a Turso-intercepting command instead of a real SQLite command
        return new TursoSqliteCommand(_httpClient, this);
    }

    public new SqliteCommand CreateCommand()
    {
        return new TursoSqliteCommand(_httpClient, this);
    }
}
