using System.Diagnostics;
using LiteDB;
using System.Threading;

namespace LiteDb_Memory_Lib;

public sealed class ConnectionManager
{
    #region Attributes

    private static readonly Lazy<ConnectionManager> LazyInstance =
        new(() => new ConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _syncRoot = new();

    private readonly Dictionary<string, LiteDatabase> _databases;

    private readonly Dictionary<string, MemoryStream> _memoryFiles;

    #endregion

    #region Constructors

    private ConnectionManager()
    {
        _databases = new Dictionary<string, LiteDatabase>(StringComparer.OrdinalIgnoreCase);
        _memoryFiles = new Dictionary<string, MemoryStream>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Retrieves a previously registered database by using its alias.
    /// </summary>
    public LiteDatabase? GetDatabase(string alias, bool createIfMissing = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        lock (_syncRoot)
        {
            if (_databases.TryGetValue(alias, out var database))
            {
                return database;
            }

            return !createIfMissing ? null : CreateInMemoryDatabaseLocked(alias, replaceExisting: false);
        }
    }

    /// <summary>
    /// Creates a new database connection or replaces the existing one using the provided alias.
    /// </summary>
    public EnumsLiteDbMemory.Output CreateDatabase(string alias, string? path = null, bool substituteIfExist = false,
        bool isShared = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        lock (_syncRoot)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (!substituteIfExist && _databases.ContainsKey(alias))
                {
                    return EnumsLiteDbMemory.Output.THERE_EXISTS_DATABASE;
                }

                CreateInMemoryDatabaseLocked(alias, substituteIfExist);
                return EnumsLiteDbMemory.Output.SUCCESS;
            }

            if (!File.Exists(path))
            {
                return EnumsLiteDbMemory.Output.PATH_NOT_FOUND;
            }

            if (_databases.ContainsKey(alias))
            {
                if (!substituteIfExist)
                {
                    return EnumsLiteDbMemory.Output.THERE_EXISTS_DATABASE;
                }

                DisposeDatabaseLocked(alias);
            }

            var connectionString = isShared
                ? @$"filename={path};connection=shared"
                : @$"filename={path};connection=direct";

            _databases[alias] = new LiteDatabase(new ConnectionString(connectionString));
            if (_memoryFiles.Remove(alias, out var memoryStream))
            {
                memoryStream.Dispose();
            }

            return EnumsLiteDbMemory.Output.SUCCESS;
        }
    }

    /// <summary>
    /// Closes an existing database and optionally persists the in-memory contents to disk.
    /// </summary>
    public EnumsLiteDbMemory.Output Close(string alias, string? pathToKeep = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        LiteDatabase? database;
        MemoryStream? memoryStream = null;

        lock (_syncRoot)
        {
            if (!_databases.TryGetValue(alias, out database))
            {
                return EnumsLiteDbMemory.Output.DB_NOT_FOUND;
            }

            _databases.Remove(alias);

            if (_memoryFiles.TryGetValue(alias, out memoryStream))
            {
                _memoryFiles.Remove(alias);
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(pathToKeep) && memoryStream is not null)
            {
                memoryStream.Position = 0;

                var directory = Path.GetDirectoryName(pathToKeep);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = File.Create(pathToKeep);
                memoryStream.CopyTo(fileStream);
            }

            return EnumsLiteDbMemory.Output.SUCCESS;
        }
        finally
        {
            memoryStream?.Dispose();
            database?.Dispose();
        }
    }

    /// <summary>
    /// Creates a new collection in the selected database and seeds it with optional documents.
    /// </summary>
    public EnumsLiteDbMemory.Output CreateCollection<T>(string alias, string collection, List<T>? documents = null,
        bool useInsertBulk = false) where T : new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);

        var db = GetDatabase(alias, createIfMissing: false);
        if (db is null)
        {
            return EnumsLiteDbMemory.Output.DB_NOT_FOUND;
        }

        var dbCollection = db.GetCollection<T>(collection);

        if (documents is not null && documents.Count > 0)
        {
            if (useInsertBulk)
            {
                dbCollection.InsertBulk(documents);
            }
            else
            {
                dbCollection.Insert(documents);
            }
        }
        else
        {
            dbCollection.Insert(new T());
        }

        db.Checkpoint();
        return EnumsLiteDbMemory.Output.SUCCESS;
    }

    /// <summary>
    /// Creates a new collection and populates it with data from a JSON file.
    /// </summary>
    public EnumsLiteDbMemory.Output CreateCollection<T>(string alias, string collection, string path,
        bool useInsertBulk = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var db = GetDatabase(alias, createIfMissing: false);
        if (db is null)
        {
            return EnumsLiteDbMemory.Output.DB_NOT_FOUND;
        }

        if (!JsonTools.TryReadJson<List<T>>(path, out var documents))
        {
            return EnumsLiteDbMemory.Output.PATH_NOT_FOUND;
        }

        if (documents is null || documents.Count == 0)
        {
            return EnumsLiteDbMemory.Output.PATH_IS_NULL_OR_EMPTY;
        }

        var dbCollection = db.GetCollection<T>(collection);

        if (useInsertBulk)
        {
            dbCollection.InsertBulk(documents);
        }
        else
        {
            dbCollection.Insert(documents);
        }

        db.Checkpoint();
        return EnumsLiteDbMemory.Output.SUCCESS;
    }

    /// <summary>
    /// Retrieves a strongly typed collection from the managed database.
    /// </summary>
    public ILiteCollection<T>? GetCollection<T>(string alias, string collection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);

        lock (_syncRoot)
        {
            return _databases.TryGetValue(alias, out var db) ? db.GetCollection<T>(collection) : null;
        }
    }

    /// <summary>
    /// Retrieves the names of all collections registered in the selected database.
    /// </summary>
    public List<string> GetCollectionNames(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        lock (_syncRoot)
        {
            return _databases.TryGetValue(alias, out var db)
                ? db.GetCollectionNames().ToList()
                : new List<string>();
        }
    }

    /// <summary>
    /// Provides access to the singleton instance of the connection manager.
    /// </summary>
    public static ConnectionManager Instance()
    {
        return LazyInstance.Value;
    }

    /// <summary>
    /// Creates an in-memory database within a thread-safe context.
    /// </summary>
    private LiteDatabase CreateInMemoryDatabaseLocked(string alias, bool replaceExisting)
    {
        if (replaceExisting)
        {
            DisposeDatabaseLocked(alias);
        }
        else if (_databases.ContainsKey(alias))
        {
            return _databases[alias];
        }

        var memoryStream = new MemoryStream();

        try
        {
            var database = new LiteDatabase(memoryStream);
            _databases[alias] = database;
            _memoryFiles[alias] = memoryStream;
            return database;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            memoryStream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Releases the LiteDB connection and associated stream for the provided alias.
    /// </summary>
    private void DisposeDatabaseLocked(string alias)
    {
        if (_databases.Remove(alias, out var existingDb))
        {
            existingDb.Dispose();
        }

        if (_memoryFiles.Remove(alias, out var existingStream))
        {
            existingStream.Dispose();
        }
    }

    #endregion
}
