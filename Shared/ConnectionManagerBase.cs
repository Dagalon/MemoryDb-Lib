using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;

namespace MemoryDb_Lib.Shared;

/// <summary>Coordinates synchronous operations independently for each database alias.</summary>
public abstract class ConnectionManagerBase<TConnection> where TConnection : DbConnection
{
    private readonly ConcurrentDictionary<string, TConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    // Keep gates after close so waiting callers and a reopened alias share the same gate.
    private readonly ConcurrentDictionary<string, object> _operationGates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Protects a complete operation, including use of a returned raw connection.
    /// Dispose on the acquiring thread; do not hold across await or acquire other aliases inside it.</summary>
    public IDisposable AcquireOperation(string? alias = null) =>
        new OperationScope(_operationGates.GetOrAdd(NormalizeAlias(alias), _ => new object()));

    protected TConnection GetConnectionCore(string? alias, string? path, Func<string?, TConnection> connectionFactory)
    {
        var key = NormalizeAlias(alias);
        using var operation = AcquireOperation(key);
        if (_connections.TryGetValue(key, out var existing))
        {
            lock (existing) EnsureOpen(existing);
            return existing;
        }

        var connection = connectionFactory(path);
        try
        {
            EnsureOpen(connection);
            _connections[key] = connection;
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public void CloseConnection(string? alias = null)
    {
        var key = NormalizeAlias(alias);
        using var operation = AcquireOperation(key);
        if (!_connections.TryRemove(key, out var connection)) return;
        lock (connection)
        {
            try { connection.Close(); }
            finally { connection.Dispose(); }
        }
    }

    /// <summary>Closes a snapshot of registered aliases, waiting for their active operations.
    /// Aliases opened concurrently after the snapshot are not included.</summary>
    public void CloseAllConnections()
    {
        List<Exception>? errors = null;
        foreach (var alias in _connections.Keys.ToArray())
        {
            try { CloseConnection(alias); }
            catch (Exception ex) { (errors ??= []).Add(ex); }
        }
        if (errors is not null) throw new AggregateException(errors);
    }

    private static void EnsureOpen(TConnection connection)
    {
        if (connection.State != ConnectionState.Open) connection.Open();
    }

    private static string NormalizeAlias(string? alias) =>
        string.IsNullOrWhiteSpace(alias) ? "default" : alias.Trim();
}
