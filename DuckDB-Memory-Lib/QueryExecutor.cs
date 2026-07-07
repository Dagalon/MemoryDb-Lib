using System.Diagnostics;
using DuckDB.NET.Data;
using System.IO;
using DuckDB.NET.Native;

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
                fieldsDefinition.Add(DuckTools.QuoteIdentifier(headers[i]) + " " + t);
            }
               
        }

        try
        {
            var tableName = $"{DuckTools.QuoteIdentifier(idDataBase)}.{DuckTools.QuoteIdentifier(idTable)}";
            var qry = $"CREATE TABLE IF NOT EXISTS {tableName}({string.Join(",", fieldsDefinition)})";
            using var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();
            
            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
            using var cmd = new DuckDBCommand(qry, db);
            cmd.ExecuteNonQuery();

            return EnumsDuckMemory.Output.SUCCESS;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
            using var cmd = new DuckDBCommand(qry, db);
            using var qryResult = cmd.ExecuteReader();

            var resultList = new List<Dictionary<string, object>>();

            if (qryResult.HasRows)
            {
                while (qryResult.Read())
                {
                    resultList.Add(Enumerable.Range(0, qryResult.FieldCount)
                        .ToDictionary(qryResult.GetName, qryResult.GetValue));
                }
            }

            return (EnumsDuckMemory.Output.SUCCESS, resultList);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
            using var cmd = new DuckDBCommand(qry, db);
            foreach (var parameter in parameters)
            {
                var dbParameter = cmd.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value;
                dbParameter.DbType = NetTypeToDuckDbType.GetDbType(parameter.Value.GetType());
                cmd.Parameters.Add(dbParameter);
            }
            
            using var qryResult = cmd.ExecuteReader();
            var resultList = new List<Dictionary<string, object>>();

            if (qryResult.HasRows)
            {
                while (qryResult.Read())
                {
                    resultList.Add(Enumerable.Range(0, qryResult.FieldCount)
                        .ToDictionary(qryResult.GetName, qryResult.GetValue));
                }
            }

            return (EnumsDuckMemory.Output.SUCCESS, resultList);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return (EnumsDuckMemory.Output.ERROR_TO_EXECUTE_QUERY, []);
        }
    }
}