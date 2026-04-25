using LiteDb_Memory_Lib;
using LiteDB;

namespace LiteDb_Memory_Tests;

public class ExecuteQueries
{

    public struct Ticket
    {
        public ObjectId OrderId { get; set; }
        public string ObjectType { get; set; }
        public int Amount { get; set; }
    }


    /// <summary>
    /// Initializes test-level resources before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Verifies that LiteDB SQL queries can be executed against a seeded collection.
    /// </summary>
    [Test]
    public void T_Get_Collections()
    {
        var idDataBase = "TEST_DB";
        var manager = ConnectionManager.Instance();

        // Create database
        manager.CreateDatabase(idDataBase);

        // Create collection
        var food = new List<Ticket>()
        {
            new() {OrderId = ObjectId.NewObjectId(), ObjectType = "Banana", Amount = 6},
            new() {OrderId = ObjectId.NewObjectId(), ObjectType = "Orange", Amount = 10},
            new() {OrderId = ObjectId.NewObjectId(), ObjectType = "Apple", Amount = 20}
        };

        manager.CreateCollection(idDataBase, "DeliveryList", food);

        // Query 
        var qry = @"SELECT ObjectType, Amount FROM DeliveryList WHERE ObjectType = 'Banana'";
        var result = LiteDb_Memory_Lib.LiteDbTools.Execute<BsonDocument>(manager, idDataBase, qry);

        var output = new BsonDocument()
        {
            {"ObjectType", "Banana"},
            {"Amount", 6}
        };
        
        Assert.That(result?[0], Is.EqualTo(output));
    }


}
