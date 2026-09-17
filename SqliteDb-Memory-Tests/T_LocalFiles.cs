using NUnit.Framework;
using Microsoft.Data.Sqlite;
using SqliteDB_Memory_Lib;
namespace FileRegressionTests;

public class LocalFiles
{
    private string _path = null!;
    [SetUp] public void Setup() => _path = Path.Combine(Path.GetTempPath(), $"memorydb-' {Guid.NewGuid():N}.db");
    [TearDown] public void Cleanup() { SqliteConnection.ClearAllPools(); File.Delete(_path); }

    [Test]
    public void GetInstanceCreatesRequestedFile()
    {
        using (var db = SqLiteLiteTools.GetInstance(_path))
        {
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "CREATE TABLE items(id INTEGER)";
            cmd.ExecuteNonQuery();
        }
        Assert.That(File.Exists(_path), Is.True);
    }
    [Test]
    public void GetInstanceReadsExistingDatabase()
    {
        using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString()))
        {
            source.Open();
            using var cmd = source.CreateCommand();
            cmd.CommandText = "CREATE TABLE items(id INTEGER); INSERT INTO items VALUES (42)";
            cmd.ExecuteNonQuery();
        }
        using var db = SqLiteLiteTools.GetInstance(_path);
        db.Open();
        using var query = db.CreateCommand();
        query.CommandText = "SELECT id FROM items";
        Assert.That(Convert.ToInt32(query.ExecuteScalar()), Is.EqualTo(42));
    }

    [Test]
    public void AttachCreatesValidDatabaseAndReopensIt()
    {
        using (var db = SqLiteLiteTools.GetInstance(null))
        {
            db.Open();
            Assert.That(SqLiteLiteTools.CreateDatabase(db, "local", _path).IsSuccess, Is.True);
            using var cmd = db.CreateCommand();
            cmd.CommandText = "CREATE TABLE local.items(id INTEGER); INSERT INTO local.items VALUES (7)";
            cmd.ExecuteNonQuery();
        }
        using var reopened = SqLiteLiteTools.GetInstance(null);
        reopened.Open();
        Assert.That(SqLiteLiteTools.CreateDatabase(reopened, "loaded", _path).IsSuccess, Is.True);
        using var query = reopened.CreateCommand();
        query.CommandText = "SELECT id FROM loaded.items";
        Assert.That(Convert.ToInt32(query.ExecuteScalar()), Is.EqualTo(7));
    }

    [Test]
    public void ResolveSqlReadsFileHeldByWriter()
    {
        File.WriteAllText(_path, "SELECT 42");
        using var writer = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        Assert.That(SqLiteLiteTools.ResolveSql(_path), Is.EqualTo("SELECT 42"));
    }

    [Test]
    public void ExclusiveLockIsReported()
    {
        File.WriteAllText(_path, "SELECT 42");
        using var writer = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Throws<IOException>(() => SqLiteLiteTools.ResolveSql(_path));
    }

    [Test]
    public void FailedAttachCanBeRetried()
    {
        File.WriteAllText(_path, "not a database");
        using var db = SqLiteLiteTools.GetInstance(null);
        db.Open();
        Assert.That(SqLiteLiteTools.CreateDatabase(db, "retry", _path).IsSuccess, Is.False);
        File.Delete(_path);
        Assert.That(SqLiteLiteTools.CreateDatabase(db, "retry", _path).IsSuccess, Is.True);
    }
}