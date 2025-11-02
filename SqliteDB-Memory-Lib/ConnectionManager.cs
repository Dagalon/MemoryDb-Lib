using Microsoft.Data.Sqlite;
using System.Data;

namespace SqliteDB_Memory_Lib
{
    public sealed class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> LazyInstance =
            new(() => new ConnectionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly object _syncRoot = new();
        private readonly Dictionary<string, SqliteConnection> _connections = new(StringComparer.OrdinalIgnoreCase);

        private ConnectionManager()
        {
        }

        /// <summary>
        /// Provides access to the singleton instance of the connection manager.
        /// </summary>
        public static ConnectionManager GetInstance()
        {
            return LazyInstance.Value;
        }

        /// <summary>
        /// Retrieves an open SQLite connection identified by the provided alias, creating it when necessary.
        /// </summary>
        public SqliteConnection GetConnection(string? alias = null, string? path = null)
        {
            var normalizedAlias = NormalizeAlias(alias);

            lock (_syncRoot)
            {
                if (_connections.TryGetValue(normalizedAlias, out var existingConnection))
                {
                    EnsureOpen(existingConnection);
                    return existingConnection;
                }

                var newConnection = SqLiteLiteTools.GetInstance(path);
                EnsureOpen(newConnection);
                _connections[normalizedAlias] = newConnection;

                return newConnection;
            }
        }

        /// <summary>
        /// Closes and disposes the connection associated with the provided alias.
        /// </summary>
        public void CloseConnection(string? alias = null)
        {
            var normalizedAlias = NormalizeAlias(alias);

            lock (_syncRoot)
            {
                if (!_connections.TryGetValue(normalizedAlias, out var connection))
                {
                    return;
                }

                try
                {
                    connection.Close();
                }
                finally
                {
                    connection.Dispose();
                    _connections.Remove(normalizedAlias);
                }
            }
        }

        /// <summary>
        /// Closes and disposes all active SQLite connections managed by this instance.
        /// </summary>
        public void CloseAllConnections()
        {
            lock (_syncRoot)
            {
                foreach (var connection in _connections.Values)
                {
                    try
                    {
                        connection.Close();
                    }
                    finally
                    {
                        connection.Dispose();
                    }
                }

                _connections.Clear();
            }
        }

        /// <summary>
        /// Opens the provided SQLite connection when it is not already open.
        /// </summary>
        private static void EnsureOpen(SqliteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        /// <summary>
        /// Normalizes aliases to ensure consistent lookups inside the connection dictionary.
        /// </summary>
        private static string NormalizeAlias(string? alias)
        {
            return string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim();
        }

        /// <summary>
        /// Static helper that delegates to <see cref="CloseConnection(string?)"/>.
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
            var keepIdDataBases = _mapIdDataBase.Values.ToList();
            int indexValues = keepIdDataBases.IndexOf(idDataBase);
            string pathIdDataBase = _mapIdDataBase.Keys.ToList()[indexValues];

            _mapIdDataBase.Remove(pathIdDataBase);

        }

    }

}
