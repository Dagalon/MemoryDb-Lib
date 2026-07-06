using DuckDB.NET.Data;
using MemoryDb_Lib.Shared;


namespace DuckDb_Memory_Lib;

public sealed class ConnectionManager : ConnectionManagerBase<DuckDBConnection>
{
    private static readonly Lazy<ConnectionManager> LazyInstance =
        new(() => new ConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

    private ConnectionManager() { }

    /// <summary>
    /// Provides access to the singleton instance of the connection manager.
    /// </summary>
    public static ConnectionManager GetInstance()
    {
        return LazyInstance.Value;
    }

    /// <summary>
    /// Retrieves an open DuckDB connection identified by the provided alias, creating it when necessary.
    /// </summary>
    public DuckDBConnection GetConnection(string? alias = null, string? path = null)
    {
        return GetConnectionCore(alias, path, DuckTools.GetInstance);
    }

    /// <summary>
    /// Static helper that delegates to <see cref="ConnectionManagerBase{TConnection}.CloseConnection(string?)"/>.
    /// </summary>
    public static void Close(string? alias = null)
    {
        GetInstance().CloseConnection(alias);
    }

    /// <summary>
    /// Static helper that disposes every managed connection.
    /// </summary>
    public static void CloseAll()
    {
        GetInstance().CloseAllConnections();
    }
}

    public sealed class KeeperRegisterIdDataBase
    {
        private static readonly Dictionary<string, string> _mapIdDataBase = new Dictionary<string, string>();

        private KeeperRegisterIdDataBase() { }

        /// <summary>
        /// Retrieves the database identifier registered for the provided file path.
        /// </summary>
        public static string GetIdDataBase(string path)
        {
            string idDataBase = "";
            if (_mapIdDataBase.ContainsKey(path))
            {
                idDataBase = _mapIdDataBase[path];
            }

            return idDataBase;
        }

        /// <summary>
        /// Verifies whether the supplied path is already registered.
        /// </summary>
        public static bool CheckPathDataBase(string path)
        {
            return _mapIdDataBase.ContainsKey(path);
        }

        /// <summary>
        /// Verifies whether the supplied identifier is already associated with a path.
        /// </summary>
        public static bool CheckIdDataBase(string idDb)
        {
            return _mapIdDataBase.ContainsValue(idDb);
        }

        /// <summary>
        /// Registers the relationship between a database file path and its identifier.
        /// </summary>
        public static void Register(string path, string idDataBase)
        {
            if (!CheckPathDataBase(path))
            {
                _mapIdDataBase[path] = idDataBase;
            }
        }

        /// <summary>
        /// Removes the record for the supplied database identifier.
        /// </summary>
        public static void DeleteRegister(string idDataBase)
        {
            var pathIdDataBase = _mapIdDataBase
                .FirstOrDefault(entry => string.Equals(entry.Value, idDataBase, StringComparison.OrdinalIgnoreCase))
                .Key;

            if (pathIdDataBase is not null)
            {
                _mapIdDataBase.Remove(pathIdDataBase);
            }
        }
}
