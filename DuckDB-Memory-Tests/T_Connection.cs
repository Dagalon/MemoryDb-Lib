using DuckDb_Memory_Lib;

namespace DuckDB_Memory_Tests;

public class Connection
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void T_Create_Data_Base()
    {
        const string idDataBase = "TEST_DB";
        const string idAttachedDatabase = "TEST_DB_ATTACHED";

        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection();

        var db = DuckTools.CreateDatabase(conn, idDataBase, null);
        var attachedDb = DuckTools.AttachedDataBase(conn, null, idAttachedDatabase);

        var listDataBases = DuckTools.GetListDataBase(conn);

        Assert.Multiple(() =>
        {
            Assert.That(listDataBases != null && listDataBases.Contains(idDataBase));
            Assert.That(listDataBases != null && listDataBases.Contains(idAttachedDatabase));
        });
        
    }
}
