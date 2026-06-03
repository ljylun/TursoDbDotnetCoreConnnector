using System.Data;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbConnection implementation that communicates with Turso via HTTP (Hrana protocol).
/// This enables EF Core to work with Turso through the standard ADO.NET abstraction.
/// </summary>
public class TursoConnection : DbConnection
{
    private readonly TursoHttpClient _httpClient;
    private ConnectionState _state = ConnectionState.Closed;
    private string _connectionString = string.Empty;
    private TursoTransaction? _currentTransaction;

    public TursoConnection(TursoHttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public TursoConnection(TursoHttpClient httpClient, string connectionString)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _connectionString = connectionString;
    }

    public override string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value ?? string.Empty;
    }

    public override string Database => "Turso";
    public override string DataSource => "Turso HTTP";
    public override string ServerVersion => "libSQL/HTTP";
    public override ConnectionState State => _state;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        if (_currentTransaction != null)
            throw new InvalidOperationException("A transaction is already in progress. Turso HTTP does not support nested transactions.");

        _currentTransaction = new TursoTransaction(this, isolationLevel);
        return _currentTransaction;
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("Cannot change database with Turso HTTP connection.");
    }

    public override void Open()
    {
        _state = ConnectionState.Open;
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        _state = ConnectionState.Open;
        await Task.CompletedTask;
    }

    public override async Task CloseAsync()
    {
        _state = ConnectionState.Closed;
        await Task.CompletedTask;
    }

    internal TursoHttpClient HttpClient => _httpClient;

    /// <summary>
    /// Called by TursoTransaction after commit/rollback to clear the current transaction reference.
    /// </summary>
    internal void ClearTransaction()
    {
        _currentTransaction = null;
    }

    protected override DbCommand CreateDbCommand()
    {
        var cmd = new TursoCommand(_httpClient);
        if (_currentTransaction != null)
            cmd.Transaction = _currentTransaction;
        return cmd;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
        base.Dispose(disposing);
    }
}
