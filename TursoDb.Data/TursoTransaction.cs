using System.Data;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbTransaction implementation for Turso HTTP.
/// Uses BEGIN / COMMIT / ROLLBACK SQL statements since Turso's HTTP API
/// does not support server-side transaction management.
/// </summary>
public class TursoTransaction : DbTransaction
{
    private readonly TursoConnection _connection;
    private readonly IsolationLevel _isolationLevel;
    private bool _begun;
    private bool _committed;
    private bool _rolledBack;
    private bool _disposed;

    public TursoTransaction(TursoConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public override IsolationLevel IsolationLevel => _isolationLevel;
    protected override DbConnection? DbConnection => _connection;

    /// <summary>
    /// Executes the BEGIN SQL statement. Called by the connection when the first
    /// command in the transaction is executed.
    /// </summary>
    public async Task EnsureBegunAsync()
    {
        if (_begun) return;

        var beginSql = _isolationLevel switch
        {
            IsolationLevel.Serializable or IsolationLevel.Snapshot => "BEGIN IMMEDIATE",
            IsolationLevel.ReadCommitted => "BEGIN IMMEDIATE",
            _ => "BEGIN"
        };

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = beginSql;
        await cmd.ExecuteNonQueryAsync();
        _begun = true;
    }

    public override void Commit()
    {
        CommitAsync().GetAwaiter().GetResult();
    }

    public async Task CommitAsync()
    {
        if (!_begun) return;
        if (_rolledBack)
            throw new InvalidOperationException("Transaction has already been rolled back.");
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed.");

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "COMMIT";
        await cmd.ExecuteNonQueryAsync();
        _committed = true;
        _connection.ClearTransaction();
    }

    public override void Rollback()
    {
        RollbackAsync().GetAwaiter().GetResult();
    }

    public async Task RollbackAsync()
    {
        if (!_begun) return;
        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed.");

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "ROLLBACK";
        await cmd.ExecuteNonQueryAsync();
        _rolledBack = true;
        _connection.ClearTransaction();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_begun && !_committed && !_rolledBack)
            {
                try
                {
                    Rollback();
                }
                catch
                {
                    // Best-effort rollback on dispose
                }
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
