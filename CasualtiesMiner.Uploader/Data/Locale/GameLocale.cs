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


    public string GetObjectName(string id, string fallback) => Main.GetValueOrDefault(id, fallback);
    public string GetObjectDescription(string id, string fallback) => Main.GetValueOrDefault(id + "dsc", fallback);
    
    public string GetMoodles(string key, string fallback) => Moodles.GetValueOrDefault(key, fallback);
    public string GetMoodlesDesc(string key, string fallback) => Moodles.GetValueOrDefault(key + "dsc", fallback);
    
    public string GetOther(string key, string fallback) => Other.GetValueOrDefault(key, fallback);
    public string GetOtherDesc(string key, string fallback) => Other.GetValueOrDefault(key + "dsc", fallback);

    public string GetBuildings(string key, string fallback) => Buildings.GetValueOrDefault(key, fallback);
    public string GetBuildingsDesc(string key, string fallback) => Buildings.GetValueOrDefault(key + "dsc", fallback);
}
