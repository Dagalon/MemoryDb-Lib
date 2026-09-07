namespace DuckDb_Memory_Lib;

/// <summary>
/// Represents the outcome of a DuckDB operation, including the exception message when it fails.
/// </summary>
public readonly struct DuckOperationResult : IEquatable<DuckOperationResult>
{
    public DuckOperationResult(EnumsDuckMemory.Output output, string? exceptionMessage = null)
    {
        Output = output;
        ExceptionMessage = exceptionMessage;
    }

    public EnumsDuckMemory.Output Output { get; }

    public string? ExceptionMessage { get; }

    public bool IsSuccess => Output == EnumsDuckMemory.Output.SUCCESS;

    public static implicit operator DuckOperationResult(EnumsDuckMemory.Output output) => new(output);

    public static implicit operator EnumsDuckMemory.Output(DuckOperationResult result) => result.Output;

    public bool Equals(DuckOperationResult other) =>
        Output == other.Output && ExceptionMessage == other.ExceptionMessage;

    public override bool Equals(object? obj) => obj switch
    {
        DuckOperationResult result => Equals(result),
        EnumsDuckMemory.Output output => Output == output,
        _ => false
    };

    public override int GetHashCode() => HashCode.Combine(Output, ExceptionMessage);

    public override string ToString() => Output.ToString();
}
