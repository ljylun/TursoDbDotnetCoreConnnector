using System.Collections;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbDataReader implementation for Turso HTTP query results.
/// </summary>
public class TursoDataReader : DbDataReader
{
    private readonly TursoQueryResult _result;
    private int _currentRowIndex = -1;

    public TursoDataReader(TursoQueryResult result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override int Depth => 0;
    public override int FieldCount => _result.Columns.Count;
    public override bool HasRows => _result.Rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => (int)_result.AffectedRowCount;

    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => "TEXT";
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override Type GetFieldType(int ordinal) => typeof(object);
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => new(GetString(ordinal));
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetName(int ordinal) => _result.Columns[ordinal];

    public override int GetOrdinal(string name)
    {
        var index = _result.Columns.IndexOf(name);
        if (index < 0)
            throw new IndexOutOfRangeException($"Column '{name}' not found.");
        return index;
    }

    public override string GetString(int ordinal) => GetValue(ordinal)?.ToString() ?? string.Empty;
    public override object GetValue(int ordinal) => _result.Rows[_currentRowIndex][ordinal]!;

    public override int GetValues(object[] values)
    {
        var row = _result.Rows[_currentRowIndex];
        var count = Math.Min(values.Length, row.Count);
        for (int i = 0; i < count; i++)
            values[i] = row[i]!;
        return count;
    }

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) is null;
    public override bool NextResult() => false;

    public override bool Read()
    {
        _currentRowIndex++;
        return _currentRowIndex < _result.Rows.Count;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    public override IEnumerator GetEnumerator() => _result.Rows.GetEnumerator();
}
