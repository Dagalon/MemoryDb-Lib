using System.Reflection;
using DuckDb_Memory_Lib;

namespace DuckDB_Memory_Tests;

public class ExecuteQueries
{
    /// <summary>
    /// Initializes test-level resources before each test execution.
    /// </summary>
    [SetUp]
    public void Setup() { }

    /// <summary>
    /// Verifies table creation from a parquet file and subsequent read operations.
    /// </summary>
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
    [Test]
    public void T_ExecuteQryReader_Uses_DuckDb_Parameters()
    {
        var conn = ConnectionManager.GetInstance().GetConnection($"PARAM_DB_{Guid.NewGuid():N}");
        var result = QueryExecutor.ExecuteQryReader(
            conn,
            "SELECT @value AS VALUE",
            new Dictionary<string, string> { ["@value"] = "O'Reilly" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
            Assert.That(result.Item2.Single()["VALUE"], Is.EqualTo("O'Reilly"));
        });
    }

}
