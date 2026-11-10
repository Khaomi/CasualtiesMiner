namespace CasualtiesMiner.Shared;

public static class MoodleIconSizes
{
    private static readonly Dictionary<string, int> KnownSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["miserable"] = 30,
        ["maxbleeding"] = 28,
    };

    public static int GetSourceSize(string icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return 20;
        }

        var stem = icon;
        var dot = stem.LastIndexOf('.');
        if (dot > 0)
        {
            stem = stem[..dot];
        }

        if (stem.StartsWith("Moodle", StringComparison.OrdinalIgnoreCase) && stem.Length > 6)
        {
            stem = stem[6..];
        }

        return KnownSizes.GetValueOrDefault(stem, 20);
    }
}
