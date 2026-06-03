using System.Data;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbCommand implementation for Turso HTTP.
/// </summary>
public class TursoCommand : DbCommand
{
    private readonly TursoHttpClient _httpClient;
    private readonly List<DbParameter> _parameters = new();

    public TursoCommand(TursoHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => new TursoParameterCollection(_parameters);
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync().GetAwaiter().GetResult();
    }

    public override object? ExecuteScalar()
    {
        return ExecuteScalarAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        await EnsureTransactionBegunAsync();
        var tursoParams = GetTursoParameters();
        var affected = await _httpClient.ExecuteNonQueryAsync(CommandText, tursoParams);
        return (int)affected;
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        await EnsureTransactionBegunAsync();
        var tursoParams = GetTursoParameters();
        return await _httpClient.ExecuteScalarAsync(CommandText, tursoParams);
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        await EnsureTransactionBegunAsync();
        var tursoParams = GetTursoParameters();
        var result = await _httpClient.QueryAsync(CommandText, tursoParams);
        return new TursoDataReader(result);
    }

    protected override DbParameter CreateDbParameter()
    {
        return new TursoDbParameter();
    }

    public override void Prepare() { }

    private async Task EnsureTransactionBegunAsync()
    {
        if (DbTransaction is TursoTransaction tx)
        {
            await tx.EnsureBegunAsync();
        }
    }

    private TursoParameter[] GetTursoParameters()
    {
        if (_parameters.Count == 0)
            return Array.Empty<TursoParameter>();

        return _parameters.Select(p => TursoParameter.FromObject(p.Value)).ToArray();
    }
}
