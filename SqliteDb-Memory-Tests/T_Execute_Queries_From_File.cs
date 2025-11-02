using SqliteDB_Memory_Lib;

namespace SqliteDb_Memory_Tests;

public class ExecuteQueriesFromFile
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void T_Execute_Query()
    {
        const string idDataBase = "TEST_DB";
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();

        var idTable = "TABLE_PERSONAL_DATA";
        
        //  create a database
        var checkDataBase = SqLiteLiteTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(checkDataBase == EnumsSqliteMemory.Output.SUCCESS);
        
        // data of the table
        var headers = new List<string> { "ID", "NAME", "FIRST_NAME", "AGE", "JOB" };
        var data = new object[,] { { 1, "Juan", "Garcia", 25, "Programmer" }, 
            { 2, "Pedro", "Moreno", 45, "Engineering" },
            {3, "Maria", "Lopez", 32, "Electricity"},
            {4, "Luis", "Moreno", 45, "Engineering"}
        };
        
        var checkTable = SqLiteLiteTools.CreateTable(conn, idDataBase, idTable, headers, data);
        Assert.That(checkTable == EnumsSqliteMemory.Output.SUCCESS);
        
        // root path of the file and excute the query
        var rootPath =  Path.Combine(AppContext.BaseDirectory, "Data");
        var pathQry = Path.Combine(rootPath, "Qry_Parameterized.txt");
        var parameters = new Dictionary<string, string> { { "@id", "1" }, { "@name", @"'Juan'" } };
        var result = SqLiteLiteTools.ExecuteQryReader(conn, pathQry, parameters);
        
        Assert.That(result is { Item1: EnumsSqliteMemory.Output.SUCCESS, Item2.Count: 1 });
    }

    [Test]
    public void T_Execute_Query_Massive_Insert()
    {
        const string idDataBase = "TEST_DB";
        
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();
        
        //  create a database
        var checkDataBase = SqLiteLiteTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(checkDataBase == EnumsSqliteMemory.Output.SUCCESS);
         
        // path data to create the table
        var idTable = "TABLE_PERSONAL_DATA";
        var rootPath =  Path.Combine(AppContext.BaseDirectory, "Data");
        var pathCsv = Path.Combine(rootPath, "fake_personal_data.csv");
        var result = SqLiteLiteTools.CreateTable(conn, idDataBase, idTable, pathCsv);
        
        Assert.That(result == EnumsSqliteMemory.Output.SUCCESS);
        var listTables = SqLiteLiteTools.GetListTables(conn, idDataBase);
        
        Assert.Multiple(() =>
        {
            Assert.That(listTables.Item1 == EnumsSqliteMemory.Output.SUCCESS);
            Assert.That(listTables.Item2 != null && listTables.Item2.Contains(idTable));
        });

    }

}