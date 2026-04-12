using DuckDB.NET.Data;
using System.IO;

namespace DuckDb_Memory_Lib;

public class QueryExecutor
{
    
    /// <summary>
    /// Creates a table with the provided column definitions.
    /// </summary>
    public static EnumsDuckMemory.Output CreateTable(DuckDBConnection db, string idDataBase, string idTable,
        List<string> headers, List<Type>? types = null)
    {

        var fieldsDefinition = new List<string>();
        var noFields = headers.Count;

        if (types is null)
        {
            for (var i = 0; i < noFields; i++)
            {
                fieldsDefinition.Add(headers[i]);
            }
               
        }
        else
        {
            for (var i = 0; i < noFields; i++)
            {
                var t = NetTypeToDuckDbType.GetDuckDbType(types[i]);
                fieldsDefinition.Add(headers[i] + " " + t);
            }
               
        }

        try
        {
            var qry = $"CREATE TABLE IF NOT EXISTS {idDataBase}.{idTable}({string.Join(",", fieldsDefinition)})";
            var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();
            
            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception)
        {
            return EnumsDuckMemory.Output.ERROR_TO_EXECUTE_QUERY;
        }

    }
    
    /// <summary>
    /// Creates a table from parquet file
    /// </summary>
    
    public static EnumsDuckMemory.Output CreateParquetTable(DuckDBConnection db, string idDataBase, string idTable, string parquetPathFile)
    {
        if (!File.Exists(parquetPathFile))
        {
            return EnumsDuckMemory.Output.PATH_NOT_FOUND;
        }

        try
        {
            var qry = $@"CREATE TABLE ""{idDataBase}"".""{idTable}"" AS SELECT * FROM '{parquetPathFile}';";
            var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();

            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception)
        {
            return EnumsDuckMemory.Output.ERROR_TO_EXECUTE_QUERY;
        }
    }
}