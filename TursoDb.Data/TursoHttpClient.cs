using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TursoDb.Data;

/// <summary>
/// Configuration options for connecting to a Turso database.
/// </summary>
public class TursoOptions
{
    public string DatabaseUrl { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string PipelineVersion { get; set; } = "v2";
}

/// <summary>
/// HTTP client for executing SQL statements against a Turso database
/// using the Hrana over HTTP protocol (POST /v2/pipeline).
/// </summary>
public class TursoHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TursoOptions _options;
    private readonly ILogger? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TursoHttpClient(TursoOptions options, ILogger? logger = null, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.DatabaseUrl))
            throw new ArgumentException("DatabaseUrl is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.AuthToken))
            throw new ArgumentException("AuthToken is required.", nameof(options));

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.AuthToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<long> ExecuteNonQueryAsync(string sql, params TursoParameter[] parameters)
    {
        var result = await ExecuteInternalAsync(sql, parameters);
        return result.AffectedRowCount;
    }

    public async Task<TursoQueryResult> QueryAsync(string sql, params TursoParameter[] parameters)
    {
        return await ExecuteInternalAsync(sql, parameters);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, params TursoParameter[] parameters)
    {
        var result = await ExecuteInternalAsync(sql, parameters);
        if (result.Rows.Count == 0 || result.Columns.Count == 0)
            return null;
        return result.Rows[0][0];
    }

    private async Task<TursoQueryResult> ExecuteInternalAsync(string sql, TursoParameter[] parameters)
    {
        var pipelineUrl = $"{_options.DatabaseUrl.TrimEnd('/')}/{_options.PipelineVersion}/pipeline";
        var request = BuildRequest(sql, parameters);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        _logger?.LogInformation("Turso request: {Json}", json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(pipelineUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("Turso HTTP error {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new TursoHttpException((int)response.StatusCode,
                $"Turso API error: {response.StatusCode}. Body: {responseBody}");
        }

        _logger?.LogInformation("Turso response: {Body}", responseBody);
        return ParseResponse(responseBody);
    }

    private static TursoPipelineRequest BuildRequest(string sql, TursoParameter[] parameters)
    {
        var stmt = new TursoStatement { Sql = sql };
        if (parameters.Length > 0)
        {
            stmt.Args = parameters.Select(p => new TursoArg
            {
                Type = p.LibsqlType,
                Value = p.Value
            }).ToList();
        }

        return new TursoPipelineRequest
        {
            Requests =
            [
                new TursoRequestItem { Type = "execute", Stmt = stmt },
                new TursoRequestItem { Type = "close" }
            ]
        };
    }

    private static TursoQueryResult ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = new TursoQueryResult();

        if (!root.TryGetProperty("results", out var resultsElement))
            return result;

        foreach (var item in resultsElement.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var typeElement))
                continue;

            var type = typeElement.GetString();
            if (type == "ok" && item.TryGetProperty("response", out var responseElement))
            {
                if (responseElement.TryGetProperty("type", out var responseType) &&
                    responseType.GetString() == "execute" &&
                    responseElement.TryGetProperty("result", out var execResult))
                {
                    if (execResult.TryGetProperty("cols", out var colsElement))
                    {
                        foreach (var col in colsElement.EnumerateArray())
                        {
                            if (col.TryGetProperty("name", out var nameElement))
                                result.Columns.Add(nameElement.GetString() ?? string.Empty);
                        }
                    }

                    if (execResult.TryGetProperty("rows", out var rowsElement))
                    {
                        foreach (var row in rowsElement.EnumerateArray())
                        {
                            var rowValues = new List<object?>();
                            foreach (var cell in row.EnumerateArray())
                                rowValues.Add(ParseJsonValue(cell));
                            result.Rows.Add(rowValues);
                        }
                    }

                    if (execResult.TryGetProperty("affected_row_count", out var affectedElement))
                        result.AffectedRowCount = affectedElement.ValueKind == JsonValueKind.Number
                            ? affectedElement.GetInt64()
                            : long.TryParse(affectedElement.GetString(), out var ac) ? ac : 0;

                    if (execResult.TryGetProperty("last_insert_rowid", out var rowidElement) &&
                        rowidElement.ValueKind != JsonValueKind.Null)
                    {
                        result.LastInsertRowId = rowidElement.ValueKind == JsonValueKind.Number
                            ? rowidElement.GetInt64()
                            : long.TryParse(rowidElement.GetString(), out var rid) ? rid : 0;
                    }
                }
            }
            else if (type == "error")
            {
                var message = item.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetProperty("message").GetString()
                    : "Unknown error";
                throw new TursoHttpException(400, $"Turso SQL error: {message}");
            }
        }

        return result;
    }

    private static object? ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => ParseTypedValue(element),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Turso may return typed values as objects like {"type": "integer", "value": "5"}.
    /// This method extracts the actual value from such typed objects.
    /// </summary>
    private static object? ParseTypedValue(JsonElement element)
    {
        if (element.TryGetProperty("type", out var typeProp) &&
            element.TryGetProperty("value", out var valueProp))
        {
            var type = typeProp.GetString();
            return type switch
            {
                "null" => null,
                "integer" => valueProp.ValueKind == JsonValueKind.Number
                    ? valueProp.GetInt64()
                    : long.TryParse(valueProp.GetString(), out var l) ? l : valueProp.GetString(),
                "float" => valueProp.ValueKind == JsonValueKind.Number
                    ? valueProp.GetDouble()
                    : double.TryParse(valueProp.GetString(), out var d) ? d : valueProp.GetString(),
                "text" => valueProp.GetString(),
                "blob" => valueProp.GetString(),
                _ => valueProp.ToString()
            };
        }

        // If it's an object without type/value, just return the string representation
        return element.ToString();
    }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Represents a parameter to bind to a SQL statement.
/// </summary>
public class TursoParameter
{
    public string LibsqlType { get; }
    public object? Value { get; }

    private TursoParameter(string libsqlType, object? value)
    {
        LibsqlType = libsqlType;
        Value = value;
    }

    public static TursoParameter Null() => new("null", null);
    public static TursoParameter Integer(long value) => new("integer", value);
    public static TursoParameter Real(double value) => new("float", value);
    public static TursoParameter Text(string value) => new("text", value);
    public static TursoParameter Blob(byte[] value) => new("blob", Convert.ToBase64String(value));

    public static TursoParameter FromObject(object? value)
    {
        return value switch
        {
            null => Null(),
            long l => Integer(l),
            int i => Integer(i),
            short s => Integer(s),
            byte b => Integer(b),
            ulong ul => Integer((long)ul),
            uint ui => Integer(ui),
            ushort us => Integer(us),
            double d => Real(d),
            float f => Real(f),
            decimal dec => Real((double)dec),
            string t => Text(t),
            bool b => Integer(b ? 1 : 0),
            byte[] ba => Blob(ba),
            DateTime dt => Text(dt.ToString("O")),
            Guid g => Text(g.ToString()),
            _ => Text(value.ToString() ?? string.Empty)
        };
    }
}

/// <summary>
/// Result of a Turso SQL query execution.
/// </summary>
public class TursoQueryResult
{
    public List<string> Columns { get; } = [];
    public List<List<object?>> Rows { get; } = [];
    public long AffectedRowCount { get; set; }
    public long? LastInsertRowId { get; set; }
}

internal class TursoPipelineRequest
{
    [JsonPropertyName("requests")]
    public List<TursoRequestItem> Requests { get; set; } = [];
}

internal class TursoRequestItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("stmt")]
    public TursoStatement? Stmt { get; set; }
}

internal class TursoStatement
{
    [JsonPropertyName("sql")]
    public string Sql { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    [JsonConverter(typeof(TursoArgsConverter))]
    public List<TursoArg>? Args { get; set; }
}

/// <summary>
/// Custom JSON converter for the args list.
/// Turso's Hrana protocol requires:
/// - "float" type → value as JSON number
/// - "integer" type → value as JSON string
/// - "text" type → value as JSON string
/// - "null" type → null
/// </summary>
internal class TursoArgsConverter : JsonConverter<List<TursoArg>>
{
    public override void Write(Utf8JsonWriter writer, List<TursoArg> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var arg in value)
        {
            writer.WriteStartObject();
            writer.WriteString("type", arg.Type);

            writer.WritePropertyName("value");
            if (arg.Value is null)
            {
                writer.WriteNullValue();
            }
            else if (arg.Type == "float" && arg.Value is double d)
            {
                writer.WriteNumberValue(d);
            }
            else if (arg.Type == "float" && arg.Value is float f)
            {
                writer.WriteNumberValue(f);
            }
            else
            {
                writer.WriteStringValue(arg.Value.ToString());
            }

            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    public override List<TursoArg> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<TursoArg>();
        if (reader.TokenType != JsonTokenType.StartArray)
            return result;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var arg = new TursoArg();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    var propName = reader.GetString();
                    reader.Read();

                    if (propName == "type")
                        arg.Type = reader.GetString() ?? string.Empty;
                    else if (propName == "value")
                    {
                        arg.Value = reader.TokenType switch
                        {
                            JsonTokenType.Null => null,
                            JsonTokenType.String => reader.GetString(),
                            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
                            JsonTokenType.Number => reader.GetDouble(),
                            JsonTokenType.True => true,
                            JsonTokenType.False => false,
                            _ => reader.GetString()
                        };
                    }
                }
                result.Add(arg);
            }
        }
        return result;
    }
}

internal class TursoArg
{
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }
}

/// <summary>
/// Exception thrown when the Turso API returns an error.
/// </summary>
public class TursoHttpException : Exception
{
    public int StatusCode { get; }
    public TursoHttpException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
