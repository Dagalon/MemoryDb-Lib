using XLS_Memory_Lib;

namespace SqliteDb_Memory_Tests;

public class ExcelLocalFiles
{
    [TestCase(false)]
    [TestCase(true)]
    public void CreateDbCanOpenAndReopenLocalFile(bool duck)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        var alias = "local" + Guid.NewGuid().ToString("N");
        try
        {
            for (var pass = 0; pass < 2; pass++)
            {
                var status = duck ? MemoryDbDuckDbExcelFunctions.Open(alias, path) : MemoryDbSqliteExcelFunctions.Open(alias, path);
                Assert.That(status, Does.Not.Contain("ERROR"));
                var manager = duck
                    ? (System.Data.Common.DbConnection)DuckDb_Memory_Lib.ConnectionManager.GetInstance().GetConnection(alias)
                    : SqliteDB_Memory_Lib.ConnectionManager.GetInstance().GetConnection(alias);
                using (var cmd = manager.CreateCommand())
                {
                    cmd.CommandText = pass == 0
                        ? $"CREATE TABLE {alias}.items(id INTEGER); INSERT INTO {alias}.items VALUES (42)"
                        : $"SELECT id FROM {alias}.items";
                    if (pass == 0) cmd.ExecuteNonQuery();
                    else Assert.That(Convert.ToInt32(cmd.ExecuteScalar()), Is.EqualTo(42));
                }
                if (duck) MemoryDbDuckDbExcelFunctions.Close(alias);
                else MemoryDbSqliteExcelFunctions.Close(alias);
            }
        }
        finally
        {
            if (duck) MemoryDbDuckDbExcelFunctions.Close(alias);
            else MemoryDbSqliteExcelFunctions.Close(alias);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}
