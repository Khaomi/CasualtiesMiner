using System.Text.Json;
using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data;
using CasualtiesMiner.Uploader.Data.Enums;
using CasualtiesMiner.Uploader.Data.Locale;
using CasualtiesMiner.Uploader.Data.Mappers;
using CasualtiesMiner.Uploader.MediaWiki;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader;

public static class WikiUpload
{
    public record EditRequest(
        string Title,
        string Contents,
        string EditMessage,
        string? ForceUploadIfPageChanged = null,
        string? Group = null);

    public static async Task<bool> Upload(UploadOptions options)
    {
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
                return false;
            }

            Console.WriteLine($"Logging in to {options.ApiUrl} as {options.User} ...");
            await client.LoginAsync(options.User!, options.Password!);
            Console.WriteLine("Login successful.");
        }
        else
        {
            if (Directory.Exists(options.DryRunOutput))
                Directory.Delete(options.DryRunOutput, true);
            Directory.CreateDirectory(options.DryRunOutput);

            Console.WriteLine("Dry run: no edits will be performed.");
        }

        List<EditRequest> edits = [];

        if (options.Mode is "table_schema" or "all")
        {
            edits.AddRange(UploadSchemasAsync());
        }

        if (options.Mode is "locales" or "all")
        {
            edits.AddRange(UploadLocalesAsync(locales, dataRows));
        }

        if (options.Mode is "table_data" or "all")
        {
            edits.AddRange(UploadBulkAsync(dataRows));
        }

        if (edits.Count > 0)
        {
            Console.WriteLine($"Fetching page hashes ({edits.Count} total)...");
            await client.CachePageHashesAsync(edits.Select(x => x.Title));
        }

        Dictionary<string, string> statuses = [];

        foreach (var group in edits.GroupBy(x => x.Group))
        {
            Console.WriteLine($"== Uploading {group.Key ?? "misc"} pages ==");
            var preferredLogWidth = group.Max(x => x.Title.Length);
            foreach (var edit in group)
            {
                var forceUpload =
                    edit.ForceUploadIfPageChanged != null &&
                    statuses.TryGetValue(edit.ForceUploadIfPageChanged, out var dependentStatus) &&
                    dependentStatus != MediaWikiClient.NoChange;
                var status = statuses[edit.Title] = await client.EditAsync(
                    edit.Title,
                    edit.Contents,
                    edit.EditMessage,
                    options.DryRun ? options.DryRunOutput : null,
                    forceUpload: forceUpload);
                Console.WriteLine($"  {edit.Title.PadRight(preferredLogWidth, ' ')} {status}{(forceUpload ? " (force upload)" : "")}");
            }
        }

        return true;
    }

    private static IEnumerable<EditRequest> UploadSchemasAsync()
    {
        foreach (var (bucket, schema) in BucketSchemas.All())
        {
            var title = "Bucket:" + bucket;
            yield return new EditRequest(title, schema, "Update Bucket schema", Group: "Bucket schema");
        }
    }

    private static IEnumerable<EditRequest> UploadLocalesAsync(
        LocaleCatalog locales,
        DataRows dataRows)
    {
        if (locales.Locales.Count == 0)
        {
            Console.WriteLine("  Warning: no locale files found; skipping Module:Locale/<lang>/*.");
            return [];
        }

        var itemIds = dataRows.Items.Select(r => r.ItemId).ToArray();
        var moodleItems = dataRows.Moodles.Select(r => r.LocaleId).ToArray();
        var blockItems = dataRows.Tiles.Select(r => r.Name).ToArray();
        var buildingItems = dataRows.BuildingEntities.Select(r => r.LocaleId).Distinct().ToArray();

        var allItems = Enumerable.Empty<EditRequest>();

        foreach (var locale in locales.Locales)
        {
            allItems = allItems
                .Concat(UploadWikiLocale(locale, "items", LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    itemIds.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Item)))))
                .Concat(UploadWikiLocale(locale, "liquids", LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    dataRows.Liquids.Select(LocaleModuleEntry.CreateFromLiquid))))
                .Concat(UploadWikiLocale(locale, "blocks", LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    blockItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Block)))))
                .Concat(UploadWikiLocale(locale, "moodles", LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    moodleItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Moodle)))))
                .Concat(UploadWikiLocale(locale, "buildings", LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    buildingItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Building)))))
                .Concat(UploadWikiLocale(locale, "character/experiment",
                    LocaleWikiGenerator.BuildCharacterModule(locale, LocaleWikiGenerator.Character.Experiment)))
                .Concat(UploadWikiLocale(locale, "character/milky",
                    LocaleWikiGenerator.BuildCharacterModule(locale, LocaleWikiGenerator.Character.Milky)))
                .Concat(UploadWikiLocale(locale, "character/dune",
                    LocaleWikiGenerator.BuildCharacterModule(locale, LocaleWikiGenerator.Character.Dune)))
                .Concat(UploadWikiLocale(locale, "notes", LocaleWikiGenerator.BuildLoreModule(locale)))
                .Concat(UploadWikiLocale(locale, "pdaNotes",
                    LocaleWikiGenerator.BuildPdaModule(locale)))
                .Concat(UploadWikiLocale(locale, "pauseQuotes",
                    LocaleWikiGenerator.BuildPauseQuotesModule(locale)))
                .Concat(UploadWikiLocale(locale, "ui", LocaleWikiGenerator.BuildUiModule(locale)));
        }

        return allItems;
    }

    private static IEnumerable<EditRequest> UploadBulkAsync(
        DataRows dataRows)
    {
        List<(string ModuleName, string TargetBucket, string ModuleData)> wikiContents =
        [
            ("Item", "Item", WikiGenerator.BuildItemDataModule(dataRows.Items)),
            ("ItemBattery", "Item_battery", WikiGenerator.BuildItemBatteryDataModule(dataRows.Items)),
            ("ItemBatterySlot", "Item_battery_slot", WikiGenerator.BuildItemBatterySlotModule(dataRows.Items)),
            ("ItemLiquid", "Item_liquid", WikiGenerator.BuildItemLiquidDataModule(dataRows.Items)),
            ("ItemContainer", "Item_container", WikiGenerator.BuildItemContainerModule(dataRows.Items)),
            ("ItemGun", "Item_gun", WikiGenerator.BuildItemGunModule(dataRows.Items)),
            ("Liquid", "Liquid", WikiGenerator.BuildLiquidDataModule(dataRows.Liquids)),
            ("Block", "Block", WikiGenerator.BuildBlockDataModule(dataRows.Tiles)),
            ("Recipe", "Recipe", WikiGenerator.BuildRecipeDataModule(dataRows.Recipes)),
            ("RecipeItem", "Recipe_ingridient", WikiGenerator.BuildRecipeItemDataModule(dataRows.RecipeItems)),
            ("RecipeResult", "Recipe_result", WikiGenerator.BuildRecipeResultDataModule(dataRows.RecipeResults)),
            ("Moodle", "Moodle", WikiGenerator.BuildMoodleDataModule(dataRows.Moodles)),
            ("Building", "Building", WikiGenerator.BuildBuildingDataModule(dataRows.BuildingEntities)),
            ("GameField", "Gamefield", WikiGenerator.BuildGameFieldDataModule(dataRows.GameFields)),
            ("BodyField", "Bodyfield", WikiGenerator.BuildBodyFieldDataModule(dataRows.BodyFields)),
        ];

        foreach (var (moduleName, targetBucket, moduleData) in wikiContents)
        {
            foreach (var item in UploadWikiContent(moduleName, targetBucket, moduleData))
                yield return item;
        }
    }

    private static IEnumerable<EditRequest> UploadWikiLocale(
        GameLocale locale,
        string suffix,
        string localeModuleContents)
    {
        var uiTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, suffix);
        yield return new EditRequest(uiTitle, localeModuleContents, $"Update {locale.Code} strings for {suffix}", Group: "Locale");
    }

    private static IEnumerable<EditRequest> UploadWikiContent(
        string moduleBaseName,
        string targetBucket,
        string dataModuleContents)
    {
        var dataModuleTitle = WikiContent.MakeDataModuleTitle(moduleBaseName);
        var triggerPageTitle = WikiContent.MakeTriggerPageTitle(moduleBaseName);

        yield return new EditRequest(
            dataModuleTitle,
            dataModuleContents,
            $"Regenerate {moduleBaseName} module data", Group: "Bucket data");

        yield return new EditRequest(
            triggerPageTitle,
            WikiContent.MakeTriggerPage(dataModuleTitle, targetBucket),
            $"Refresh {moduleBaseName} Bucket data",
            ForceUploadIfPageChanged: dataModuleTitle, Group: "Bucket data");
    }

    private static async Task<DataRows> LoadDumpedData(UploadOptions options)
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

    private static async Task<LocaleCatalog> LoadLocalesAsync(UploadOptions options)
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

    public sealed class UploadOptions
    {
        public string Mode { get; init; } = "all";
        public string ApiUrl { get; init; } = "https://casualtiesunknown.miraheze.org/w/api.php";
        public string? User { get; init; }
        public string? Password { get; init; }
        public string DataPath { get; init; } = "data.json";
        public string? LocaleDir { get; init; }
        public string LocalePath { get; init; } = "EN.json";
        public string DefaultLocale { get; init; } = LocaleCatalog.DefaultLanguageCode;
        public string LocaleTag { get; init; } = LocaleCatalog.DefaultRemoteTag;
        public TimeSpan RequestDelay { get; init; } = TimeSpan.FromMilliseconds(750);
        public bool DryRun { get; init; }
        public string DryRunOutput { get; init; } = "dry-run-output";
    }
}