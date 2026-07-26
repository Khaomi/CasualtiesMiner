using System.Text.Json;
using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data;
using CasualtiesMiner.Uploader.Data.Enums;
using CasualtiesMiner.Uploader.Data.Locale;
using CasualtiesMiner.Uploader.Data.Mappers;
using CasualtiesMiner.Uploader.MediaWiki;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader;

public static class Program
{
    public static readonly string DryRunFolder = Path.Combine("dry-run-output");

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

        var mode = args[0].ToLowerInvariant();
        var options = CliOptions.Parse(args);

        if (mode is not ("schemas" or "locales" or "bulk" or "all"))
        {
            Console.Error.WriteLine($"Unknown mode '{mode}'.");
            PrintUsage();

            return 1;
        }

        var dataRows = await LoadDumpedData(options);
        var locales = await LoadLocalesAsync(options);

        Console.WriteLine($"Loaded {dataRows.Items.Count} items from {options.DataPath}.");
        Console.WriteLine($"Loaded {dataRows.Liquids.Count} liquids from {options.DataPath}.");
        Console.WriteLine($"Loaded {dataRows.Tiles.Count} blocks from {options.DataPath}.");
        Console.WriteLine($"Loaded {dataRows.Moodles.Count} moodles from {options.DataPath}.");
        Console.WriteLine($"Loaded {dataRows.BuildingEntities.Count} buildings from {options.DataPath}.");
        Console.WriteLine($"Loaded {locales.Locales.Count} locale(s) (default: {locales.DefaultCode}).");

        using var client = new MediaWikiClient(options.ApiUrl, options.RequestDelay);

        if (!options.DryRun)
        {
            if (string.IsNullOrEmpty(options.User) || string.IsNullOrEmpty(options.Password))
            {
                Console.Error.WriteLine(
                    "Credentials required for a live run. Pass --user/--password or set CU_WIKI_USER/CU_WIKI_PASSWORD, or use --dry-run.");
                return 1;
            }

            Console.WriteLine($"Logging in to {options.ApiUrl} as {options.User} ...");
            await client.LoginAsync(options.User!, options.Password!);
            Console.WriteLine("Login successful.");
        }
        else
        {
            if (Directory.Exists(DryRunFolder))
                Directory.Delete(DryRunFolder, true);
            Directory.CreateDirectory(DryRunFolder);

            Console.WriteLine("Dry run: no edits will be performed.");
        }

        if (mode is "schemas" or "all")
        {
            await UploadSchemasAsync(client, options);
        }

        if (mode is "locales" or "bulk" or "all")
        {
            await UploadLocalesAsync(client, locales, dataRows, options);
        }

        if (mode is "bulk" or "all")
        {
            await UploadBulkAsync(client, dataRows, options);
        }

        Console.WriteLine("Done.");
        return 0;
    }

    private static async Task UploadSchemasAsync(MediaWikiClient client, CliOptions options)
    {
        Console.WriteLine("== Uploading Bucket schemas ==");

        foreach (var (bucket, schema) in BucketSchemas.All())
        {
            var title = "Bucket:" + bucket;
            var status = await client.EditAsync(title, schema, "Update Bucket schema", options.DryRun);
            Console.WriteLine($"  {title}: {status}");
        }
    }

    private static async Task UploadLocalesAsync(
        MediaWikiClient client,
        LocaleCatalog locales,
        DataRows dataRows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading locale modules ==");

        if (locales.Locales.Count == 0)
        {
            Console.WriteLine("  Warning: no locale files found; skipping Module:Locale/<lang>/*.");
            return;
        }

        var itemIds = dataRows.Items.Select(r => r.ItemId).ToArray();
        var moodleItems = dataRows.Moodles.Select(r => r.LocaleId).ToArray();
        var blockItems = dataRows.Tiles.Select(r => r.Name).ToArray();
        var buildingItems = dataRows.BuildingEntities.Select(r => r.Id).ToArray();

        foreach (var locale in locales.Locales)
        {
            await UploadWikiLocale(client, options, locale, "items", LocaleWikiGenerator.BuildLocaleModule(
                locale,
                itemIds.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Item))));

            await UploadWikiLocale(client, options, locale, "liquids", LocaleWikiGenerator.BuildLocaleModule(
                locale,
                dataRows.Liquids.Select(LocaleModuleEntry.CreateFromLiquid)));

            await UploadWikiLocale(client, options, locale, "blocks", LocaleWikiGenerator.BuildLocaleModule(
                locale,
                blockItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Block))));
            
            await UploadWikiLocale(client, options, locale, "moodles", LocaleWikiGenerator.BuildLocaleModule(
                locale,
                moodleItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Moodle))));

            await UploadWikiLocale(client, options, locale, "buildings", LocaleWikiGenerator.BuildLocaleModule(
                locale,
                buildingItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Building))));

            await UploadWikiLocale(client, options, locale, "ui", LocaleWikiGenerator.BuildUiModule(locale));
        }
    }

    private static async Task UploadBulkAsync(
        MediaWikiClient client,
        DataRows dataRows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading bulk Bucket data ==");

        List<(string ModuleName, string TargetBucket, string ModuleData)> wikiContents =
        [
            ("Item", "Item", WikiGenerator.BuildItemDataModule(dataRows.Items)),
            ("ItemBattery", "Item battery", WikiGenerator.BuildItemBatteryDataModule(dataRows.Items)),
            ("ItemLiquid", "Item liquid", WikiGenerator.BuildItemLiquidDataModule(dataRows.Items)),
            ("Liquid", "Liquid", WikiGenerator.BuildLiquidDataModule(dataRows.Liquids)),
            ("Block", "Block", WikiGenerator.BuildBlockDataModule(dataRows.Tiles)),
            ("Recipe", "Recipe", WikiGenerator.BuildRecipeDataModule(dataRows.Recipes)),
            ("RecipeItem", "Recipe ingridient", WikiGenerator.BuildRecipeItemDataModule(dataRows.RecipeItems)),
            ("RecipeResult", "Recipe result", WikiGenerator.BuildRecipeResultDataModule(dataRows.RecipeResults)),
            ("Moodle", "Moodle", WikiGenerator.BuildMoodleDataModule(dataRows.Moodles)),
            ("Building", "Building", WikiGenerator.BuildBuildingDataModule(dataRows.BuildingEntities)),
            ("GameField", "Gamefield", WikiGenerator.BuildGameFieldDataModule(dataRows.GameFields)),
            ("BodyField", "Bodyfield", WikiGenerator.BuildBodyFieldDataModule(dataRows.BodyFields))
        ];

        foreach (var (moduleName, targetBucket, moduleData) in wikiContents)
        {
            await UploadWikiContent(
                client, options,
                moduleName, targetBucket,
                moduleData);
        }
    }

    private static async Task UploadWikiLocale(
        MediaWikiClient client, 
        CliOptions options,
        GameLocale locale,
        string suffix,
        string localeModuleContents)
    {
        var uiTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, suffix);
        var uiStatus = await client.EditAsync(
            uiTitle,
            localeModuleContents,
            $"Update {locale.Code} strings for {suffix}",
            options.DryRun);
        Console.WriteLine($"  {uiTitle}: {uiStatus}");
    }

    private static async Task UploadWikiContent(
        MediaWikiClient client,
        CliOptions options,
        string moduleBaseName,
        string targetBucket,
        string dataModuleContents)
    {
        var dataModuleTitle = WikiContent.MakeDataModuleTitle(moduleBaseName);
        var triggerPageTitle = WikiContent.MakeTriggerPageTitle(moduleBaseName);

        var data = await client.EditAsync(
            dataModuleTitle,
            dataModuleContents,
            $"Regenerate {moduleBaseName} module data",
            options.DryRun);
        Console.WriteLine($"  {dataModuleTitle}: {data}");
        
        var trigger = await client.EditAsync(
            triggerPageTitle,
            WikiContent.MakeTriggerPage(dataModuleTitle, targetBucket),
            $"Refresh {moduleBaseName} Bucket data",
            options.DryRun);
        Console.WriteLine($"  {triggerPageTitle}: {trigger}");
    }

    private static async Task<DataRows> LoadDumpedData(CliOptions options)
    {
        var data = await File.ReadAllTextAsync(options.DataPath);
        
        var dataJson = JsonSerializer.Deserialize<DumpedData>(data, DumpedData.SerializationOptions)!;
        
        return new DataRows
        {
            Items = dataJson.Items
                .Where(item => !string.IsNullOrWhiteSpace(item.fullName))
                .Select(ItemRowMapper.Map)
                .OrderBy(row => row.ItemId, StringComparer.Ordinal)
                .ToList(),
            Liquids = dataJson.Liquids
                .Where(item => !string.IsNullOrWhiteSpace(item.liquidId) || !string.IsNullOrWhiteSpace(item.localeName))
                .Select(LiquidRowMapper.Map)
                .OrderBy(row => row.LiquidId, StringComparer.Ordinal)
                .ToList(),
            Tiles = dataJson.Tiles
                .Where(item => !string.IsNullOrWhiteSpace(item.name))
                .Select(BlockRowMapper.Map)
                .OrderBy(row => row.Name, StringComparer.Ordinal)
                .ToList(),
            RecipeItems = dataJson.Recipes
                .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
                .SelectMany(RecipeItemRowMapper.Map)
                .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
                .ToList(),
            RecipeResults = dataJson.Recipes
                .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
                .Select(RecipeResultRowMapper.Map)
                .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
                .ToList(),
            Recipes = dataJson.Recipes
                .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
                .Select(RecipeRowMapper.Map)
                .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
                .ToList(),
            Moodles = dataJson.Moodles
                .Where(m => !string.IsNullOrWhiteSpace(m.localeId))
                .Select(MoodleRowMapper.Map)
                .OrderBy(row => row.LocaleId, StringComparer.Ordinal)
                .ToList(),
            GameFields = GameFieldRowMapper.Map(dataJson.Fields).ToList(),
            BuildingEntities = dataJson.Buildings
                .Where(m => !string.IsNullOrWhiteSpace(m.id))
                .Select(BuildingEntityRowMapper.Map)
                .OrderBy(row => row.Id, StringComparer.Ordinal)
                .ToList(),
            BodyFields = BodyFieldRowMapper.Map().ToList()
        };
    }

    private static async Task<LocaleCatalog> LoadLocalesAsync(CliOptions options)
    {
        var catalog = await LocaleCatalog.LoadAsync(
            options.LocaleDir,
            options.LocalePath,
            options.DefaultLocale,
            options.LocaleTag);

        if (catalog.Locales.Count == 0)
            Console.WriteLine("Warning: no locale files found; infoboxes will fall back to item ids.");

        return catalog;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            CasualtiesMiner.Uploader - uploads dumped item data to a Bucket-enabled MediaWiki.

            Usage:
              uploader <mode> [options]

            Modes:
              schemas    Upload Bucket table definitions (Bucket:* pages).
              locales    Upload Module:Locale and Module:Locale/<lang>/* from game JSON files.
              bulk       Upload locales, Bucket modules, Module:*/data, and refresh Bucket (items, liquids, moodles).
              all        schemas, then bulk (locales + modules + Bucket data).

            Options:
              --api <url>              api.php endpoint (default: casualtiesunknown.miraheze.org)
              --user / --password      Bot credentials (or CU_WIKI_USER / CU_WIKI_PASSWORD env vars).
              --data <path>            Path to data.json (default: data.json).
              --locale-dir <path>      Folder with game locale JSON (only files with a "main" object).
              --locale <path>          Single locale file (e.g. ru-RU.json); if omitted, only EN is fetched remotely.
              --locale-tag <git-ref>   Git ref for orsoniks/scavgame-locale (default: v6.1; "6.1" → v6.1).
              GitHub overrides local only for the same file name (ru-RU.json).
              --default-locale <code>  Locale id = JSON stem (default: EN → EN.json).
              --request-delay-ms <n> Pause after each API call (default: 750).
              --dry-run                Preview changes without logging in or editing.
            """);
    }

    private sealed class CliOptions
    {
        public string ApiUrl { get; private init; } = "https://casualtiesunknown.miraheze.org/w/api.php";
        public string? User { get; private init; }
        public string? Password { get; private init; }
        public string DataPath { get; private init; } = "data.json";
        public string? LocaleDir { get; private init; }
        public string LocalePath { get; private init; } = "EN.json";
        public string DefaultLocale { get; private init; } = LocaleCatalog.DefaultLanguageCode;
        public string LocaleTag { get; private init; } = LocaleCatalog.DefaultRemoteTag;
        public TimeSpan RequestDelay { get; private init; } = TimeSpan.FromMilliseconds(750);
        public bool DryRun { get; private init; }

        public static CliOptions Parse(string[] args)
        {
            string? Get(string name)
            {
                var index = Array.IndexOf(args, name);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
            }

            var delayRaw = Get("--request-delay-ms");

            return new CliOptions
            {
                ApiUrl = Get("--api") ?? "https://casualtiesunknown.miraheze.org/w/api.php",
                User = Get("--user") ?? Environment.GetEnvironmentVariable("CU_WIKI_USER"),
                Password = Get("--password") ?? Environment.GetEnvironmentVariable("CU_WIKI_PASSWORD"),
                DataPath = Get("--data") ?? "data.json",
                LocaleDir = Get("--locale-dir"),
                LocalePath = Get("--locale") ?? "EN.json",
                DefaultLocale = Get("--default-locale") ?? LocaleCatalog.DefaultLanguageCode,
                LocaleTag = Get("--locale-tag") ?? LocaleCatalog.DefaultRemoteTag,
                RequestDelay = int.TryParse(delayRaw, out var delayMs) ? TimeSpan.FromMilliseconds(delayMs) : TimeSpan.FromMilliseconds(750),
                DryRun = args.Contains("--dry-run")
            };
        }
    }
}
