using DuckDB.NET.Data;
using System.IO;

namespace DuckDb_Memory_Lib;

public static class QueryExecutor
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
    /// Creates or replaces a table from a parquet file.
    /// </summary>
    public static EnumsDuckMemory.Output CreateParquetTable(DuckDBConnection db, string idDataBase, string idTable, string parquetPathFile)
    {
        if (string.IsNullOrWhiteSpace(parquetPathFile))
        {
            return EnumsDuckMemory.Output.PATH_IS_NULL_OR_EMPTY;
        }

        if (!File.Exists(parquetPathFile))
        {
            return EnumsDuckMemory.Output.PATH_NOT_FOUND;
        }

        try
        {
            var tableName = $"{DuckTools.QuoteIdentifier(idDataBase)}.{DuckTools.QuoteIdentifier(idTable)}";
            var parquetPath = parquetPathFile.Replace("'", "''");
            var qry = $"CREATE OR REPLACE TABLE {tableName} AS SELECT * FROM '{parquetPath}';";
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
    /// Executes a SQL statement and returns the resulting rows.
    /// </summary>
    public static (EnumsDuckMemory.Output, List<Dictionary<string, object>>) ExecuteQryReader(DuckDBConnection db, string qry)
    {
        try
        {
            var cmd = new DuckDBCommand(qry, db);
            var qryResult = cmd.ExecuteReader();

            var resultList = new List<Dictionary<string, object>>();

            if (qryResult.HasRows)
            {
                while (qryResult.Read())
                {
                    resultList.Add(Enumerable.Range(0, qryResult.FieldCount)
                        .ToDictionary(qryResult.GetName, qryResult.GetValue));
                }
            }

            qryResult.Close();
            return (EnumsDuckMemory.Output.SUCCESS, resultList);
        }
        catch (Exception)
        {
            return (EnumsDuckMemory.Output.ERROR_TO_EXECUTE_QUERY, []);
        }
    }

    /// <summary>
    /// Executes a parameterized SQL statement and returns the resulting rows.
    /// </summary>
    public static (EnumsDuckMemory.Output, List<Dictionary<string, object>>) ExecuteQryReader(DuckDBConnection db, string qry, Dictionary<string, string> parameters)
    {
        try
        {
            qry = parameters.Keys.Aggregate(qry,
                (current, param) => current.Replace(param, parameters[param], StringComparison.OrdinalIgnoreCase));
            return ExecuteQryReader(db, qry);
        }
        catch (Exception ex)
        {
           throw new Exception( $"{ex.Message}-{EnumsDuckMemory.Output.ERROR_TO_EXECUTE_QUERY}");
        }
    }
}