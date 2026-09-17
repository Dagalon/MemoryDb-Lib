using Microsoft.Data.Sqlite;
using SqliteDB_Memory_Lib;

namespace SqliteDb_Memory_Tests;

public class SharedImports
{
    [Test]
    public void ExistingDatabaseCanBeReadDuringWalWriteTransaction()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        try
        {
            using var writer = SqLiteLiteTools.GetInstance(path);
            writer.Open();
            using var cmd = writer.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE items(id INTEGER); INSERT INTO items VALUES (42)";
            cmd.ExecuteNonQuery();
            using var transaction = writer.BeginTransaction();
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO items VALUES (43)";
            cmd.ExecuteNonQuery();
            using var reader = SqLiteLiteTools.GetInstance(path);
            reader.Open();
            using var query = reader.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM items";
            Assert.That(Convert.ToInt32(query.ExecuteScalar()), Is.EqualTo(1));
            transaction.Rollback();
        }
        finally { SqliteConnection.ClearAllPools(); File.Delete(path); }
    }

    [TestCase("id;name\n1;Ana\n", 1)]
    [TestCase("id;name\n", 0)]
    public void CsvCanBeImportedWhileOpenForWriting(string content, int count)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, content);
            using var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            using var db = SqLiteLiteTools.GetInstance(null);
            db.Open();
            Assert.That(SqLiteLiteTools.CreateTable(db, "main", "items", path).IsSuccess, Is.True);
            Assert.That(SqLiteLiteTools.Insert(db, "main", "items", path, "").IsSuccess, Is.True);
            using var query = db.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM items";
            Assert.That(Convert.ToInt32(query.ExecuteScalar()), Is.EqualTo(count * 2));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void SqlFileHelpersReadWhileOpenForWriting()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "SELECT @id");
            using var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            using var db = SqLiteLiteTools.GetInstance(null);
            db.Open();
            var parameters = new Dictionary<string, string> { ["@id"] = "42" };
            Assert.That(SqLiteLiteTools.ExecuteQryNotReader(db, path, parameters).IsSuccess, Is.True);
            var rows = SqLiteLiteTools.ExecuteQryReader(db, path, parameters);
            Assert.That(rows.Output.IsSuccess, Is.True);
            Assert.That(rows.Rows, Has.Count.EqualTo(1));
            Assert.That(SqLiteLiteTools.SubstituteParameters(path, parameters).Sql, Is.EqualTo("SELECT 42"));
            Assert.That(SqLiteLiteTools.GetListParameters(path, "").Parameters, Does.Contain("@id"));
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void InvalidCsvReleasesFileAndDatabaseErrorsAreReturned()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var db = SqLiteLiteTools.GetInstance(null);
            db.Open();
            Assert.That(SqLiteLiteTools.CreateTable(db, "main", "items", path).IsSuccess, Is.False);
            using (var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            File.WriteAllText(path, "id\n1\n");
            Assert.That(SqLiteLiteTools.CreateTable(db, "missing", "items", path).IsSuccess, Is.False);
        }
        finally { File.Delete(path); }
    }
}
