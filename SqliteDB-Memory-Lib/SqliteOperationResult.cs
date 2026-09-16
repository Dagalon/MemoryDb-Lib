namespace SqliteDB_Memory_Lib;

/// <summary>
/// Represents the outcome of an SQLite operation, including the exception message when it fails.
/// </summary>
public readonly struct SqliteOperationResult : IEquatable<SqliteOperationResult>
{
    public SqliteOperationResult(EnumsSqliteMemory.Output output, string? exceptionMessage = null)
    {
        Output = output;
        ExceptionMessage = exceptionMessage;
    }

    public EnumsSqliteMemory.Output Output { get; }

    public string? ExceptionMessage { get; }

    public bool IsSuccess => Output == EnumsSqliteMemory.Output.SUCCESS;

    public static implicit operator SqliteOperationResult(EnumsSqliteMemory.Output output) => new(output);

    public static implicit operator EnumsSqliteMemory.Output(SqliteOperationResult result) => result.Output;

    public bool Equals(SqliteOperationResult other) =>
        Output == other.Output && ExceptionMessage == other.ExceptionMessage;

    public override bool Equals(object? obj) => obj switch
    {
        SqliteOperationResult result => Equals(result),
        EnumsSqliteMemory.Output output => Output == output,
        _ => false
    };

    public override int GetHashCode() => HashCode.Combine(Output, ExceptionMessage);

    public override string ToString() => Output.ToString();
}
