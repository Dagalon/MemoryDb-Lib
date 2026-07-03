using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class MemoryDbSqliteExcelFunctions
{
    private const string Category = "Memory DB - SQLite";

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.CREATE_DB", Description = "Opens or creates a named SQLite in-memory connection, optionally from a database file path.", Category = Category)]
    public static object Open(string name, string path)
    {
        try
        {
            var conn =  Manager.GetConnection(name, string.IsNullOrWhiteSpace(path) ? null : path);
            var output = SqliteDB_Memory_Lib.SqLiteLiteTools.CreateDatabase(conn,  NullIfBlank(name), NullIfBlank(path));

            return output == SqliteDB_Memory_Lib.EnumsSqliteMemory.Output.SUCCESS
                ? "SUCCESS"
                : "ERROR-: " + output;

        }
        catch (Exception ex)
        {
            return  string.Concat("ERROR-: ", Error(ex.Message));
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
    public static object[,] Databases(string databaseId)
    {
        try
        {
            var names = SqliteDB_Memory_Lib.SqLiteLiteTools.GetListDataBase(Manager.GetConnection(databaseId)) ?? [];
            return Tables.Vector("Database", names);
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.TABLES", Description = "Lists SQLite tables in the selected attached database.", Category = Category)]
    public static object[,] TablesList(string databaseId, object dependency)
    {
        try
        {
            var (status, tables) = SqliteDB_Memory_Lib.SqLiteLiteTools.GetListTables(Manager.GetConnection(databaseId), string.IsNullOrWhiteSpace(databaseId) ? "main" : databaseId);
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
    public static string CreateTable(string databaseId, string table, object[,] range, object dependency)
    {
        try
        {
            var output =  Relational.RelationalCreateTable(table, range, databaseId, isDuckDb: false);

            if (output == "SUCCESS")
            {
                return "SUCCES";
            }

            return "ERROR:-" + output;
        }
        catch (Exception ex)
        {
            return "ERROR:-" + Error(ex.Message);
        }
    }
    
    [ExcelFunction(Name = "MEMORY_DB.SQLITE.INSERT", Description = "Inserts rows into a SQLite table from an Excel range. First row must contain headers.", Category = Category)]
    public static string Insert(string databaseId, string table, object[,] range, object dependency)
    {
        if (!Tables.TryRangeToHeadersAndValues(range, out var headers, out var values, out var error)) return Error(error);
        try
        {
            var connection = Manager.GetConnection(databaseId);
            return SqliteDB_Memory_Lib.SqLiteLiteTools.Insert(connection, databaseId, table, headers, values, string.Empty).ToString();
        }
        catch (Exception ex)
        {
            return  "ERROR:-" + Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.EXECUTE", Description = "Executes a non-query SQL statement against a named SQLite connection.", Category = Category)]
    public static string Execute(string databaseId, string sql, object dependency) => Relational.RelationalExecute(databaseId, sql, isDuckDb: false);

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.SCALAR", Description = "Executes a scalar SQL query against a named SQLite connection.", Category = Category)]
    public static object Scalar(string databaseId, string sql, object dependency) => Relational.RelationalScalar(databaseId, sql, isDuckDb: false);

    [ExcelFunction(
        Name = "MEMORY_DB.SQLITE.QUERY",
        Description = "Executes a SQLite query or a SQL file and spills the result as a two-dimensional array.",
        Category = Category)]
    public static object[,] Query(string databaseId,  string sql,  bool includeHeaders, object dependency)
    {
        try
        {
            var sqlText = SqliteDB_Memory_Lib.SqLiteLiteTools.ResolveSql(sql);
            return Relational.RelationalQuery(databaseId, sqlText, includeHeaders, isDuckDb: false);
        }
        catch (Exception ex)
        {
            return new object[,]
            {
                { "ERROR:- " + Error(ex.Message) }
            };
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.DROP.TABLE", Description = "Drops a SQLite table from the selected attached database.", Category = Category)]
    public static string? DropTable(string databaseId, string table)
    {
        try
        {
            var (status, msg) = SqliteDB_Memory_Lib.SqLiteLiteTools.DropTable(Manager.GetConnection(databaseId), databaseId, table);

            return status switch
            {
                SqliteDB_Memory_Lib.EnumsSqliteMemory.Output.SUCCESS => "SUCCESS",
                SqliteDB_Memory_Lib.EnumsSqliteMemory.Output.TABLE_NOT_FOUND => msg,
                _ => status.ToString()
            };
        }
        catch (Exception ex)
        {
            return  "ERROR:-" + Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.SAVE", Description = "Saves an attached SQLite database to a file.", Category = Category)]
    public static string Save(string databaseId, string path)
    {
        try
        {
            return SqliteDB_Memory_Lib.SqLiteLiteTools.SaveDataBase(Manager.GetConnection(databaseId), databaseId, path).ToString();
        }
        catch (Exception ex)
        {
            return  "ERROR:-" + Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.SQLITE.CLOSE", Description = "Closes a named SQLite connection.", Category = Category)]
    public static string Close(string databaseId)
    {
        Manager.CloseConnection(databaseId);
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
