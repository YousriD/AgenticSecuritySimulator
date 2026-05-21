using System.Text.Json;

namespace AgenticSecuritySimulator.Core.Scenarios;

public static class ScenarioCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<ScenarioDefinition> LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return [];

        return Directory.EnumerateFiles(directory, "*.json")
            .Select(LoadFromFile)
            .OrderBy(s => s.Id)
            .ToList();
    }

    public static ScenarioDefinition LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ScenarioDefinition>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Could not parse scenario: {path}");
    }
}
