using DuckDb_Memory_Lib;

namespace DuckDB_Memory_Tests;

public class Connection
{
    /// <summary>
    /// Initializes test-level resources before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Verifies in-memory and attached DuckDB database creation.
    /// </summary>
    [Test]
    public void T_Create_Data_Base()
    {
        const string idDataBase = "TEST_DB";
        const string idAttachedDatabase = "TEST_DB_ATTACHED";

        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();

        var db = DuckTools.CreateDatabase(conn, idDataBase, null);
        Assert.That(db, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        
        var attachedDb = DuckTools.AttachedDataBase(conn, null, idAttachedDatabase);
        Assert.That(attachedDb, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        
        var listDataBases = DuckTools.GetListDataBase(conn);

        Assert.Multiple(() =>
        {
            Assert.That(listDataBases != null && listDataBases.Contains(idDataBase));
            Assert.That(listDataBases != null && listDataBases.Contains(idAttachedDatabase));
        });
        
    }
}
