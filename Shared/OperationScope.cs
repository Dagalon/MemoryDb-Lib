namespace MemoryDb_Lib.Shared;

/// <summary>A reentrant, synchronous operation scope. Dispose on the acquiring thread.</summary>
internal sealed class OperationScope : IDisposable
{
    private object? _gate;

    internal OperationScope(object gate)
    {
        Monitor.Enter(gate);
        _gate = gate;
    }

    public void Dispose()
    {
        var gate = _gate;
        if (gate is null) return;
        Monitor.Exit(gate);
        _gate = null;
    }
}
