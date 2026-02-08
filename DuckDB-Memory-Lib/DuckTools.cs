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
                attachedQry = $"ATTACH '{strConnection}' AS \"{aliasDataBase}\" ";
            }

            try
            {
                var cmd = new DuckDBCommand(attachedQry, db);
                cmd.ExecuteNonQuery();
            }
            catch (DuckDBException)
            {
                return EnumsDuckMemory.Output.ERROR_TO_ATTACHED_DATABASE;
            }

            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch
        {
            return EnumsDuckMemory.Output.DB_NOT_FOUND;
        }
    }
  
}