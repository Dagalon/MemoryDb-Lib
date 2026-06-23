using System.Data.Common;
using System.Globalization;
using ExcelDna.Integration;

namespace XLS_Memory_Lib;

internal static class Relational
{
    internal static string RelationalCreateTable(string alias, string table, object[,] range, string databaseId, bool isDuckDb)
{
    if (!Tables.TryRangeToHeadersAndValues(range, out var headers, out var values, out var error)) return $"ERROR: {error}";

    try
    {
        if (isDuckDb)
        {
            ExecuteDuck(alias, BuildCreateTableSql(databaseId, table, headers, values));
            RelationalInsertRows(alias, table, headers, values, databaseId, isDuckDb: true);
            return "SUCCESS";
        }

        var connection = SqliteDB_Memory_Lib.ConnectionManager.GetInstance().GetConnection(alias);
        return SqliteDB_Memory_Lib.SqLiteLiteTools.CreateTable(connection, databaseId, table, headers, values).ToString();
    }
    catch (Exception ex)
    {
        return $"ERROR: {ex.Message}";
    }
}

    internal static string RelationalExecute(string alias, string sql, bool isDuckDb)
{
    if (string.IsNullOrWhiteSpace(sql)) return "ERROR: sql is required";

    try
    {
        var affectedRows = isDuckDb ? ExecuteDuck(alias, sql) : ExecuteSqlite(alias, sql);
        return affectedRows.ToString(CultureInfo.InvariantCulture);
    }
    catch (Exception ex)
    {
        return $"ERROR: {ex.Message}";
    }
}

    internal static object RelationalScalar(string alias, string sql, bool isDuckDb)
{
    if (string.IsNullOrWhiteSpace(sql)) return "ERROR: sql is required";

    try
    {
        using var command = CreateCommand(alias, sql, isDuckDb);
        return command.ExecuteScalar() ?? ExcelEmpty.Value;
    }
    catch (Exception ex)
    {
        return $"ERROR: {ex.Message}";
    }
}

    internal static object[,] RelationalQuery(string alias, string sql, bool includeHeaders, bool isDuckDb)
{
    if (string.IsNullOrWhiteSpace(sql)) return Tables.ErrorTable("sql is required");

    try
    {
        using var command = CreateCommand(alias, sql, isDuckDb);
        using var reader = command.ExecuteReader();
        return Tables.ReaderToArray(reader, includeHeaders);
    }
    catch (Exception ex)
    {
        return Tables.ErrorTable(ex.Message);
    }
}

    internal static void RelationalInsertRows(string alias, string table, List<string> headers, object[,] values, string databaseId, bool isDuckDb)
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

        ExecuteDuck(alias, $"INSERT INTO {qualifiedTable} ({fieldList}) VALUES ({string.Join(", ", literals)})");
    }
}

    internal static int ExecuteSqlite(string alias, string sql)
{
    using var command = CreateCommand(alias, sql, isDuckDb: false);
    return command.ExecuteNonQuery();
}

    internal static int ExecuteDuck(string alias, string sql)
{
    using var command = CreateCommand(alias, sql, isDuckDb: true);
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
