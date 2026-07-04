using System.Text.Json;

namespace CasualtiesMiner.Uploader.Data.Locale;

internal sealed class LocaleCatalog
{
    public const string DefaultLanguageCode = "EN";
    public const string DefaultRemoteTag = "v7.0.1";

    private const string RemoteRepo = "orsoniks/scavgame-locale";

    public static LocaleCatalog Empty { get; } = new([], DefaultLanguageCode);

    public IReadOnlyList<GameLocale> Locales { get; }
    public string DefaultCode { get; }
    public GameLocale? Default { get; }

    private LocaleCatalog(IReadOnlyList<GameLocale> locales, string defaultCode)
    {
        Locales = locales;
        DefaultCode = defaultCode;
        Default = locales.FirstOrDefault(l => l.Code == defaultCode) ?? locales.FirstOrDefault();
    }

    public static async Task<LocaleCatalog> LoadAsync(
        string? localeDir,
        string? localeFile,
        string defaultCode = DefaultLanguageCode,
        string remoteTag = DefaultRemoteTag,
        CancellationToken cancellationToken = default)
    {
        remoteTag = NormalizeRemoteTag(remoteTag);
        var byCode = new Dictionary<string, GameLocale>(StringComparer.Ordinal);

        foreach (var locale in LoadLocal(localeDir, localeFile))
            byCode[locale.Code] = locale;

        var remoteFileNames = CollectRemoteFileNames(localeDir, localeFile, defaultCode);
        var remotes = await FetchRemoteLocalesAsync(remoteFileNames, remoteTag, cancellationToken);

        foreach (var remote in remotes)
        {
            if (byCode.Remove(remote.Code, out _))
                Console.WriteLine($"Using {remote.FileName} from GitHub (overrides local {remote.Code}.json).");

            byCode[remote.Code] = remote;
        }

        if (!byCode.ContainsKey(defaultCode))
        {
            Console.WriteLine(
                $"Warning: default locale '{defaultCode}' not found locally or on GitHub (ref {remoteTag}).");
        }

        return new LocaleCatalog([.. byCode.Values.OrderBy(l => l.Code, StringComparer.Ordinal)], defaultCode);
    }

    private static HashSet<string> CollectRemoteFileNames(
        string? localeDir,
        string? localeFile,
        string defaultCode)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(localeDir))
        {
            var dir = ResolvePath(localeDir);
            if (Directory.Exists(dir))
            {
                foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
                {
                    if (IsGameLocaleFile(path))
                    {
                        names.Add(Path.GetFileName(path));
                    }
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(localeFile))
        {
            var file = ResolvePath(localeFile);

            if (File.Exists(file) && IsGameLocaleFile(file))
            {
                names.Add(Path.GetFileName(file));
            }
        }

        names.Add(ToLocaleFileName(defaultCode));
        return names;
    }

    private static List<GameLocale> LoadLocal(string? localeDir, string? localeFile)
    {
        var locales = new List<GameLocale>();

        if (!string.IsNullOrWhiteSpace(localeDir))
        {
            var dir = ResolvePath(localeDir);
            if (Directory.Exists(dir))
            {
                foreach (var path in Directory.EnumerateFiles(dir, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsGameLocaleFile(path))
                        locales.Add(ParseFile(path));
                }
            }

            return locales;
        }

        if (!string.IsNullOrWhiteSpace(localeFile))
        {
            var file = ResolvePath(localeFile);
            if (File.Exists(file) && IsGameLocaleFile(file))
                locales.Add(ParseFile(file));
        }

        return locales;
    }

    private static async Task<IReadOnlyList<GameLocale>> FetchRemoteLocalesAsync(
        IEnumerable<string> fileNames,
        string remoteTag,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CasualtiesMiner-Uploader/1.0");

        var locales = new List<GameLocale>();

        foreach (var fileName in fileNames.Distinct(StringComparer.Ordinal))
        {
            try
            {
                var url = $"https://raw.githubusercontent.com/{RemoteRepo}/{remoteTag}/{fileName}";
                var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Warning: {fileName} not on GitHub ref '{remoteTag}' ({(int)response.StatusCode}).");
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var stem = Path.GetFileNameWithoutExtension(fileName);

                locales.Add(ParseJson(body, stem, fileName));
                Console.WriteLine($"Loaded {fileName} from {url}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to download {fileName}: {ex.Message}");
            }
        }

        return locales;
    }

    private static string ResolvePath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static bool IsGameLocaleFile(string path)
    {
        var name = Path.GetFileName(path);
        if (IsKnownNonLocaleFileName(name))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("main", out var main)
                   && main.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    internal static string ToLocaleFileName(string codeOrPath)
    {
        var name = Path.GetFileName(codeOrPath);
        return name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.json";
    }

    internal static string NormalizeRemoteTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return DefaultRemoteTag;

        tag = tag.Trim();
        if (tag.Length > 0 && char.IsDigit(tag[0]) && !tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            return "v" + tag;

        return tag;
    }

    private static bool IsKnownNonLocaleFileName(string fileName)
    {
        if (fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase))
            return true;

        if (fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static GameLocale ParseFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var stem = Path.GetFileNameWithoutExtension(path);

        return ParseDocument(document, stem, Path.GetFileName(path));
    }

    private static GameLocale ParseJson(string json, string code, string fileName)
    {
        using var document = JsonDocument.Parse(json);
        return ParseDocument(document, code, fileName);
    }

    private static GameLocale ParseDocument(JsonDocument document, string code, string fileName)
    {
        var main = ReadStringDictionary(document.RootElement, "main");
        var other = ReadStringDictionary(document.RootElement, "other");
        var moodles = ReadStringDictionary(document.RootElement, "moodles");
        var buildings = ReadStringDictionary(document.RootElement, "buildings");

        return new GameLocale
        {
            Code = code,
            FileName = fileName,
            Main = main,
            Other = other,
            Moodles = moodles,
            Buildings = buildings,
        };
    }

    private static Dictionary<string, string> ReadStringDictionary(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var entry in element.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.String)
                result[entry.Name] = entry.Value.GetString() ?? string.Empty;
        }

        return result;
    }
}
