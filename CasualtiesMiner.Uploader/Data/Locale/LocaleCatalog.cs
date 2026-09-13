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
        var characters = ReadCharacterNotes(document.RootElement, "character");
        var loreNotes = ReadLoreNotes(document.RootElement, "notes");
        var pda = ReadPda(document.RootElement, "pdaNotes");
        var pauseQuotes = ReadStringList(document.RootElement, "pauseQuotes");
        
        // TODO: Manual changes for survivor notes that were removed at the author's request from 7.0.1 onwards
        // Remove / change these if needed

        string? item1;
        bool doSurvivorNoteChanges =
            loreNotes.Count >= 2 &&
            loreNotes[0].Count >= 29 &&
            loreNotes[2].Count >= 2 &&
            loreNotes[0][17].TryGetValue("Item1", out item1) && item1.StartsWith("i found a weapon. a firearm.") &&
            loreNotes[0][18].TryGetValue("Item1", out item1) && item1.StartsWith("im so mad at myself for acting") &&
            loreNotes[0][26].TryGetValue("Item1", out item1) && item1.StartsWith("It's full of all kinds of") &&
            loreNotes[0][28].TryGetValue("Item1", out item1) && item1.StartsWith("I'm feeling lonely... so I") &&
            loreNotes[3][1].TryGetValue("Item1", out item1) && item1.StartsWith("i cant anymore. why.");

        if (doSurvivorNoteChanges)
        {
            loreNotes[0][17]["Item1"] =
                "i found some kind of... shooty device. scared the hell out of myself with it... and destroyed whatever that trap was infront of me. not used to operating this thing.";
            loreNotes[0][18]["Item1"] =
                "everyone keeps shooing me from their pods. they remember me... from up there. i shouldn't have acted like that. this is terrible.";
            loreNotes[0].RemoveAt(28);
            loreNotes[0].RemoveAt(26);
            loreNotes[3][1]["Item1"] =
                "its over. goodbye. sorry.";
        }
        else
        {
            Console.WriteLine("WARNING: Could not apply the survivor note removals!");
        }

        return new GameLocale
        {
            Code = code,
            FileName = fileName,
            Main = main,
            Other = other,
            Moodles = moodles,
            Characters = characters,
            Buildings = buildings,
            LoreNotes = loreNotes,
            PDA = pda,
            PauseQuotes = pauseQuotes
        };
    }
    
    private static List<string> ReadStringList(JsonElement root, string propertyName)
    {
        var result = new List<string>();

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"Warn: locale property {propertyName} could not be read correctly!");
            return result;
        }

        foreach (var (index, entry) in element.EnumerateArray().Index())
        {
            string? value = null;
            
            if (entry.ValueKind == JsonValueKind.String)
                value = entry.GetString();

            if (value != null)
                result.Add(value);
            else
                Console.WriteLine($"Warn: locale property {propertyName}.{index} could not be read correctly!");
        }

        return result;
    }

    private static Dictionary<string, string> ReadStringDictionary(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine($"Warn: locale property {propertyName} could not be read correctly!");
            return result;
        }

        foreach (var entry in element.EnumerateObject())
        {
            string? value = null;
            
            if (entry.Value.ValueKind == JsonValueKind.String)
                value = entry.Value.GetString();

            if (value != null)
                result[entry.Name] = value;
            else
                Console.WriteLine($"Warn: locale property {propertyName}.{entry.Name} could not be read correctly!");
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadCharacterNotes(JsonElement root, string propertyName)
    {
        var result = new List<IReadOnlyDictionary<string, IReadOnlyList<string>>>();

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"Warn: locale property {propertyName} could not be read correctly!");
            return result;
        }

        foreach (var (index, character) in element.EnumerateArray().Index())
        {
            var characterResult = new Dictionary<string, IReadOnlyList<string>>();
            result.Add(characterResult);

            if (character.ValueKind == JsonValueKind.Object)
            {
                foreach (var entry in character.EnumerateObject())
                {
                    var lines = new List<string>();

                    if (entry.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var (lineIndex, lineValue) in entry.Value.EnumerateArray().Index())
                        {
                            string? line = null;

                            if (lineValue.ValueKind == JsonValueKind.String)
                                line = lineValue.GetString();
                            
                            if (line != null)
                                lines.Add(line);
                            else
                                Console.WriteLine($"Warn: locale property {propertyName}.{index}.{entry.Name}.{lineIndex} could not be read correctly!");
                        }

                        characterResult[entry.Name] = lines;
                    }
                    else
                    {
                        Console.WriteLine($"Warn: locale property {propertyName}.{index}.{entry.Name} could not be read correctly!");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Warn: locale property {propertyName}.{index} could not be read correctly!");
            }
        }

        return result;
    }

    private static List<Dictionary<string, string>> ReadPda(JsonElement root, string propertyName)
    {
        var pdaList = new List<Dictionary<string, string>>();

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"Warn: locale property {propertyName} could not be read correctly!");
            return pdaList;
        }

        foreach (var (index, pda) in element.EnumerateArray().Index())
        {
            var pdaResult = new Dictionary<string, string>();
            pdaList.Add(pdaResult);

            if (pda.ValueKind == JsonValueKind.Object)
            {
                foreach (var entry in pda.EnumerateObject())
                {
                    string? value = null;
            
                    if (entry.Value.ValueKind == JsonValueKind.String)
                        value = entry.Value.GetString();

                    if (value != null)
                        pdaResult[entry.Name] = value;
                    else
                        Console.WriteLine($"Warn: locale property {propertyName}.{index}.{entry.Name} could not be read correctly!");
                }
            }
            else
            {
                Console.WriteLine($"Warn: locale property {propertyName}.{index} could not be read correctly!");
            }
        }
        
        return pdaList;
    }

    private static List<List<Dictionary<string, string>>> ReadLoreNotes(JsonElement root, string propertyName)
    {
        var result = new List<List<Dictionary<string, string>>>();

        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"Warn: locale property {propertyName} could not be read correctly!");
            return result;
        }

        foreach (var (layerIndex, layer) in element.EnumerateArray().Index())
        {
            var layerResult = new List<Dictionary<string, string>>();
            result.Add(layerResult);
            
            if (layer.ValueKind == JsonValueKind.Array)
            {
                foreach (var (noteIndex, note) in layer.EnumerateArray().Index())
                {
                    var notesResult = new Dictionary<string, string>();
                    layerResult.Add(notesResult);

                    if (note.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var entry in note.EnumerateObject())
                        {
                            string? value = null;
            
                            if (entry.Value.ValueKind == JsonValueKind.String)
                                value = entry.Value.GetString();

                            if (value != null)
                                notesResult[entry.Name] = value;
                            else
                                Console.WriteLine(
                                    $"Warn: locale property {propertyName}.{layerIndex}.{entry.Name}.{entry.Name} could not be read correctly!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warn: locale property {propertyName}.{layerIndex}.{noteIndex} could not be read correctly!");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Warn: locale property {propertyName}.{layerIndex} could not be read correctly!");
            }
        }

        return result;
    }
}
