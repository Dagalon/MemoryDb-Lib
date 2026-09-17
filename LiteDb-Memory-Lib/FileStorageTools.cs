using MemoryDb_Lib.Shared;
using LiteDB;

namespace LiteDb_Memory_Lib;

public static class FileStorageTools
{
    /// <summary>
    /// Uploads a file from disk into the LiteDB file storage for the provided alias.
    /// </summary>
    public static EnumsLiteDbMemory.Output Upload(ConnectionManager manager, string alias, string id, string fileName, string pathFile)
    {
        if (!File.Exists(pathFile))
        {
            return EnumsLiteDbMemory.Output.PATH_NOT_FOUND;
        }

        var db = manager.GetDatabase(alias, createIfMissing: false);
        if (db is null)
        {
            return EnumsLiteDbMemory.Output.DB_NOT_FOUND;
        }

        var fs = db.GetStorage<string>(id, GetAliasFiles(id));

        using var stream = SharedFile.OpenRead(pathFile);
        fs.Upload(fileName, pathFile, stream);

        db.Checkpoint();

        return EnumsLiteDbMemory.Output.SUCCESS;
    }

    /// <summary>
    /// Uploads a file represented by a memory stream into the LiteDB storage.
    /// </summary>
    public static EnumsLiteDbMemory.Output Upload(ConnectionManager manager, string alias, string id, string fileName, MemoryStream? stream)
    {
        var db = manager.GetDatabase(alias, createIfMissing: false);
        if (db is null)
        {
            return EnumsLiteDbMemory.Output.DB_NOT_FOUND;
        }

        var fs = db.GetStorage<string>(id, GetAliasFiles(id));

        if (stream is null)
        {
            if (!File.Exists(fileName))
            {
                return EnumsLiteDbMemory.Output.PATH_NOT_FOUND;
            }

            using var sourceFile = SharedFile.OpenRead(fileName);
            fs.Upload(fileName, fileName, sourceFile);
        }
        else
        {
            fs.Upload(fileName, fileName, stream);
        }

        db.Checkpoint();

        return EnumsLiteDbMemory.Output.SUCCESS;

    }

    /// <summary>
    /// Retrieves the reference of a stored file by using its identifier.
    /// </summary>
    public static LiteFileInfo<string>? Find(ConnectionManager manager, string alias, string id, string fileName)
    {
        var db = manager.GetDatabase(alias, createIfMissing: false);

        if (db is null)
        {
            throw new Exception($"The database {alias} is not registered in the connection");
        }

        var fs = db.GetStorage<string>(id, GetAliasFiles(id));
        LiteFileInfo<string>? output = fs.FindById(fileName);

        return output;

    }

    /// <summary>
    /// Builds the internal alias used to store the file chunks.
    /// </summary>
    private static string GetAliasFiles(string alias)
    {
        return $"{alias}chunk";
    }

}
