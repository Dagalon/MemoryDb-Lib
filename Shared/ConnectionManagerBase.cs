using System.Data;
using System.Data.Common;

namespace MemoryDb_Lib.Shared;

/// <summary>
/// Thread-safe base implementation for named database connection managers.
/// </summary>
public abstract class ConnectionManagerBase<TConnection> where TConnection : DbConnection
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, TConnection> _connections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves an open connection identified by the provided alias, creating it when necessary.
    /// </summary>
    protected TConnection GetConnectionCore(string? alias, string? path, Func<string?, TConnection> connectionFactory)
    {
        var normalizedAlias = NormalizeAlias(alias);

        lock (_syncRoot)
        {
            if (_connections.TryGetValue(normalizedAlias, out var existingConnection))
            {
                EnsureOpen(existingConnection);
                return existingConnection;
            }

            var newConnection = connectionFactory(path);
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
    /// Closes and disposes all active connections managed by this instance.
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

    private static void EnsureOpen(TConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }
    }

    private static string NormalizeAlias(string? alias)
    {
        return string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim();
    }
}
