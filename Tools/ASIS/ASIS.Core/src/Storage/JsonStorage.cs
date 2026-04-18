using System.Text.Json;

namespace ASIS.Core.Storage;

public static class JsonStorage
{
    private static JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path))
            return new T();

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<T>(json, _options) ?? new T();
    }

    public static void Save<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, _options);

        File.WriteAllText(path, json);
    }
}