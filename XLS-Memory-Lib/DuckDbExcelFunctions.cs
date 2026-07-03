using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class MemoryDbDuckDbExcelFunctions
{
    private const string Category = "Memory DB - DuckDB";

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CREATE_DB", Description = "Opens or creates a named DuckDB in-memory connection, optionally from a database file path.", Category = Category)]
    public static object Open(string name, string path)
    {
        try
        {
            var conn = Manager.GetConnection(name, NullIfBlank(path));
            var output = DuckDb_Memory_Lib.DuckTools.CreateDatabase(conn, NullIfBlank(name), NullIfBlank(path));

            return output == DuckDb_Memory_Lib.EnumsDuckMemory.Output.SUCCESS
                ? "SUCCESS"
                : "ERROR-: " + output;
        }
        catch (Exception ex)
        {
            return string.Concat("ERROR-: ", Error(ex.Message));
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
    public static object[,] Databases(string databaseId)
    {
        try
        {
            var names = DuckDb_Memory_Lib.DuckTools.GetListDataBase(Manager.GetConnection(databaseId)) ?? [];
            return Tables.Vector("Database", names);
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.TABLES", Description = "Lists DuckDB tables in the selected attached database.", Category = Category)]
    public static object[,] TablesList(string databaseId, object dependency)
    {
        try
        {
            var (status, tables) = DuckDb_Memory_Lib.DuckTools.GetListTables(Manager.GetConnection(databaseId), string.IsNullOrWhiteSpace(databaseId) ? "main" : databaseId);
            return status == DuckDb_Memory_Lib.EnumsDuckMemory.Output.SUCCESS
                ? Tables.Vector("Table", tables ?? [])
                : Tables.ErrorTable(status.ToString());
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CREATE.TABLE", Description = "Creates a DuckDB table from an Excel range. First row must contain headers.", Category = Category)]
    public static string CreateTable(string databaseId, string table, object[,] range, object dependency)
    {
        try
        {
            var output = Relational.RelationalCreateTable(table, range, databaseId, isDuckDb: true);
            return output == "SUCCESS" ? "SUCCESS" : "ERROR:-" + output;
        }
        catch (Exception ex)
        {
            return "ERROR:-" + Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.INSERT", Description = "Inserts rows into a DuckDB table from an Excel range. First row must contain headers.", Category = Category)]
    public static string Insert(string databaseId, string table, object[,] range, object dependency)
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
    public static string Execute(string databaseId, string sql, object dependency) => Relational.RelationalExecute(databaseId, sql, isDuckDb: true);

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.SCALAR", Description = "Executes a scalar SQL query against a named DuckDB connection.", Category = Category)]
    public static object Scalar(string databaseId, string sql, object dependency) => Relational.RelationalScalar(databaseId, sql, isDuckDb: true);

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.QUERY", Description = "Executes a DuckDB query or a SQL file and spills the result as a two-dimensional array.", Category = Category)]
    public static object[,] Query(string databaseId, string sql, bool includeHeaders, object dependency)
    {
        try
        {
            var sqlText = DuckDb_Memory_Lib.DuckTools.ResolveSql(sql);
            return Relational.RelationalQuery(databaseId, sqlText, includeHeaders, isDuckDb: true);
        }
        catch (Exception ex)
        {
            return new object[,] { { "ERROR:- " + Error(ex.Message) } };
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.DROP.TABLE", Description = "Drops a DuckDB table from the selected attached database.", Category = Category)]
    public static string? DropTable(string databaseId, string table)
    {
        try
        {
            var (status, msg) = DuckDb_Memory_Lib.DuckTools.DropTable(Manager.GetConnection(databaseId), databaseId, table);
            return status switch
            {
                DuckDb_Memory_Lib.EnumsDuckMemory.Output.SUCCESS => "SUCCESS",
                DuckDb_Memory_Lib.EnumsDuckMemory.Output.TABLE_NOT_FOUND => msg,
                _ => status.ToString()
            };
        }
        catch (Exception ex)
        {
            return "ERROR:-" + Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.DUCKDB.CLOSE", Description = "Closes a named DuckDB connection.", Category = Category)]
    public static string Close(string databaseId)
    {
        Manager.CloseConnection(databaseId);
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
