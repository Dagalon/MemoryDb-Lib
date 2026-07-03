using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class MemoryDbDuckDbExcelFunctions
{
    private const string Category = "Memory DB - DuckDB";

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.OPEN", Description = "Opens or creates a named DuckDB in-memory connection, optionally from a database file path.", Category = Category)]
    public static string Open(string alias, string path = "")
    {
        try
        {
            
            Manager.GetConnection(alias, string.IsNullOrWhiteSpace(path) ? null : path);
            return "SUCCESS";
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.ATTACH", Description = "Attaches a DuckDB database file or in-memory database to an existing connection.", Category = Category)]
    public static string Attach(string alias, string databaseId, string path = "", bool removeIfExist = false)
    {
        try
        {
            var connection = Manager.GetConnection(alias);
            return DuckDb_Memory_Lib.DuckTools.AttachedDataBase(connection, NullIfBlank(path), NullIfBlank(databaseId), removeIfExist).ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.DATABASES", Description = "Lists attached DuckDB databases for a named connection.", Category = Category)]
    public static object[,] Databases(string alias)
    {
        try
        {
            var names = DuckDb_Memory_Lib.DuckTools.GetListDataBase(Manager.GetConnection(alias)) ?? [];
            return Tables.Vector("Database", names.Cast<object>());
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CREATE.TABLE", Description = "Creates a DuckDB table from an Excel range. First row must contain headers.", Category = Category)]
    public static string CreateTable(string table, object[,] range, string databaseId = "main")
    {
        return Relational.RelationalCreateTable(table, range, databaseId, isDuckDb: true);
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.INSERT", Description = "Inserts rows into a DuckDB table from an Excel range. First row must contain headers.", Category = Category)]
    public static string Insert(string alias, string table, object[,] range, string databaseId = "main")
    {
        if (!Tables.TryRangeToHeadersAndValues(range, out var headers, out var values, out var error)) return Error(error);
        try
        {
            Relational.RelationalInsertRows(table, headers, values, databaseId, isDuckDb: true);
            return "SUCCESS";
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.EXECUTE", Description = "Executes a non-query SQL statement against a named DuckDB connection.", Category = Category)]
    public static string Execute(string alias, string sql) => Relational.RelationalExecute(alias, sql, isDuckDb: true);

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.SCALAR", Description = "Executes a scalar SQL query against a named DuckDB connection.", Category = Category)]
    public static object Scalar(string alias, string sql) => Relational.RelationalScalar(alias, sql, isDuckDb: true);

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.QUERY", Description = "Executes a DuckDB query and spills the result as a two-dimensional array.", Category = Category)]
    public static object[,] Query(string alias, string sql, bool includeHeaders = true) => Relational.RelationalQuery(alias, sql, includeHeaders, isDuckDb: true);

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CLOSE", Description = "Closes a named DuckDB connection.", Category = Category)]
    public static string Close(string alias)
    {
        Manager.CloseConnection(alias);
        return "SUCCESS";
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CLOSE.ALL", Description = "Closes all DuckDB connections.", Category = Category)]
    public static string CloseAll()
    {
        Manager.CloseAllConnections();
        return "SUCCESS";
    }

    private static DuckDb_Memory_Lib.ConnectionManager Manager => DuckDb_Memory_Lib.ConnectionManager.GetInstance();
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string Error(string message) => $"ERROR: {message}";
}
