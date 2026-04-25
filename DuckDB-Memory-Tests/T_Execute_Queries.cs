using System.Reflection;
using DuckDb_Memory_Lib;

namespace DuckDB_Memory_Tests;

public class ExecuteQueries
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void T_Create_Table_From_Parquet()
    {
        const string idDataBase = "TEST_DB";
        const string idTable = "CUSTOMERS_DATA";

        var rootPath =  Path.Combine(AppContext.BaseDirectory, "Data");
        var pathFile = Path.Combine(rootPath, "sample_customers_orders.parquet");
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();
        
        var dbOutput = DuckTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(dbOutput, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        
        var result = QueryExecutor.CreateParquetTable(conn, idDataBase, idTable, pathFile);
        Assert.That(result, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        
        var listTables = DuckTools.GetListTables(conn, idDataBase);
        Assert.That(listTables.Item1, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        Assert.That(listTables.Item2 != null && listTables.Item2.Contains("CUSTOMERS_DATA"));
        
        var qryExample = $@"SELECT * FROM {idDataBase}.{idTable}";
        var resultQry = QueryExecutor.ExecuteQryReader(conn, qryExample);
        Assert.That(resultQry.Item1, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));

    }
}