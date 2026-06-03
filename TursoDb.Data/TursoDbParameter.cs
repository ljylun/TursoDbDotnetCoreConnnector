using System.Data;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbParameter implementation for Turso.
/// </summary>
public class TursoDbParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object? _value;
    private DbType _dbType = DbType.String;

    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override object? Value
    {
        get => _value;
        set => _value = value;
    }

    public override DbType DbType
    {
        get => _dbType;
        set => _dbType = value;
    }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override DataRowVersion SourceVersion { get; set; }
    public override byte Precision { get; set; }
    public override byte Scale { get; set; }
    public override int Size { get; set; }

    public override void ResetDbType()
    {
        _dbType = DbType.String;
    }
}
