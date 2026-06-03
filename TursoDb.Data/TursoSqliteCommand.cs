using System.Data;
using Microsoft.Data.Sqlite;

namespace TursoDb.Data;

/// <summary>
/// A SqliteCommand subclass that intercepts all SQL execution
/// and routes it to Turso via HTTP.
/// Since SqliteDataReader is sealed, we create one via reflection
/// by loading data into a temporary in-memory SQLite table.
/// </summary>
public class TursoSqliteCommand : SqliteCommand
{
    static TursoSqliteCommand()
    {
    }

    private readonly TursoHttpClient _httpClient;

    public TursoSqliteCommand(TursoHttpClient httpClient, TursoSqliteConnection connection)
    {
        _httpClient = httpClient;
        Connection = connection;
    }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync().GetAwaiter().GetResult();
    }

    public override object? ExecuteScalar()
    {
        return ExecuteScalarAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        var parameters = ExtractParameters();
        var affected = await _httpClient.ExecuteNonQueryAsync(CommandText, parameters);
        return (int)affected;
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        var parameters = ExtractParameters();
        return await _httpClient.ExecuteScalarAsync(CommandText, parameters);
    }

    public override SqliteDataReader ExecuteReader()
    {
        return ExecuteReaderAsync().GetAwaiter().GetResult();
    }

    public override SqliteDataReader ExecuteReader(CommandBehavior behavior)
    {
        return ExecuteReaderAsync(behavior).GetAwaiter().GetResult();
    }

    public override async Task<SqliteDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
    {
        var parameters = ExtractParameters();
        var result = await _httpClient.QueryAsync(CommandText, parameters);
        return CreateSqliteDataReader(result);
    }

    public override async Task<SqliteDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
    {
        return await ExecuteReaderAsync(cancellationToken);
    }

    private TursoParameter[] ExtractParameters()
    {
        // Turso's Hrana protocol uses positional parameters, so we must return
        // them in the order they appear in the SQL statement.
        // EF Core uses named parameters (@p0, @p1, ...), so we need to sort
        // by parameter name to match the SQL order.
        var sortedParams = Parameters.Cast<SqliteParameter>()
            .OrderBy(p => p.ParameterName, StringComparer.Ordinal)
            .ToList();

        return sortedParams
            .Select(p => TursoParameter.FromObject(p.Value))
            .ToArray();
    }

    /// <summary>
    /// Creates a SqliteDataReader from Turso query results by loading data into
    /// a temporary in-memory SQLite table with proper column types.
    /// </summary>
    private static SqliteDataReader CreateSqliteDataReader(TursoQueryResult queryResult)
    {
        var tempConn = new SqliteConnection("Data Source=:memory:");
        tempConn.Open();

        if (queryResult.Columns.Count == 0 || queryResult.Rows.Count == 0)
        {
            var emptyCmd = tempConn.CreateCommand();
            emptyCmd.CommandText = "SELECT 0 WHERE 1=0";
            return emptyCmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        // Create temp table with proper column types based on column names
        var createSql = "CREATE TABLE _temp_result (";
        for (int i = 0; i < queryResult.Columns.Count; i++)
        {
            if (i > 0) createSql += ", ";
            createSql += $"\"{queryResult.Columns[i]}\" {GetColumnType(queryResult.Columns[i])}";
        }
        createSql += ")";

        using (var createCmd = tempConn.CreateCommand())
        {
            createCmd.CommandText = createSql;
            createCmd.ExecuteNonQuery();
        }

        // Insert rows
        using var transaction = tempConn.BeginTransaction();
        var insertSql = "INSERT INTO _temp_result VALUES (";
        for (int i = 0; i < queryResult.Columns.Count; i++)
        {
            if (i > 0) insertSql += ", ";
            insertSql += $"@p{i}";
        }
        insertSql += ")";

        using var insertCmd = tempConn.CreateCommand();
        insertCmd.CommandText = insertSql;
        insertCmd.Transaction = transaction;
        for (int i = 0; i < queryResult.Columns.Count; i++)
        {
            insertCmd.Parameters.Add(new SqliteParameter($"@p{i}", ""));
        }

        foreach (var row in queryResult.Rows)
        {
            for (int i = 0; i < row.Count && i < queryResult.Columns.Count; i++)
            {
                var val = row[i];
                if (val is null)
                {
                    insertCmd.Parameters[$"@p{i}"].Value = DBNull.Value;
                }
                else if (val is string s)
                {
                    // Try to parse as DateTime for proper SQLite storage
                    if (DateTime.TryParse(s, out var dt))
                        insertCmd.Parameters[$"@p{i}"].Value = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    else if (string.IsNullOrEmpty(s))
                        insertCmd.Parameters[$"@p{i}"].Value = DBNull.Value;
                    else
                        insertCmd.Parameters[$"@p{i}"].Value = s;
                }
                else
                {
                    insertCmd.Parameters[$"@p{i}"].Value = val;
                }
            }
            insertCmd.ExecuteNonQuery();
        }
        transaction.Commit();

        var selectCmd = tempConn.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM _temp_result";
        return selectCmd.ExecuteReader(CommandBehavior.CloseConnection);
    }

    private static string GetColumnType(string columnName)
    {
        var col = columnName.ToLowerInvariant();
        if (col.Contains("id"))
            return "INTEGER";
        if (col.Contains("price") || col.Contains("amount") || col.Contains("cost"))
            return "REAL";
        if (col.Contains("count") || col.Contains("days") || col.Contains("duration") || col.Contains("quantity"))
            return "INTEGER";
        if (col.Contains("is_") || col.Contains("active") || col.Contains("enabled"))
            return "INTEGER";
        if (col.Contains("at") || col.Contains("date") || col.Contains("time"))
            return "TEXT";
        return "TEXT";
    }
}
