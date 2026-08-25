namespace CasualtiesMiner.Uploader.Data.Locale;

internal sealed class GameLocale
{
    public string FileName { get; init; } = string.Empty;

    public required string Code { get; init; }

    public required IReadOnlyDictionary<string, string> Main { get; init; }
    public required IReadOnlyDictionary<string, string> Other { get; init; }
    public required IReadOnlyDictionary<string, string> Moodles { get; init; }
    public required IReadOnlyDictionary<string, string> Buildings { get; init; }
    public required IReadOnlyList<IReadOnlyList<IDictionary<string, string>>> LoreNotes { get; init; }
    public required IReadOnlyList<IDictionary<string, string>> PDA { get; init; }


    public string GetObjectName(string id, string fallback)
    {
        return Main.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : fallback;
    }

    public string GetObjectDescription(string id, string fallback)
    {
        return Main.TryGetValue(id + "dsc", out var description) ? description : fallback;
    }

    public string GetMoodles(string key, string fallback)
    {
        return Moodles.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string GetMoodlesDesc(string key, string fallback)
    {
        return Moodles.TryGetValue(key + "dsc", out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string GetOther(string key, string fallback)
    {
        return Other.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string GetOtherDesc(string key, string fallback)
    {
        return Other.TryGetValue(key + "dsc", out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string GetBuildings(string key, string fallback)
    {
        return Buildings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string GetBuildingsDesc(string key, string fallback)
    {
        return Buildings.TryGetValue(key + "dsc", out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
