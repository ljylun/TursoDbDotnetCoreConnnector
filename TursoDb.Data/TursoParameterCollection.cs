using System.Collections;
using System.Data;
using System.Data.Common;

namespace TursoDb.Data;

/// <summary>
/// ADO.NET DbParameterCollection implementation for Turso.
/// </summary>
public class TursoParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters;

    public TursoParameterCollection()
    {
        _parameters = new List<DbParameter>();
    }

    public TursoParameterCollection(List<DbParameter> parameters)
    {
        _parameters = parameters;
    }

    public override int Count => _parameters.Count;
    public override object SyncRoot => _parameters;

    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var item in values)
            _parameters.Add((DbParameter)item);
    }

    public override void Clear() => _parameters.Clear();

    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index)
    {
        foreach (var p in _parameters)
            array.SetValue(p, index++);
    }

    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName)
    {
        for (int i = 0; i < _parameters.Count; i++)
        {
            if (_parameters[i].ParameterName == parameterName)
                return i;
        }
        return -1;
    }

    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _parameters.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _parameters.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters.RemoveAt(index);
    }

    protected override DbParameter GetParameter(int index) => _parameters[index];

    protected override DbParameter GetParameter(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
            throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
        return _parameters[index];
    }

    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
            throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
        _parameters[index] = value;
    }
}
