using ExcelDna.Integration;
using LiteDB;

namespace XLS_Memory_Lib;

public static class MemoryDbLiteDbExcelFunctions
{
    private const string Category = "Memory DB - LiteDB";

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.CREATE", Description = "Creates or replaces a named in-memory LiteDB database, or opens a file-backed database when path is provided.", Category = Category)]
    public static string Create(string alias, string path = "", bool replaceExisting = true, bool shared = false)
    {
        if (string.IsNullOrWhiteSpace(alias)) return Error("alias is required");
        var result = Manager.CreateDatabase(alias, NullIfBlank(path), replaceExisting, shared);
        return result.ToString();
    }

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.CLOSE", Description = "Closes a named LiteDB database and optionally persists it to disk.", Category = Category)]
    public static string Close(string alias, string pathToKeep = "")
    {
        if (string.IsNullOrWhiteSpace(alias)) return Error("alias is required");
        return Manager.Close(alias, NullIfBlank(pathToKeep)).ToString();
    }

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.COLLECTIONS", Description = "Lists the collections registered in a LiteDB database.", Category = Category)]
    public static object[,] Collections(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return Tables.ErrorTable("alias is required");
        var names = Manager.GetCollectionNames(alias);
        return Tables.Vector("Collection", names.Cast<object>());
    }

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.INSERT.JSON", Description = "Inserts one JSON document into a LiteDB collection.", Category = Category)]
    public static string InsertJson(string alias, string collection, string jsonDocument)
    {
        if (string.IsNullOrWhiteSpace(alias)) return Error("alias is required");
        if (string.IsNullOrWhiteSpace(collection)) return Error("collection is required");
        if (string.IsNullOrWhiteSpace(jsonDocument)) return Error("jsonDocument is required");

        try
        {
            var database = Manager.GetDatabase(alias, createIfMissing: false);
            if (database is null) return LiteDb_Memory_Lib.EnumsLiteDbMemory.Output.DB_NOT_FOUND.ToString();

            var document = LiteDB.JsonSerializer.Deserialize(jsonDocument).AsDocument;
            database.GetCollection<BsonDocument>(collection).Insert(document);
            database.Checkpoint();
            return LiteDb_Memory_Lib.EnumsLiteDbMemory.Output.SUCCESS.ToString();
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.FINDALL.JSON", Description = "Returns all LiteDB collection documents as JSON text.", Category = Category)]
    public static object[,] FindAllJson(string alias, string collection)
    {
        if (string.IsNullOrWhiteSpace(alias)) return Tables.ErrorTable("alias is required");
        if (string.IsNullOrWhiteSpace(collection)) return Tables.ErrorTable("collection is required");

        try
        {
            var database = Manager.GetDatabase(alias, createIfMissing: false);
            if (database is null) return Tables.ErrorTable("database not found");

            var rows = database.GetCollection<BsonDocument>(collection)
                .FindAll()
                .Select(document => document.ToString())
                .Cast<object>();
            return Tables.Vector("Json", rows);
        }
        catch (Exception ex)
        {
            return Tables.ErrorTable(ex.Message);
        }
    }

    [ExcelFunction(Name = "MEMORY_DB.LITEDB.DELETE", Description = "Deletes one LiteDB document by id from a collection.", Category = Category)]
    public static string Delete(string alias, string collection, string id)
    {
        if (string.IsNullOrWhiteSpace(alias)) return Error("alias is required");
        if (string.IsNullOrWhiteSpace(collection)) return Error("collection is required");
        if (string.IsNullOrWhiteSpace(id)) return Error("id is required");
        return LiteDb_Memory_Lib.LiteDbTools.Delete<BsonDocument>(Manager, alias, collection, id).ToString();
    }

    private static LiteDb_Memory_Lib.ConnectionManager Manager => LiteDb_Memory_Lib.ConnectionManager.Instance();
    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string Error(string message) => $"ERROR: {message}";
}
