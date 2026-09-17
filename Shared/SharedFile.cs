namespace MemoryDb_Lib.Shared;

/// <summary>Reads input files without denying concurrent writers or file replacement.</summary>
internal static class SharedFile
{
    // The existing owner's sharing permissions still apply. This is not a snapshot
    // of a file being modified and must not be used to copy live database files.
    public static FileStream OpenRead(string path) => new(
        Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
        FileShare.ReadWrite | FileShare.Delete);

    public static StreamReader OpenText(string path) => new(OpenRead(path));

    public static string ReadAllText(string path)
    {
        using var reader = OpenText(path);
        return reader.ReadToEnd();
    }
}
