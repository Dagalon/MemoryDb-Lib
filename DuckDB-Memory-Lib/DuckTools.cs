using System.Diagnostics;
using DuckDB.NET.Data;
using DuckDB.NET.Data.DataChunk.Reader;
using DuckDB.NET.Data.DataChunk.Writer;

namespace DuckDb_Memory_Lib;

public static class DuckTools
{
    /// <summary>
    /// Creates a new DuckDb connection using the provided path or an in-memory data source.
    /// </summary>
    public static DuckDBConnection GetInstance(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new DuckDBConnection("Data Source=:memory:?Cache=Shared");
        }

        return File.Exists(path)
            ? new DuckDBConnection($"Data Source={path};Mode=Memory")
            : new DuckDBConnection("Data Source=:memory:");
    }
    
    /// <summary>
    /// Creates or attaches an DuckDB database to the provided connection.
    /// </summary>
    public static EnumsDuckMemory.Output CreateDatabase(DuckDBConnection connection, string? idDataBase, string? path)
    {
        var listDataBase = GetListDataBase(connection);

        if (listDataBase != null && idDataBase != null && listDataBase.Contains(idDataBase))
        {
            return EnumsDuckMemory.Output.SUCCESS;
        }

        if (string.IsNullOrEmpty(idDataBase))
        {

            if (string.IsNullOrEmpty(path))
            {
                return EnumsDuckMemory.Output.PATH_AND_ID_IS_NULL_OR_EMPTY;
            }
                   
            idDataBase = Path.GetFileName(path).Split('.')[0];

        }
               
        if (path != null && KeeperRegisterIdDataBase.CheckPathDataBase(path))
        {
            return EnumsDuckMemory.Output.THERE_EXISTS_DATABASE;
        }

        if (!string.IsNullOrEmpty(path))
        {
            KeeperRegisterIdDataBase.Register(path, idDataBase);
        }

        var attachedOutPut= AttachedDataBase(connection, path, idDataBase);

        if (attachedOutPut == EnumsDuckMemory.Output.ERROR_TO_ATTACHED_DATABASE)
        {
            return EnumsDuckMemory.Output.ERROR_TO_ATTACHED_DATABASE;
        }

        return EnumsDuckMemory.Output.SUCCESS;
    }
    

    /// <summary>
    /// Escapes and quotes a SQL identifier so it can be safely used in DuckDB statements.
    /// </summary>
    public static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("SQL identifier cannot be null or empty.", nameof(identifier));

        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Generates a unique SQL parameter name for use in parameterized SQL commands.
    /// </summary>
    public static string ParameterName(int index)
    {
        return "@p" + index;
    }

    /// <summary>
    /// Resolves the specified SQL input by reading it from a text file when a valid file path is provided; otherwise, returns the input as a SQL statement.
    /// </summary>
    public static string ResolveSql(string sqlOrPath)
    {
        if (string.IsNullOrWhiteSpace(sqlOrPath))
            throw new ArgumentException("SQL query or file path cannot be empty.", nameof(sqlOrPath));

        return File.Exists(sqlOrPath) ? File.ReadAllText(sqlOrPath) : sqlOrPath;
    }

    /// <summary>
    /// Retrieves the list of database aliases attached to the connection.
    /// </summary>
    public static List<string>? GetListDataBase(DuckDBConnection db)
    {

        var cmd = new DuckDBCommand("PRAGMA database_list", db);
        var dataBases = cmd.ExecuteReader();
        List<string> idList = [];

        while (dataBases.Read())
        {
            idList.Add(dataBases[1].ToString()!);
        }

        dataBases.Close();
        return idList;
    }

    /// <summary>
    /// Register a user-defined function (UDF).
    /// </summary>

#pragma warning disable DuckDBNET001
    public static EnumsDuckMemory.Output RegisterScalarFunction<TInput, TOutput>(
        DuckDBConnection db,
        string idFunction,
        Action<IReadOnlyList<IDuckDBDataReader>, IDuckDBDataWriter, ulong> func)
    {
        db.RegisterScalarFunction<TInput, TOutput>(
            idFunction,
            func,
            isPureFunction: true);

        return EnumsDuckMemory.Output.SUCCESS;
    }
#pragma warning restore DuckDBNET001

    /// <summary>
    /// Attaches an external database file to the provided connection, creating the file when required.
    /// </summary>
    public static EnumsDuckMemory.Output AttachedDataBase(DuckDBConnection db, string? path, string? aliasDataBase, bool removeIfExist=false)
    {
        try
        {
            if (!string.IsNullOrEmpty(path)){
                if (File.Exists(path))
                {
                    if (removeIfExist)
                    {
                        File.Delete(path);
                    }

                }
                       
                var directory = Path.GetDirectoryName(path);
                if (Directory.Exists(directory))
                {
                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();
                    }

                }
                else
                {
                    if (directory != null) Directory.CreateDirectory(directory);
                    File.Create(path).Close();
                }

            }

            var strConnection =   string.IsNullOrEmpty(path) ? ":memory:" : path;
            string  attachedQry;
            if (String.IsNullOrEmpty(aliasDataBase))
            {
                attachedQry = $"ATTACH '{strConnection}'";
            }
            else
            {
                attachedQry = $"ATTACH '{strConnection}' AS {QuoteIdentifier(aliasDataBase)}";
            }

            try
            {
                var cmd = new DuckDBCommand(attachedQry, db);
                cmd.ExecuteNonQuery();
            }
            catch (DuckDBException ex)
            {
                CaptureException(ex);
                return EnumsDuckMemory.Output.ERROR_TO_ATTACHED_DATABASE;
            }

            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception ex)
        {
            CaptureException(ex);
            return EnumsDuckMemory.Output.DB_NOT_FOUND;
        }
    }
    
    /// <summary>
    /// Retrieves the list of tables contained in the specified database alias.
    /// </summary>
    public static (EnumsDuckMemory.Output, List<string>?) GetListTables(DuckDBConnection db, string idDataBase)
    {
        var dataBases = GetListDataBase(db);

        if (dataBases == null || !dataBases.Contains(idDataBase))
        {
            return (EnumsDuckMemory.Output.DB_NOT_FOUND, null);
        }

        try
        {
            List<string>? tables = [];
            var qry = $"SELECT table_name FROM information_schema.tables WHERE table_catalog = '{idDataBase.Replace("'", "''")}' AND table_type = 'BASE TABLE';";
            var cmd = new DuckDBCommand(qry, db);
            var qryReader = cmd.ExecuteReader();

            while (qryReader.Read())
            {
                tables.Add(qryReader[0].ToString()!);
            }

            qryReader.Close();
            if (tables.Count == 0)
            {
                tables.Add($"There is not tables in {idDataBase}");
            }

            return (EnumsDuckMemory.Output.SUCCESS, tables);
        }
        catch (Exception ex)
        {
            CaptureException(ex);
            return (EnumsDuckMemory.Output.DB_NOT_FOUND, null);
        }
    }

    /// <summary>
    /// Drops a table from the specified database alias when it exists.
    /// </summary>
    public static (EnumsDuckMemory.Output, string?) DropTable(DuckDBConnection db, string idDataBase, string idTable)
    {
        if (string.IsNullOrEmpty(idDataBase))
        {
            idDataBase = "main";
        }

        var tablesResult = GetListTables(db, idDataBase);
        if (tablesResult.Item1 != EnumsDuckMemory.Output.SUCCESS)
        {
            return (tablesResult.Item1, $"Error accessing database {idDataBase}");
        }

        var listTables = tablesResult.Item2;
        if (listTables == null || !listTables.Contains(idTable))
            return (EnumsDuckMemory.Output.TABLE_NOT_FOUND, $"The data base {idDataBase} doesn't contain the table {idTable}");

        try
        {
            var qry = $"DROP TABLE {QuoteIdentifier(idDataBase)}.{QuoteIdentifier(idTable)}";
            var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();
            return (EnumsDuckMemory.Output.SUCCESS, "");
        }
        catch (Exception ex)
        {
            CaptureException(ex);
            return (EnumsDuckMemory.Output.DB_NOT_FOUND, "Error dropping table");
        }
    }

    /// <summary>
    /// Detaches a database alias from the connection.
    /// </summary>
    public static EnumsDuckMemory.Output DeleteDataBase(DuckDBConnection db, string idDatabase)
    {
        if (string.IsNullOrEmpty(idDatabase))
        {
            return EnumsDuckMemory.Output.PATH_IS_NULL_OR_EMPTY;
        }

        try
        {
            var qry = $"DETACH DATABASE {QuoteIdentifier(idDatabase)}";
            var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();
            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception ex)
        {
            CaptureException(ex);
            return EnumsDuckMemory.Output.DB_NOT_FOUND;
        }
    }

    private static void CaptureException(Exception exception)
    {
        Debug.WriteLine(exception);
    }
}
