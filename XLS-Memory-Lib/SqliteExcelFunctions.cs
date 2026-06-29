using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class MemoryDbSqliteExcelFunctions
{
    private const string Category = "Memory DB - SQLite";

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.OPEN", Description = "Opens or creates a named SQLite in-memory connection, optionally from a database file path.", Category = Category)]
    public static object Open(string name, string path)
    {
        try
        {
            Manager.GetConnection(name, string.IsNullOrWhiteSpace(path) ? null : path);
            return "SUCCESS";
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.ATTACH", Description = "Attaches a SQLite database file or in-memory database to an existing connection.", Category = Category)]
    public static string Attach(string alias, string databaseId, string path = "", bool removeIfExist = false)
    {
        try
        {
            var connection = Manager.GetConnection(alias);
            return SqliteDB_Memory_Lib.SqLiteLiteTools.AttachedDataBase(connection, NullIfBlank(path), NullIfBlank(databaseId), removeIfExist).ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.DATABASES", Description = "Lists attached SQLite databases for a named connection.", Category = Category)]
    public static object[,] Databases(string alias)
    {
        try
        {
            var names = SqliteDB_Memory_Lib.SqLiteLiteTools.GetListDataBase(Manager.GetConnection(alias)) ?? [];
            return Tables.Vector("Database", names);
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.TABLES", Description = "Lists SQLite tables in the selected attached database.", Category = Category)]
    public static object[,] TablesList(string alias, string databaseId = "main")
    {
        try
        {
            var (status, tables) = SqliteDB_Memory_Lib.SqLiteLiteTools.GetListTables(Manager.GetConnection(alias), string.IsNullOrWhiteSpace(databaseId) ? "main" : databaseId);
            return status == SqliteDB_Memory_Lib.EnumsSqliteMemory.Output.SUCCESS
                ? Tables.Vector("Table", (tables ?? []))
                : Tables.ErrorTable(status.ToString());
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.CREATE.TABLE", Description = "Creates a SQLite table from an Excel range. First row must contain headers.", Category = Category)]
    public static string CreateTable(string alias, string table, object[,] range, string databaseId = "main")
    {
        return Relational.RelationalCreateTable(alias, table, range, databaseId, isDuckDb: false);
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.INSERT", Description = "Inserts rows into a SQLite table from an Excel range. First row must contain headers.", Category = Category)]
    public static string Insert(string alias, string table, object[,] range, string databaseId = "main")
    {
        if (!Tables.TryRangeToHeadersAndValues(range, out var headers, out var values, out var error)) return Error(error);
        try
        {
            var connection = Manager.GetConnection(alias);
            return SqliteDB_Memory_Lib.SqLiteLiteTools.Insert(connection, databaseId, table, headers, values, string.Empty).ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.EXECUTE", Description = "Executes a non-query SQL statement against a named SQLite connection.", Category = Category)]
    public static string Execute(string alias, string sql) => Relational.RelationalExecute(alias, sql, isDuckDb: false);

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.SCALAR", Description = "Executes a scalar SQL query against a named SQLite connection.", Category = Category)]
    public static object Scalar(string alias, string sql) => Relational.RelationalScalar(alias, sql, isDuckDb: false);

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.QUERY", Description = "Executes a SQLite query and spills the result as a two-dimensional array.", Category = Category)]
    public static object[,] Query(string alias, string sql, bool includeHeaders = true) => Relational.RelationalQuery(alias, sql, includeHeaders, isDuckDb: false);

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.DROP.TABLE", Description = "Drops a SQLite table from the selected attached database.", Category = Category)]
    public static string DropTable(string alias, string table, string databaseId = "main")
    {
        try
        {
            var (status, _) = SqliteDB_Memory_Lib.SqLiteLiteTools.DropTable(Manager.GetConnection(alias), databaseId, table);
            return status.ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.SAVE", Description = "Saves an attached SQLite database to a file.", Category = Category)]
    public static string Save(string alias, string databaseId, string path)
    {
        try
        {
            return SqliteDB_Memory_Lib.SqLiteLiteTools.SaveDataBase(Manager.GetConnection(alias), databaseId, path).ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.CLOSE", Description = "Closes a named SQLite connection.", Category = Category)]
    public static string Close(string alias)
    {
        Manager.CloseConnection(alias);
        return "SUCCESS";
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.CLOSE.ALL", Description = "Closes all SQLite connections.", Category = Category)]
    public static string CloseAll()
    {
        Manager.CloseAllConnections();
        return "SUCCESS";
    }

    private static SqliteDB_Memory_Lib.ConnectionManager Manager => SqliteDB_Memory_Lib.ConnectionManager.GetInstance();
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string Error(string message) => $"ERROR: {message}";
}
