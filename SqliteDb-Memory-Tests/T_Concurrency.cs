using SqliteDB_Memory_Lib;
using XLS_Memory_Lib;

namespace SqliteDB_Memory_Tests;

public class Concurrency
{
    [TestCase(typeof(MemoryDbDuckDbExcelFunctions))]
    [TestCase(typeof(MemoryDbSqliteExcelFunctions))]
    [TestCase(typeof(MemoryDbLiteDbExcelFunctions))]
    public void DatabaseWorksheetFunctionsEnableMultithreadedCalculation(Type type)
    {
        foreach (var method in type.GetMethods())
        {
            var attribute = Attribute.GetCustomAttribute(method, typeof(ExcelDna.Integration.ExcelFunctionAttribute))
                as ExcelDna.Integration.ExcelFunctionAttribute;
            if (attribute is not null)
                Assert.That(attribute.IsThreadSafe, Is.True, method.Name);
        }
    }

    [Test]
    public void ParallelInsertsOnOneConnectionKeepEveryRow()
    {
        using var connection = SqLiteLiteTools.GetInstance(null);
        connection.Open();
        QueryExecutor.ExecuteQryNotReader(connection, "CREATE TABLE data(value INTEGER)");
        Parallel.For(0, 100, i => QueryExecutor.Insert(connection, "main", "data", ["value"], new object[,] { { i } }, null));
        var rows = QueryExecutor.ExecuteQryReader(connection, "SELECT count(*) AS count, sum(value) AS total FROM data");
        Assert.That(Convert.ToInt32(rows[0]["count"]), Is.EqualTo(100));
        Assert.That(Convert.ToInt32(rows[0]["total"]), Is.EqualTo(4950));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExcelQueriesCanRunConcurrently(bool duckDb)
    {
        var alias = "parallel_" + Guid.NewGuid().ToString("N");
        try
        {
            Parallel.For(0, 80, i =>
            {
                var result = duckDb
                    ? MemoryDbDuckDbExcelFunctions.Scalar(alias, $"SELECT {i}", new object())
                    : MemoryDbSqliteExcelFunctions.Scalar(alias, $"SELECT {i}", new object());
                Assert.That(Convert.ToInt32(result), Is.EqualTo(i));
            });
        }
        finally
        {
            if (duckDb) DuckDb_Memory_Lib.ConnectionManager.Close(alias);
            else ConnectionManager.Close(alias);
        }
    }

    [Test]
    public void ClosingOneAliasDoesNotBlockIndependentDatabase()
    {
        var manager = ConnectionManager.GetInstance();
        var alias = Guid.NewGuid().ToString("N");
        var otherAlias = Guid.NewGuid().ToString("N");
        using var started = new ManualResetEventSlim();
        Task? close = null;
        try
        {
            using (manager.AcquireOperation(alias))
            {
                manager.GetConnection(alias);
                close = Task.Run(() => { started.Set(); manager.CloseConnection(alias); });
                Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(close.Wait(100), Is.False);
                var other = Task.Run(() => manager.GetConnection(otherAlias));
                Assert.That(other.Wait(TimeSpan.FromSeconds(5)), Is.True);
            }
            Assert.That(close.Wait(TimeSpan.FromSeconds(5)), Is.True);
        }
        finally
        {
            close?.Wait(TimeSpan.FromSeconds(5));
            manager.CloseConnection(alias);
            manager.CloseConnection(otherAlias);
        }
    }
}
