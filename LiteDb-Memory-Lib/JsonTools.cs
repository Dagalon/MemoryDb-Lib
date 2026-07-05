using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace LiteDb_Memory_Lib;

public static class JsonTools
{
    /// <summary>
    /// Reads a JSON file from disk and deserializes it into the requested type.
    /// </summary>
    public static T? ReadJson<T>(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("The JSON path cannot be null or whitespace.", nameof(jsonPath));
        }

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException("The JSON file could not be located.", jsonPath);
        }

        using StreamReader reader = new(jsonPath);
        var json = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// Attempts to read a JSON file from disk and deserialize it into the requested type.
    /// </summary>
    public static bool TryReadJson<T>(string jsonPath, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            var value = ReadJson<T>(jsonPath);
            result = value!;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            Debug.WriteLine(ex);
            result = default;
            return false;
        }
    }
}
