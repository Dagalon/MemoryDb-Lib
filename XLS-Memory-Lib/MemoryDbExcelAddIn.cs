using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class MemoryDbExcelAddIn
{
    [ExcelFunction(Name = "MEMDB.VERSION", Description = "Returns the XLS-Memory-Lib add-in version.", Category = "Memory DB")]
    public static string Version() => typeof(MemoryDbExcelAddIn).Assembly.GetName().Version?.ToString() ?? "1.0.0";
}
