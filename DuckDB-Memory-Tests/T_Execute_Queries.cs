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
        const string idTable = "TABLE_PERSONAL_DATA";

        const string pathFile = "";
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();
        
        var db = DuckTools.CreateDatabase(conn, idDataBase, null);
        
        var result = QueryExecutor.CreateParquetTable(conn, idDataBase, idTable, pathFile);
        
        
    }
}