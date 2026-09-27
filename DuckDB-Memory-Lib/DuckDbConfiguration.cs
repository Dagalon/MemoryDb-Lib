using DuckDB.NET.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DuckDb_Memory_Lib;

/// <summary>Configuration for newly created connections. Existing connections retain their settings.</summary>
public sealed class DuckDbConfiguration
{
    public const string FileName = "memory-db.json";
    private static DuckDbConfiguration _current = new()
    {
        TempPath = AppContext.BaseDirectory,
        ExtensionPath = AppContext.BaseDirectory
    };

    [JsonPropertyName("temp_path")]
    public string? TempPath { get; init; }


    [JsonPropertyName("extension_path")]
    public string? ExtensionPath { get; init; }
 
    public static DuckDbConfiguration Current => Volatile.Read(ref _current);

    /// <summary>Loads the optional JSON file relative to the add-in directory.</summary>
    public static void Initialize(string addInDirectory, string? configurationFile = null)
    {
        if (string.IsNullOrWhiteSpace(addInDirectory))
        {
            throw new ArgumentException("The add-in folder must not be empty.", nameof(addInDirectory));
        }
      
        var configuration = Load(addInDirectory: addInDirectory, configurationFile: configurationFile);
        Volatile.Write(ref _current, configuration);
      
    }

    public static DuckDbConfiguration Load(
        string addInDirectory,
        string? configurationFile = null)
    {
        var directory = Path.GetFullPath(addInDirectory);

        var name = string.IsNullOrWhiteSpace(configurationFile)
            ? FileName
            : configurationFile;

        var file = Path.GetFullPath(name, directory);

        var configFile = File.Exists(file)
            ? JsonSerializer.Deserialize<DuckDbConfiguration>(
                  File.ReadAllText(file),
                  new JsonSerializerOptions
                  {
                      PropertyNameCaseInsensitive = true
                  })
              ?? throw new InvalidDataException(
                  $"Invalid DuckDB configuration: {file}")
            : new DuckDbConfiguration();

        return new DuckDbConfiguration
        {
            TempPath = Resolve(configFile.TempPath, directory),
            ExtensionPath = Resolve(configFile.ExtensionPath, directory)
        };
    }

    private static string Resolve(string? path, string directory)
    {
        return string.IsNullOrWhiteSpace(path)
            ? directory
            : Path.GetFullPath(path, directory);
    }

    internal void Apply(DuckDBConnection connection, string temporaryDirectory)
    {
        Directory.CreateDirectory(temporaryDirectory);
        Directory.CreateDirectory(ExtensionPath!);
        using var command = connection.CreateCommand();
        command.CommandText = $"SET temp_directory = '{temporaryDirectory.Replace("'", "''")}'; " +
                              $"SET extension_directory = '{ExtensionPath!.Replace("'", "''")}';";
        command.ExecuteNonQuery();
    }
}
