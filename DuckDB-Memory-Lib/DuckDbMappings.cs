using System.Linq.Expressions;
using DuckDB.NET.Data.Mapping;

namespace DuckDb_Memory_Lib;

public abstract class EntityAppenderMap<T> : DuckDBAppenderMap<T>
{
    protected EntityAppenderMap()
    {
        BuildMap();
    }

    protected abstract void BuildMap();
}