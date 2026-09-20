using LiteDB;
using LiteDb_Memory_Lib;

namespace LiteDb_Memory_Tests;

public class Concurrency
{
    [Test]
    public void ConcurrentCollectionWritesKeepEveryDocument()
    {
        var manager = ConnectionManager.Instance();
        var alias = Guid.NewGuid().ToString("N");
        try
        {
            manager.CreateDatabase(alias);
            Parallel.For(0, 80, i =>
            {
                var result = manager.CreateCollection<BsonDocument>(alias, "data", [new BsonDocument { ["_id"] = i, ["value"] = i }]);
                Assert.That(result, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
            });
            var documents = FilterTools.FindAll<BsonDocument>(manager, alias, "data");
            Assert.That(documents, Has.Count.EqualTo(80));
            Assert.That(documents!.Sum(d => d["value"].AsInt32), Is.EqualTo(3160));
        }
        finally { manager.Close(alias); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CloseAndReplacementWaitForActiveOperation(bool replace)
    {
        var manager = ConnectionManager.Instance();
        var alias = Guid.NewGuid().ToString("N");
        var otherAlias = Guid.NewGuid().ToString("N");
        using var started = new ManualResetEventSlim();
        Task? mutation = null;
        try
        {
            using (manager.AcquireOperation(alias))
            {
                manager.CreateDatabase(alias);
                mutation = Task.Run(() =>
                {
                    started.Set();
                    if (replace) manager.CreateDatabase(alias, substituteIfExist: true);
                    else manager.Close(alias);
                });
                Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(mutation.Wait(100), Is.False);
                var independent = Task.Run(() => manager.CreateDatabase(otherAlias));
                Assert.That(independent.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(manager.GetDatabase(alias, false), Is.Not.Null);
            }
            Assert.That(mutation.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(manager.GetDatabase(alias, false) is not null, Is.EqualTo(replace));
        }
        finally
        {
            mutation?.Wait(TimeSpan.FromSeconds(5));
            manager.Close(alias);
            manager.Close(otherAlias);
        }
    }
}
