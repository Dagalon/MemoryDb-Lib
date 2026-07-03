using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace SqliteDB_Memory_Lib
{
    public static class QueryExecutor
    {

        /// <summary>
        /// Inserts multiple rows into the specified table by using parameterized statements.
        /// </summary>
        public static void Insert(
            SqliteConnection db,
            string idDataBase,
            string idTable,
            List<string> fields,
            object[,] values,
            string? extraEnd)
        {
            var quotedTable =
                $"{SqLiteLiteTools.QuoteIdentifier(idDataBase)}.{SqLiteLiteTools.QuoteIdentifier(idTable)}";

            var quotedFields = fields
                .Select(SqLiteLiteTools.QuoteIdentifier)
                .ToList();

            var insertParameters = Enumerable
                .Range(0, fields.Count)
                .Select(SqLiteLiteTools.ParameterName)
                .ToList();

            var qry = !string.IsNullOrWhiteSpace(extraEnd)
                ? $"INSERT INTO {quotedTable} ({string.Join(", ", quotedFields)}) VALUES ({string.Join(", ", insertParameters)}) {extraEnd}"
                : $"INSERT INTO {quotedTable} ({string.Join(", ", quotedFields)}) VALUES ({string.Join(", ", insertParameters)})";

            lock (db)
            {
                using var transaction = db.BeginTransaction();
                using var cmd = db.CreateCommand();

                cmd.Transaction = transaction;
                cmd.CommandText = qry;

                var noRows = values.GetLength(0);
                var noColumns = values.GetLength(1);

                try
                {
                    for (var i = 0; i < noRows; i++)
                    {
                        cmd.Parameters.Clear();

                        var jumpRow = true;

                        for (var j = 0; j < noColumns; j++)
                        {
                            var value = values[i, j];

                            if (!string.IsNullOrEmpty(value.ToString()))
                                jumpRow = false;

                            cmd.Parameters.AddWithValue(SqLiteLiteTools.ParameterName(j), value ?? DBNull.Value);
                        }

                        if (!jumpRow)
                            cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }


        /// <summary>
        /// Creates a table with the provided column definitions.
        /// </summary>
        public static void CreateTable(
            SqliteConnection db,
            string idDataBase,
            string idTable,
            object[,]? values,
            List<string> headers,
            List<Type>? types = null)
        {

            if (types is null && values is not null)
            {
                types = NetTypeToSqLiteType.InferTypes(values, headers.Count);
            }
            
            var fieldsDefinition = new List<string>();
            var noFields = headers.Count;

            for (var i = 0; i < noFields; i++)
            {
                var columnName = SqLiteLiteTools.QuoteIdentifier(headers[i]);
                if (types is null)
                {
                    fieldsDefinition.Add(columnName);
                }
                else
                {
                    var sqliteType = NetTypeToSqLiteType.GetDbType(types[i]);
                    fieldsDefinition.Add($"{columnName} {sqliteType}");
                }
            }

            var tableName =
                $"{SqLiteLiteTools.QuoteIdentifier(idDataBase)}.{SqLiteLiteTools.QuoteIdentifier(idTable)}";

            var qry =
                $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", fieldsDefinition)})";

            using var cmd = new SqliteCommand(qry, db);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a SELECT statement built from the supplied clauses.
        /// </summary>
        public static List<Dictionary<string, object>>? Select(SqliteConnection db, string idDataBase, string idTable,
                                                              string select, string where, string groupBy, string orderBy)
        {
            var qry = $"SELECT {select} FROM {idDataBase}.{idTable} WHERE {where} GROUP BY {groupBy} ORDER BY {orderBy}";

            if (string.IsNullOrEmpty(where))
            {
                qry = qry.Replace("WHERE", "");
            }

            if (string.IsNullOrEmpty(groupBy))
            {
                qry = qry.Replace("GROUP BY", "");
            }

            if (string.IsNullOrEmpty(orderBy))
            {
                qry = qry.Replace("ORDER BY", "");
            }

            var cmd = new SqliteCommand(qry, db);
            var qryResult = cmd.ExecuteReader();
            var resultList = new List<Dictionary<string, object>>();

            if (qryResult.HasRows)
            {
                while (qryResult.Read())
                {
                    resultList.Add(Enumerable.Range(0, qryResult.FieldCount)
                        .ToDictionary(qryResult.GetName, qryResult.GetValue));
                }

                qryResult.Close();
                return resultList;
            }

            qryResult.Close();
            return null;

        }

        /// <summary>
        /// Executes a SQL statement that does not return rows.
        /// </summary>
        public static void ExecuteQryNotReader(SqliteConnection db, string qry)
        {
            var cmd = new SqliteCommand(qry, db);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a parameterized SQL statement that does not return rows.
        /// </summary>
        public static void ExecuteQryNotReader(SqliteConnection db, string qry, Dictionary<string, string> parameters)
        {
            qry = parameters.Keys.Aggregate(qry, (current, param) => current.Replace(param, parameters[param], StringComparison.OrdinalIgnoreCase));

            var cmd = new SqliteCommand(qry, db);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a SQL statement and returns the resulting rows.
        /// </summary>
        public static List<Dictionary<string, object>> ExecuteQryReader(SqliteConnection db, string qry)
        {
            var cmd = new SqliteCommand(qry, db);
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
            return resultList;
        }

        /// <summary>
        /// Executes a parameterized SQL statement and returns the resulting rows.
        /// </summary>
        public static List<Dictionary<string, object>> ExecuteQryReader(SqliteConnection db, string qry, Dictionary<string, string> parameters)
        {
            qry = parameters.Keys.Aggregate(qry, (current, param) => current.Replace(param, parameters[param], StringComparison.OrdinalIgnoreCase));

            return ExecuteQryReader(db, qry);
        }
    }
}

