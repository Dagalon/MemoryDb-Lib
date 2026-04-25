using LiteDb_Memory_Lib;
using LiteDB;

namespace LiteDb_Memory_Tests;

public class Connection
{
    /// <summary>
    /// Initializes test-level resources before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Verifies that shared and regular in-memory databases can be created by alias.
    /// </summary>
    [Test]
    public void T_Create_Data_Base()
    {
        var aliasShared = "Test_Db_Shared";
        var alias = "Test_Db";
        
        var manager = ConnectionManager.Instance();
        
        // Create a shared database 
        manager.CreateDatabase(aliasShared, isShared: true);
        
        // Create a database
        manager.CreateDatabase(alias);
        
        manager.Close(aliasShared);
        manager.Close(alias);
    }
    
    /// <summary>
    /// Verifies that a database can be persisted to disk and reopened afterwards.
    /// </summary>
    [Test]
    public void T_Create_And_Remove_Data_Base()
    {
        var aliasDb = "Test_Db";
        var manager = ConnectionManager.Instance();
        
        // Create a shared database 
        manager.CreateDatabase(aliasDb, isShared: true);

        // Create BsonDocument
        var customer = new BsonDocument
        {
            ["_id"] = ObjectId.NewObjectId(),
            ["Name"] = "David Garcia Lorite",
            ["role"] = "Developer"
        };
        
        // Create a collection
        manager.CreateCollection(aliasDb, "personal_data",[customer]);
       
        // Write to disk
        var folderPath = "C:\\GitRepositories\\Net\\C#\\LiteDb-Memory-Lib-develop\\LiteDb-Memory-Tests\\Data";
        var pathToKeep = Path.Combine(folderPath, "Test_Db_Shared.bin");
        manager.Close(aliasDb, pathToKeep);
        
        // Load again the database
        manager.CreateDatabase(aliasDb, pathToKeep);
        var element = FilterTools.FindById<BsonDocument>(manager, aliasDb, "personal_data", customer["_id"]);
        
        Assert.That(element, Is.Not.Null);
    }
}
