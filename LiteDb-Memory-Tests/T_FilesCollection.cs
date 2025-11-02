using System.Runtime.InteropServices;
using LiteDB;
using LiteDb_Memory_Lib;

namespace LiteDb_Memory_Tests
{
    internal class FilesCollection
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void T_Upload_File()
        {
            // Instance manager connections
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var rootPath = Path.Combine(basePath,"Data");
            const string aliasDb = "Test-db-Images";
            const string collection = "Images";
            const string fileName = "file-test";
            
            var pathToKeepDataBase = Path.Combine(rootPath, $"{aliasDb}.bin");

            var manager = ConnectionManager.Instance();
            var checkDataBaseIsCreated = manager.CreateDatabase(aliasDb, isShared: true);
            Assert.That(checkDataBaseIsCreated, Is.EqualTo(EnumsLiteDbMemory.Output.SUCCESS)); 
            
            manager.CreateCollection<BsonDocument>(aliasDb, collection);
            
            var listCollection = manager.GetCollectionNames(aliasDb);
            Assert.That(listCollection.Contains(collection));
            
            // Upload a file
            var checkUploadFile = FileStorageTools.Upload(manager, aliasDb, collection, fileName, Path.Combine(rootPath, $"{fileName}.png"));

            // Find the document with a file
            var image = FileStorageTools.Find(manager, aliasDb, collection, fileName);
            Assert.That(image is not null);

            // Find save database
            var isClose = manager.Close(aliasDb, pathToKeepDataBase);

            // Read again the database
            var result = manager.CreateDatabase(aliasDb, pathToKeepDataBase, isShared: true);
            listCollection = manager.GetCollectionNames(aliasDb);

            Assert.That(listCollection.Contains(collection));
            Assert.That(listCollection.Contains($"{collection}chunk"));

            // Check if a file exists from a database-loaded form file in the disk
            image = FileStorageTools.Find(manager, aliasDb, collection, fileName);

            Assert.That(image is not null);
        }

    }
}
