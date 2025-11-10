using LiteDB;
using LiteDb_Memory_Lib;

namespace LiteDb_Memory_Tests;


public struct PersonalData
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public string FirstSurname { get; set; }
    public string SecondSurname { get; set; }
    public int NumberOfOrders { get; set; }
    public string Identifier { get; set; }

}

public class GeneralTools
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void T_Create_Index()
    {
        const string aliasDb = "TEST_DB";
        const string idCollection = "PERSONAL_DATA";
        var manager = ConnectionManager.Instance();
        
        var checkCreatedDataBase = manager.CreateDatabase(aliasDb, isShared: true, substituteIfExist: true);
        Assert.That(checkCreatedDataBase, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));
        
        var checkCreateCollection = manager.CreateCollection<PersonalData>(aliasDb, idCollection);
        Assert.That(checkCreateCollection, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));

        var checkCreateIndex = LiteDb_Memory_Lib.GeneralTools.CreateIndex<PersonalData, string>(manager, aliasDb, 
            idCollection, x  => x.Identifier);
        Assert.That(checkCreateIndex, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS));

        var indexQry = @"SELECT * FROM $indexes WHERE collection ='PERSONAL_DATA'";
        var result = LiteDb_Memory_Lib.GeneralTools.Execute<BsonDocument>(manager, aliasDb, indexQry);
        Assert.That(result?[0]["expr"][1]["name"].AsString, Is.EqualTo("Identifier"));
    }
}