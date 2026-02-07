using DuckDB.NET.Data;

namespace DuckDb_Memory_Lib;

public class DuckTools
{
    /// <summary>
    /// Creates a new DuckDb connection using the provided path or an in-memory data source.
    /// </summary>
    public static DuckDBConnection GetInstance(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new DuckDBConnection("Data Source=:memory:;Cache=Shared");
        }

        return File.Exists(path)
            ? new DuckDBConnection($"Data Source={path};Mode=Memory")
            : new DuckDBConnection("Data Source=:memory:");
    }
}