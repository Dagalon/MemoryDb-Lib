using System.Data;
using DuckDB.NET.Native;

namespace DuckDb_Memory_Lib;

public static class NetTypeToDuckDbType
{
    /// <summary>
    /// Translates a .NET <see cref="Type"/> into its DuckDB <see cref="DuckDBType"/> counterpart.
    /// </summary>
    public static DuckDBType GetDuckDbType(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => DuckDBType.Varchar,
            _ when type == typeof(int) => DuckDBType.Integer,
            _ when type == typeof(long) => DuckDBType.BigInt,
            _ when type == typeof(short) => DuckDBType.SmallInt,
            _ when type == typeof(byte) => DuckDBType.TinyInt,

            _ when type == typeof(double) => DuckDBType.Double,
            _ when type == typeof(float) => DuckDBType.Float,
            _ when type == typeof(decimal) => DuckDBType.Decimal,

            _ when type == typeof(bool) => DuckDBType.Boolean,

            _ when type == typeof(DateTime) => DuckDBType.Timestamp,
            _ when type == typeof(DateOnly) => DuckDBType.Date,
            _ when type == typeof(TimeOnly) => DuckDBType.Time,

            _ when type == typeof(byte[]) => DuckDBType.Blob,
            _ when type == typeof(Guid) => DuckDBType.Uuid,

            _ => DuckDBType.Varchar
        };
    }
    
    /// <summary>
    /// Attempts to infer the most appropriate .NET type for the provided string value,
    /// in a way that maps cleanly to DuckDB native types.
    /// </summary>
    public static (object? Value, Type NetType) StrTryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (null, typeof(string));

        // Boolean (DuckDB BOOLEAN)
        if (bool.TryParse(value, out var boolValue))
            return (boolValue, typeof(bool));

        // Integer hierarchy
        if (int.TryParse(value, out var intValue))
            return (intValue, typeof(int));

        if (long.TryParse(value, out var longValue))
            return (longValue, typeof(long));

        // Exact numeric (DuckDB DECIMAL)
        if (decimal.TryParse(value, out var decimalValue))
            return (decimalValue, typeof(decimal));

        // Floating point (DuckDB DOUBLE)
        if (double.TryParse(value, out var doubleValue))
            return (doubleValue, typeof(double));

        // Date / Timestamp (DuckDB DATE / TIMESTAMP)
        if (DateOnly.TryParse(value, out var dateValue))
            return (dateValue, typeof(DateOnly));

        if (DateTime.TryParse(value, out var timestampValue))
            return (timestampValue, typeof(DateTime));

        // UUID (DuckDB UUID)
        if (Guid.TryParse(value, out var guidValue))
            return (guidValue, typeof(Guid));

        // Fallback → VARCHAR
        return (value, typeof(string));
    }
    
    /// <summary>
    /// Maps a .NET type to the closest ADO.NET DbType.
    /// The resulting DbType can be used by DuckDB.NET to infer the native DuckDB type.
    /// </summary>
    public static DbType GetDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(bool))
            return DbType.Boolean;

        if (type == typeof(byte))
            return DbType.Byte;

        if (type == typeof(short))
            return DbType.Int16;

        if (type == typeof(int))
            return DbType.Int32;

        if (type == typeof(long))
            return DbType.Int64;

        if (type == typeof(float))
            return DbType.Single;

        if (type == typeof(double))
            return DbType.Double;

        if (type == typeof(decimal))
            return DbType.Decimal;

        if (type == typeof(DateOnly))
            return DbType.Date;

        if (type == typeof(TimeOnly))
            return DbType.Time;

        if (type == typeof(DateTime))
            return DbType.DateTime;

        if (type == typeof(Guid))
            return DbType.Guid;

        if (type == typeof(byte[]))
            return DbType.Binary;

        if (type == typeof(string))
            return DbType.String;

        throw new NotSupportedException(
            $"The .NET type '{type.FullName}' cannot be mapped to a DbType.");
    }
}



