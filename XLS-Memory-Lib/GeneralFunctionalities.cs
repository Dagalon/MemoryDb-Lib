using System.Reflection;
using System.IO;
using ExcelDna.Integration;

namespace XLS_Memory_Lib;

public static class GeneralFunctionalities
{
    private const string Category = "General";
    
    [ExcelFunction(Name = "MEMORY_DB.PATH", Description = "Creates or replaces a named in-memory LiteDB database, or opens a file-backed database when path is provided.", Category = Category)]
    public static string GetPath()
    {
        var xllPath = ExcelDnaUtil.XllPath;
        var folder = Path.GetDirectoryName(xllPath)!;
        return folder;
    }

}