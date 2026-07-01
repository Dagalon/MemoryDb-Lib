using ExcelDna.Integration;

namespace XLS_Memory_Lib;

using System.Runtime.InteropServices;

public class AddIn : IExcelAddIn
{
    public void AutoOpen()
    {
        NativeLoader.Initialize();
    }

    public void AutoClose()
    {
    }
}

internal static class NativeLoader
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);

    public static void Initialize()
    {
        var baseDir =  Path.GetDirectoryName(ExcelDnaUtil.XllPath)!;
        var nativeDir = Path.Combine(baseDir, "native", "x64");

        SetDllDirectory(nativeDir);
    }
}