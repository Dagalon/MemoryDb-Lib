using DuckDb_Memory_Lib;
using System.Text.Json;

namespace DuckDB_Memory_Tests;

[NonParallelizable]
public class ConfigurationAndConcurrency
{
    [TestCase(null)]
    [TestCase("{}")]
    [TestCase("{\"temp_path\":\" \",\"extension_path\":null}")]
    public void MissingPathsUseAddInDirectory(string? json)
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            if (json is not null) File.WriteAllText(Path.Combine(directory, DuckDbConfiguration.FileName), json);
            var configuration = DuckDbConfiguration.Load(directory);
            Assert.That(configuration.TempPath, Is.EqualTo(directory));
            Assert.That(configuration.ExtensionPath, Is.EqualTo(directory));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void ConfiguredPathsReachNativeEngineAndTempsAreIsolated()
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-'" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var extensionPath = Path.Combine(directory, "extensions");
            File.WriteAllText(Path.Combine(directory, DuckDbConfiguration.FileName), JsonSerializer.Serialize(new
            {
                temp_path = "temporary",
                extension_path = extensionPath
            }));
            DuckDbConfiguration.Initialize(directory);
            using var first = DuckTools.GetInstance(null);
            using var second = DuckTools.GetInstance(null);
            first.Open();
            second.Open();
            using var command = first.CreateCommand();
            command.CommandText = "SELECT current_setting('extension_directory')";
            Assert.That(command.ExecuteScalar(), Is.EqualTo(extensionPath));
            command.CommandText = "SELECT current_setting('temp_directory')";
            var firstTemp = Convert.ToString(command.ExecuteScalar())!;
            Assert.That(Path.GetFullPath(firstTemp), Does.StartWith(Path.Combine(directory, "temporary", "duckdb-temp")));
            using var other = second.CreateCommand();
            other.CommandText = command.CommandText;
            Assert.That(other.ExecuteScalar(), Is.Not.EqualTo(firstTemp));
        }
        finally
        {
            DuckDbConfiguration.Initialize(AppContext.BaseDirectory);
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ParallelQueriesOnOneConnectionReturnCorrectResults()
    {
        using var connection = DuckTools.GetInstance(null);
        connection.Open();
        Parallel.For(0, 80, i =>
        {
            var (status, rows) = QueryExecutor.ExecuteQryReader(connection, $"SELECT {i} AS value");
            Assert.That(status, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
            Assert.That(Convert.ToInt32(rows[0]["value"]), Is.EqualTo(i));
        });
    }

    [Test]
    public void CloseWaitsForOperationWithoutBlockingAnotherAlias()
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
                var connection = manager.GetConnection(alias);
                close = Task.Run(() => { started.Set(); manager.CloseConnection(alias); });
                Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(close.Wait(100), Is.False);
                var independent = Task.Run(() => manager.GetConnection(otherAlias));
                Assert.That(independent.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(connection.State, Is.EqualTo(System.Data.ConnectionState.Open));
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
