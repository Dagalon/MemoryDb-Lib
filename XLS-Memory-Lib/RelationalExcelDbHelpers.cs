using ExcelDna.Integration;
using System.Data.Common;
using System.Globalization;

namespace XLS_Memory_Lib;

internal static class Relational
{
    internal static string RelationalCreateTable(string table, object[,] range, string databaseId, bool isDuckDb)
{
    if (!Tables.TryRangeToHeadersAndValues(range, out var headers, out var values, out var error)) return ExcelOutput.Error(error);

    try
    {
        if (isDuckDb)
        {
            ExecuteDuck(databaseId, BuildCreateTableSql(databaseId, table, headers, values));
            RelationalInsertRows(table, headers, values, databaseId, isDuckDb: true);
            return ExcelOutput.Success;
        }

        var connection = SqliteDB_Memory_Lib.ConnectionManager.GetInstance().GetConnection(databaseId);
        return ExcelOutput.FromStatus(SqliteDB_Memory_Lib.SqLiteLiteTools.CreateTable(connection, databaseId, table,  headers, values));
    }
    catch (Exception ex)
    {
        return ExcelOutput.Error(ex);
    }
}

internal static string RelationalExecute(string alias, string sql, bool isDuckDb, object[,]? parameters = null)
{
    if (string.IsNullOrWhiteSpace(sql)) return ExcelOutput.Error("sql is required");

    try
    {

        _ = isDuckDb ? ExecuteDuck(alias, sql, parameters) : ExecuteSqlite(alias, sql, parameters);
        return ExcelOutput.Success;
    }
    catch (Exception ex)
    {
        return ExcelOutput.Error(ex);
    }
}

    internal static object RelationalScalar(string alias, string sql, bool isDuckDb)
{
    if (string.IsNullOrWhiteSpace(sql)) return ExcelOutput.Error("sql is required");

    try
    {
        using var command = CreateCommand(alias, sql, isDuckDb);
        return command.ExecuteScalar() ?? ExcelEmpty.Value;
    }
    catch (Exception ex)
    {
        return ExcelOutput.Error(ex);
    }
}

internal static object[,] RelationalQuery(string alias, string sql, bool includeHeaders, bool isDuckDb, object[,]? parameters = null)
{
    if (string.IsNullOrWhiteSpace(sql)) return Tables.ErrorTable("sql is required");

    try
    {
        using var command = CreateCommand(alias, sql, isDuckDb);

        if (parameters is not null  &&  parameters[0, 0] is not ExcelMissing)
        {
            AddParameters(command, parameters, isDuckDb); 
        }
            
        
        using var reader = command.ExecuteReader();
        return Tables.ReaderToArray(reader, includeHeaders);
    }
    catch (Exception ex)
    {
        return Tables.ErrorTable(ex.Message);
    }
}

internal static void AddParameters(DbCommand command, object[,]? parameters, bool isDuckDb)
{
    if (parameters is null || parameters.GetLength(0) == 0) return;
    if (parameters.GetLength(1) < 2)
        throw new ArgumentException("parameters must contain two columns: name and value");

    for (var row = 0; row < parameters.GetLength(0); row++)
    {
        var rawName = Convert.ToString(parameters[row, 0], CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrWhiteSpace(rawName)) continue;

        var name = rawName.TrimStart('@', '$', ':');
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"parameter name at row {row + 1} is invalid");

        var parameter = command.CreateParameter();
        parameter.ParameterName = isDuckDb
            ? name
            : rawName[0] is '@' or '$' or ':' ? rawName : $"${name}";
        parameter.Value = Tables.NormalizeInput(parameters[row, 1]);
        command.Parameters.Add(parameter);
    }
}

internal static void RelationalInsertRows(string table, List<string> headers, object[,] values, string databaseId, bool isDuckDb)
{
        if (!isDuckDb) throw new InvalidOperationException("Use the SQLite helper for SQLite inserts.");

        var qualifiedTable = Qualify(databaseId, table);
        var fieldList = string.Join(", ", headers.Select(QuoteIdentifier));
        var rowCount = values.GetLength(0);
        var columnCount = values.GetLength(1);

        for (var row = 0; row < rowCount; row++)
        {
            var literals = new List<string>();
            for (var column = 0; column < columnCount; column++)
            {
                literals.Add(SqlLiteral(values[row, column]));
            }
         
            ExecuteDuck(databaseId,$"INSERT INTO {qualifiedTable} ({fieldList}) VALUES ({string.Join(", ", literals)})");
        }
}

internal static int ExecuteSqlite(string alias, string sql, object[,]? parameters = null)
{
    using var command = CreateCommand(alias, sql, isDuckDb: false);

    if (parameters is not null && parameters[0, 0] is not ExcelMissing)
    {
        AddParameters(command, parameters, isDuckDb: false);
    } 
    
    return command.ExecuteNonQuery();
}

internal static int ExecuteDuck(string alias, string sql, object[,]? parameters = null)
{
    using var command = CreateCommand(alias, sql, isDuckDb: true);


    if (parameters is not null && parameters[0, 0] is not ExcelMissing)
    {
        AddParameters(command, parameters, isDuckDb: true);
    }

    return command.ExecuteNonQuery();
}

internal static DbCommand CreateCommand(string alias, string sql, bool isDuckDb)
{
    DbCommand command = isDuckDb
        ? DuckDb_Memory_Lib.ConnectionManager.GetInstance().GetConnection(alias).CreateCommand()
        : SqliteDB_Memory_Lib.ConnectionManager.GetInstance().GetConnection(alias).CreateCommand();
    command.CommandText = sql;
    return command;
}

internal static string BuildCreateTableSql(string databaseId, string table, List<string> headers, object[,] values)
{
    var columns = headers.Select((header, index) => $"{QuoteIdentifier(header)} {InferDuckType(values, index)}");
    return $"CREATE OR REPLACE TABLE {Qualify(databaseId, table)} ({string.Join(", ", columns)})";
}

internal static string InferDuckType(object[,] values, int column)
{
    for (var row = 0; row < values.GetLength(0); row++)
    {
        var value = values[row, column];
        if (value == DBNull.Value) continue;

        return value switch
        {
            bool => "BOOLEAN",
            byte or short or int => "INTEGER",
            long => "BIGINT",
            float or double => "DOUBLE",
            decimal => "DECIMAL(38, 10)",
            DateTime => "TIMESTAMP",
            DateOnly => "DATE",
            TimeOnly => "TIME",
            _ => "VARCHAR"
        };
    }

    return "VARCHAR";
}

internal static string Qualify(string databaseId, string table)
{
    return string.IsNullOrWhiteSpace(databaseId) || databaseId.Equals("main", StringComparison.OrdinalIgnoreCase)
        ? QuoteIdentifier(table)
        : $"{QuoteIdentifier(databaseId)}.{QuoteIdentifier(table)}";
}

internal static string QuoteIdentifier(string identifier)
{
    return $"\"{identifier.Replace("\"", "\"\"")}\"";
}

internal static string SqlLiteral(object value)
{
    return value switch
    {
        null => "NULL",
        DBNull => "NULL",
        string text => $"'{text.Replace("'", "''")}'",
        bool flag => flag ? "TRUE" : "FALSE",
        DateTime dateTime => $"'{dateTime:O}'",
        DateOnly date => $"'{date:yyyy-MM-dd}'",
        TimeOnly time => $"'{time:HH:mm:ss.fffffff}'",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "NULL",
        _ => $"'{value.ToString()?.Replace("'", "''")}'"
    };
}
}
