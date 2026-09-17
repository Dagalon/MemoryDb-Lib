using LiteDB;
using LiteDb_Memory_Lib;

namespace LiteDb_Memory_Tests;

public class SharedFiles
{
    [Test]
    public void JsonAndBothUploadOverloadsReadFileHeldByWriter()
    {
        var path = Path.GetTempFileName();
        var alias = Guid.NewGuid().ToString("N");
        var manager = ConnectionManager.Instance();
        try
        {
            File.WriteAllText(path, "[{\"Name\":\"Ana\"}]");
            using var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            manager.CreateDatabase(alias);
            Assert.That(manager.CreateCollection<BsonDocument>(alias, "people", path), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            Assert.That(manager.GetCollection<BsonDocument>(alias, "people")!.Count(), Is.EqualTo(1));
            Assert.That(FileStorageTools.Upload(manager, alias, "files", "first", path), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            Assert.That(FileStorageTools.Find(manager, alias, "files", "first")!.Length, Is.EqualTo(writer.Length));
            Assert.That(FileStorageTools.Upload(manager, alias, "second", path, (MemoryStream?)null), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            Assert.That(FileStorageTools.Find(manager, alias, "second", path)!.Length, Is.EqualTo(writer.Length));
        }
        finally { manager.Close(alias); File.Delete(path); }
    }

    [Test]
    public void SharedDatabaseCanBeReadWhileAnotherConnectionIsOpen()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        var alias = Guid.NewGuid().ToString("N");
        var manager = ConnectionManager.Instance();
        try
        {
            using var source = new LiteDatabase(new ConnectionString { Filename = path, Connection = ConnectionType.Shared });
            source.GetCollection("items").Insert(new BsonDocument { ["value"] = 42 });
            Assert.That(manager.CreateDatabase(alias, path, isShared: true), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            Assert.That(manager.GetCollection<BsonDocument>(alias, "items")!.FindOne(Query.All())["value"].AsInt32, Is.EqualTo(42));
        }
        finally { manager.Close(alias); File.Delete(path); }
    }
}
