using CasualtiesMiner.Uploader;

namespace CasualtiesMiner.Cli;

public class UploadCommand : Program.Command
{
    public UploadCommand()
    {
        KeyValuePairs =
        [
            "api", "user", "password", "data", "assembly-path", "locale-dir",
            "locale", "default-locale", "locale-tag", "request-delay-ms"
        ];
        Flags = ["dry-run"];
        Usage =
            """
            Usage:
              ./CasualtiesMiner.Cli upload <mode>

            Modes:
              schemas    Upload Bucket table definitions (Bucket:* pages).
              locales    Upload Module:Locale and Module:Locale/<lang>/* from game JSON files.
              bulk       Upload locales, Bucket modules, Module:*/data, and refresh Bucket (items, liquids, moodles).
              all        schemas, then bulk (locales + modules + Bucket data).

            Options:
              --api <url>              api.php endpoint (default: casualtiesunknown.miraheze.org)
              --user / --password      Bot credentials (or CU_WIKI_USER / CU_WIKI_PASSWORD env vars).
              --data <path>            Path to data.json (default: data.json).
              --assembly <path>        Path to game assembly (default: null). 
                                       If present, it is used to generate data.json.
              --locale-dir <path>      Folder with game locale JSON (only files with a "main" object).
              --locale <path>          Single locale file (e.g. ru-RU.json); if omitted, only EN is fetched remotely.
              --locale-tag <git-ref>   Git ref for orsoniks/scavgame-locale (default: v1.0; "1.0" → v1.0).
                                       GitHub overrides local only for the same file name (ru-RU.json).
              --default-locale <code>  Locale id = JSON stem (default: EN → EN.json).
              --request-delay-ms <n>   Pause after each API call (default: 750).
              --dry-run                Preview changes without logging in or editing.
              --dry-run-output
            """;
    }
    
    public override async Task<bool> RunAsync(Memory<string> args)
    {
        if (!ParseArgs(args.Span, out var posArgs, out var kvArgs, out var flags) || posArgs.Count != 1)
        {
            Console.WriteLine(Usage);
            return false;
        }
        
        if (posArgs[0] is not ("table_schema" or "locales" or "table_data" or "all"))
        {
            Console.Error.WriteLine($"Unknown mode '{posArgs[0]}'.");
            Console.WriteLine(Usage);
            return false;
        }

        var delayMs = int.TryParse(kvArgs.GetValueOrDefault("request-delay-ms"), out var rawDelay) ? rawDelay : 750;

        var dataPath = kvArgs.GetValueOrDefault("data", "data.json");
        bool dataIsTemp = false;
        
        if (kvArgs.TryGetValue("assembly-path", out var path))
        {
            dataIsTemp = true;
            dataPath = Path.GetTempFileName();
            if (!await Dumper.Dumper.ExtractToFileAsync(path, dataPath))
            {
                Console.Error.WriteLine($"Failed to extract game data.");
                Console.WriteLine(Usage);
                return false;
            }
        }
        
        await WikiUpload.Upload(new WikiUpload.UploadOptions()
        {
            Mode = posArgs[0],
            ApiUrl = kvArgs.GetValueOrDefault("api", "https://casualtiesunknown.miraheze.org/w/api.php"),
            User = kvArgs.GetValueOrDefault("user", Environment.GetEnvironmentVariable("CU_WIKI_USER")!),
            Password = kvArgs.GetValueOrDefault("password", Environment.GetEnvironmentVariable("CU_WIKI_PASSWORD")!),
            DataPath = dataPath,
            LocaleDir = kvArgs.GetValueOrDefault("locale-dir"),
            LocalePath = kvArgs.GetValueOrDefault("locale", "EN.json"),
            DefaultLocale = kvArgs.GetValueOrDefault("default-locale", "EN"),
            LocaleTag = kvArgs.GetValueOrDefault("locale-tag", "v1.0"),
            RequestDelay = TimeSpan.FromMilliseconds(delayMs),
            DryRun = flags.Contains("dry-run"),
            DryRunOutput = kvArgs.GetValueOrDefault("dry-run-output", "dry-run-output"),
        });
        
        if (dataIsTemp)
            File.Delete(dataPath);

        return true;
    }
}