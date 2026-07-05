using LiteDB;
using LiteDb_Memory_Lib;

namespace LiteDb_Memory_Tests;

public class LiteDbToolsCoverage
{
    private sealed class LiteToolDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    [Test]
    public void T_ConnectionManager_Returns_Expected_Status_For_Duplicate_Missing_Path_And_Close()
    {
        var alias = $"LITE_STATUS_{Guid.NewGuid():N}";
        var manager = ConnectionManager.Instance();

        try
        {
            var createOutput = manager.CreateDatabase(alias);
            var duplicateOutput = manager.CreateDatabase(alias);
            var missingPathOutput = manager.CreateDatabase($"{alias}_MISSING", Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-file.db"));
            var missingCloseOutput = manager.Close($"{alias}_UNKNOWN");

            Assert.Multiple(() =>
            {
                Assert.That(createOutput, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
                Assert.That(duplicateOutput, Is.EqualTo(EnumsLiteDbMemory.Output.THERE_EXISTS_DATABASE));
                Assert.That(missingPathOutput, Is.EqualTo(EnumsLiteDbMemory.Output.PATH_NOT_FOUND));
                Assert.That(missingCloseOutput, Is.EqualTo(EnumsLiteDbMemory.Output.DB_NOT_FOUND));
            });
        }
        finally
        {
            manager.Close(alias);
        }
    }

    [Test]
    public void T_LiteDbTools_Update_DeleteMany_Delete_And_Execute_Work_For_Collections()
    {
        var alias = $"LITE_TOOLS_{Guid.NewGuid():N}";
        const string collection = "Documents";
        var manager = ConnectionManager.Instance();

        try
        {
            Assert.That(manager.CreateDatabase(alias), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            Assert.That(manager.CreateCollection(alias, collection, new List<LiteToolDocument>
            {
                new() { Id = "1", Name = "First", Amount = 10 },
                new() { Id = "2", Name = "Second", Amount = 20 },
                new() { Id = "3", Name = "Third", Amount = 30 }
            }), Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));

            var updateOutput = LiteDbTools.Update(manager, alias, collection, new LiteToolDocument
            {
                Id = "1",
                Name = "Updated",
                Amount = 15
            });
            var deleteManyOutput = LiteDbTools.DeleteMany<LiteToolDocument>(manager, alias, collection, document => document.Amount >= 30);
            var deleteOutput = LiteDbTools.Delete<LiteToolDocument>(manager, alias, collection, "2");
            var missingDatabaseOutput = LiteDbTools.Delete<LiteToolDocument>(manager, $"{alias}_MISSING", collection, "1");
            var result = LiteDbTools.Execute<BsonDocument>(manager, alias, "SELECT Id, Name, Amount FROM Documents");

            Assert.Multiple(() =>
            {
                Assert.That(updateOutput, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
                Assert.That(deleteManyOutput, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
                Assert.That(deleteOutput, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
                Assert.That(missingDatabaseOutput, Is.EqualTo(EnumsLiteDbMemory.Output.COLLECTION_NOT_FOUND));
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result?[0]["Name"].AsString, Is.EqualTo("Updated"));
                Assert.That(result?[0]["Amount"].AsInt32, Is.EqualTo(15));
            });
        }
        finally
        {
            manager.Close(alias);
        }
    }
}
