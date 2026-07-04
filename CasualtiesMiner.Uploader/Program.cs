using CasualtiesMiner.Uploader.Data;
using CasualtiesMiner.Uploader.Data.BucketRows;
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

        //basic stuff
        var itemRows = LoadItemRows(options);
        var liquidRows = LoadLiquidRows(options);
        var blockRows = LoadBlockRows(options);
        var moodleRows = LoadMoodleRows(options);
        var buildingEntityRows = LoadBuildingEntityRows(options);

        //recipe
        var recipeItemRows = LoadRecipeItemRows(options);
        var recipeResultRows = LoadRecipeResultRows(options);
        var recipeRows = LoadRecipeRows(options);

        //fields
        var gameFieldRows = LoadGameFields(options);
        var bodyFieldRows = BodyFieldRowMapper.Map();

        //locale
        var locales = await LoadLocalesAsync(options);

        Console.WriteLine($"Loaded {itemRows.Count} items from {options.DataPath}.");
        Console.WriteLine($"Loaded {liquidRows.Count} liquids from {options.DataPath}.");
        Console.WriteLine($"Loaded {moodleRows.Count} moodles from {options.DataPath}.");
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
            await UploadLocalesAsync(client,
                locales,
                itemRows,
                liquidRows,
                blockRows,
                moodleRows,
                buildingEntityRows,
                options);
        }

        if (mode is "bulk" or "all")
        {
            await UploadDisplayModulesAsync(client, options);
            await UploadBulkAsync(client,
                itemRows,
                liquidRows,
                blockRows,
                recipeItemRows,
                recipeResultRows,
                recipeRows,
                moodleRows,
                gameFieldRows,
                bodyFieldRows,
                buildingEntityRows,
                options);
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
        IReadOnlyList<ItemRow> itemRows,
        IReadOnlyList<LiquidRow> liquidRows,
        IReadOnlyList<BlockRow> blockRows,
        IReadOnlyList<MoodleRow> moodleRows,
        IReadOnlyList<BuildingEntityRow> buildingRows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading locale modules ==");

        var router = await client.EditAsync(
            WikiContent.LocaleModuleTitle,
            WikiContent.LocaleModule,
            "Update locale resolver",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.LocaleModuleTitle}: {router}");

        var wikiUiStatus = await client.EditAsync(
            WikiContent.WikiUiModuleTitle,
            LocaleWikiGenerator.BuildWikiUiModule(),
            "Update wiki UI labels",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.WikiUiModuleTitle}: {wikiUiStatus}");

        if (locales.Locales.Count == 0)
        {
            Console.WriteLine("  Warning: no locale files found; skipping Module:Locale/<lang>/*.");
            return;
        }

        var itemIds = itemRows.Select(r => r.ItemId).ToArray();
        var moodleItems = moodleRows.Select(r => r.LocaleId).ToArray();
        var blockItems = blockRows.Select(r => r.Name).ToArray();
        var buildingItems = buildingRows.Select(r => r.Id).ToArray();

        foreach (var locale in locales.Locales)
        {
            var itemsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "items");
            var itemsStatus = await client.EditAsync(
                itemsTitle,
                LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    itemIds.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Item))),
                $"Update {locale.Code} item strings",
                options.DryRun);
            Console.WriteLine($"  {itemsTitle}: {itemsStatus}");

            var liquidsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "liquids");
            var liquidsStatus = await client.EditAsync(
                liquidsTitle,
                LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    liquidRows.Select(LocaleModuleEntry.CreateFromLiquid)),
                $"Update {locale.Code} liquid strings",
                options.DryRun);
            Console.WriteLine($"  {liquidsTitle}: {liquidsStatus}");

            var blocksTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "blocks");
            var blocksStatus = await client.EditAsync(
                blocksTitle,
                LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    blockItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Block))),
                $"Update {locale.Code} tile strings",
                options.DryRun);
            Console.WriteLine($"  {blocksTitle}: {blocksStatus}");

            var moodlesTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "moodles");
            var moodlesStatus = await client.EditAsync(
                moodlesTitle,
                LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    moodleItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Moodle))),
                $"Update {locale.Code} moodle strings",
                options.DryRun);
            Console.WriteLine($"  {moodlesTitle}: {moodlesStatus}");

            var buildingsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "buildings");
            var buildingsStatus = await client.EditAsync(
                buildingsTitle,
                LocaleWikiGenerator.BuildLocaleModule(
                    locale,
                    buildingItems.Select(id => LocaleModuleEntry.Create(id, GameObjectType.Building))),
                $"Update {locale.Code} building strings",
                options.DryRun);
            Console.WriteLine($"  {buildingsTitle}: {buildingsStatus}");

            var uiTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "ui");
            var uiStatus = await client.EditAsync(
                uiTitle,
                LocaleWikiGenerator.BuildUiModule(locale),
                $"Update {locale.Code} UI strings",
                options.DryRun);
            Console.WriteLine($"  {uiTitle}: {uiStatus}");
        }
    }

    private static async Task UploadDisplayModulesAsync(MediaWikiClient client, CliOptions options)
    {
        Console.WriteLine("== Uploading display modules ==");

        var bucketItemModule = await client.EditAsync(
            WikiContent.ItemBucketModuleTitle,
            WikiContent.ItemBucketModule,
            "Update ItemBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemBucketModuleTitle}: {bucketItemModule}");

        var bucketLiquidModule = await client.EditAsync(
            WikiContent.LiquidBucketModuleTitle,
            WikiContent.LiquidBucketModule,
            "Update LiquidBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.LiquidBucketModuleTitle}: {bucketLiquidModule}");

        //var bucketBlockModule = await client.EditAsync(
        //    WikiContent.BlockBucketModuleTitle,
        //    WikiContent.BlockBucketModule,
        //    "Update BlockBucket reader",
        //    options.DryRun);
        //Console.WriteLine($"  {WikiContent.LiquidBucketModuleTitle}: {bucketLiquidModule}");

        var bucketRecipeModule = await client.EditAsync(
            WikiContent.RecipeBucketModuleTitle,
            WikiContent.RecipeBucketModule,
            "Update RecipeBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RecipeBucketModuleTitle}: {bucketRecipeModule}");

        var bucketMoodleModule = await client.EditAsync(
            WikiContent.MoodleBucketModuleTitle,
            WikiContent.MoodleBucketModule,
            "Update MoodleBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.MoodleBucketModuleTitle}: {bucketMoodleModule}");

        var bucketBuildingModule = await client.EditAsync(
            WikiContent.BuildingBucketModuleTitle,
            WikiContent.BuildingBucketModule,
            "Update BuildingBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.BuildingBucketModuleTitle}: {bucketBuildingModule}");
    }

    private static async Task UploadBulkAsync(
        MediaWikiClient client,
        IReadOnlyList<ItemRow> itemRows,
        IReadOnlyList<LiquidRow> liquidRows,
        IReadOnlyList<BlockRow> blockRows,
        IReadOnlyList<RecipeItemRow> recipeItemRows,
        IReadOnlyList<RecipeResultRow> recipeResultRows,
        IReadOnlyList<RecipeRow> recipeRows,
        IReadOnlyList<MoodleRow> moodleRows,
        IReadOnlyList<GameFieldRow> gameFieldRows,
        BodyFieldRow[] bodyFieldRows,
        IReadOnlyList<BuildingEntityRow> buildingRows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading bulk Bucket data ==");

        Console.WriteLine("== Items ==");
        var router = await client.EditAsync(
            WikiContent.RouterItemModuleTitle,
            WikiContent.RouterItemModule,
            "Update item data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterItemModuleTitle}: {router}");

        var data = await client.EditAsync(
            WikiContent.ItemDataModuleTitle,
            WikiGenerator.BuildItemDataModule(itemRows),
            "Regenerate item data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemDataModuleTitle}: {data}");

        Console.WriteLine("== Liquids ==");
        router = await client.EditAsync(
            WikiContent.RouterLiquidModuleTitle,
            WikiContent.RouterLiquidModule,
            "Update liquid data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterLiquidModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.LiquidDataModuleTitle,
            WikiGenerator.BuildLiquidDataModule(liquidRows),
            "Regenerate liquid data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.LiquidDataModuleTitle}: {data}");

        Console.WriteLine("== Tiles ==");
        router = await client.EditAsync(
            WikiContent.RouterBlockModuleTitle,
            WikiContent.RouterBlockModule,
            "Update block data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterBlockModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.BlockDataModuleTitle,
            WikiGenerator.BuildBlockDataModule(blockRows),
            "Regenerate block data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.BlockDataModuleTitle}: {data}");

        Console.WriteLine("== Moodles ==");
        router = await client.EditAsync(
            WikiContent.RouterMoodleModuleTitle,
            WikiContent.RouterMoodleModule,
            "Update moodle data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterMoodleModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.MoodleDataModuleTitle,
            WikiGenerator.BuildMoodleDataModule(moodleRows),
            "Regenerate moodle data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.MoodleDataModuleTitle}: {data}");
        
        Console.WriteLine("== Buildings ==");
        router = await client.EditAsync(
            WikiContent.RouterBuildingModuleTitle,
            WikiContent.RouterBuildingModule,
            "Update building data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterBuildingModuleTitle}: {router}");
        
        data = await client.EditAsync(
            WikiContent.BuildingDataModuleTitle,
            WikiGenerator.BuildBuildingDataModule(buildingRows),
            "Regenerate building data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.BuildingDataModuleTitle}: {data}");
        
        Console.WriteLine("== Recipes ==");
        router = await client.EditAsync(
            WikiContent.RouterRecipeItemModuleTitle,
            WikiContent.RouterRecipeItemModule,
            "Update recipe items data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterRecipeItemModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.RecipeItemDataModuleTitle,
            WikiGenerator.BuildRecipeItemDataModule(recipeItemRows),
            "Regenerate recipe item data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RecipeItemDataModuleTitle}: {data}");

        router = await client.EditAsync(
            WikiContent.RouterRecipeResultModuleTitle,
            WikiContent.RouterRecipeResultModule,
            "Update recipe result data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterRecipeResultModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.RecipeResultDataModuleTitle,
            WikiGenerator.BuildRecipeResultDataModule(recipeResultRows),
            "Regenerate recipe result data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RecipeResultDataModuleTitle}: {data}");

        router = await client.EditAsync(
            WikiContent.RouterRecipeModuleTitle,
            WikiContent.RouterRecipeModule,
            "Update recipe data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterRecipeModuleTitle}: {router}");

        router = await client.EditAsync(
            WikiContent.RecipeDataModuleTitle,
            WikiGenerator.BuildRecipeDataModule(recipeRows),
            "Regenerate recipe data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RecipeDataModuleTitle}: {router}");

        Console.WriteLine("== Game fields ==");
        router = await client.EditAsync(
            WikiContent.RouterGameFieldModuleTitle,
            WikiContent.RouterGameFieldModule,
            "Update game field data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterGameFieldModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.GameFieldDataModuleTitle,
            WikiGenerator.BuildGameFieldDataModule(gameFieldRows),
            "Regenerate game field data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.GameFieldDataModuleTitle}: {data}");

        Console.WriteLine("== Body fields ==");
        router = await client.EditAsync(
            WikiContent.RouterBodyFieldModuleTitle,
            WikiContent.RouterBodyFieldModule,
            "Update body field data router",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterBodyFieldModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.BodyFieldDataModuleTitle,
            WikiGenerator.BuildBodyFieldDataModule(bodyFieldRows),
            "Regenerate body field data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.BodyFieldDataModuleTitle}: {data}");

        Console.WriteLine("== Triggers ==");
        var itemTrigger = await client.EditAsync(
            WikiContent.TriggerItemPageTitle,
            WikiContent.TriggerItemPage,
            "Refresh Bucket item data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerItemPageTitle}: {itemTrigger}");

        var liquidTrigger = await client.EditAsync(
            WikiContent.TriggerLiquidPageTitle,
            WikiContent.TriggerLiquidPage,
            "Refresh Bucket liquid data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerLiquidPageTitle}: {liquidTrigger}");

        var blockTrigger = await client.EditAsync(
            WikiContent.TriggerBlockPageTitle,
            WikiContent.TriggerBlockPage,
            "Refresh Bucket block data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerBlockPageTitle}: {blockTrigger}");

        var recipeTrigger = await client.EditAsync(
            WikiContent.TriggerRecipePageTitle,
            WikiContent.TriggerRecipePage,
            "Refresh Bucket recipe data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerRecipePageTitle}: {recipeTrigger}");

        var recipeItemTrigger = await client.EditAsync(
            WikiContent.TriggerRecipeItemPageTitle,
            WikiContent.TriggerRecipeItemPage,
            "Refresh Bucket recipe item data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerRecipeItemPageTitle}: {recipeItemTrigger}");

        var recipeResultTrigger = await client.EditAsync(
            WikiContent.TriggerRecipeResultPageTitle,
            WikiContent.TriggerRecipeResultPage,
            "Refresh Bucket recipe result data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerRecipeResultPageTitle}: {recipeResultTrigger}");

        var moodleTrigger = await client.EditAsync(
            WikiContent.TriggerMoodlePageTitle,
            WikiContent.TriggerMoodlePage,
            "Refresh Bucket moodle data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerMoodlePageTitle}: {moodleTrigger}");

        var buildingTrigger = await client.EditAsync(
            WikiContent.TriggerBuildingPageTitle,
            WikiContent.TriggerBuildingPage,
            "Refresh Bucket building data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerBuildingPageTitle}: {buildingTrigger}");

        var gameFieldTrigger = await client.EditAsync(
            WikiContent.TriggerGameFieldPageTitle,
            WikiContent.TriggerGameFieldPage,
            "Refresh Bucket game field data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerGameFieldPageTitle}: {gameFieldTrigger}");

        var bodyFieldTrigger = await client.EditAsync(
            WikiContent.TriggerBodyFieldPageTitle,
            WikiContent.TriggerBodyFieldPage,
            "Refresh Bucket body field data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerBodyFieldPageTitle}: {bodyFieldTrigger}");
    }

    private static IReadOnlyList<ItemRow> LoadItemRows(CliOptions options)
    {
        var items = DataJson.LoadItems(options.DataPath);

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.fullName))
            .Select(ItemRowMapper.Map)
            .OrderBy(row => row.ItemId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<LiquidRow> LoadLiquidRows(CliOptions options)
    {
        var liquids = DataJson.LoadLiquids(options.DataPath);

        return liquids
            .Where(item => !string.IsNullOrWhiteSpace(item.liquidId) || !string.IsNullOrWhiteSpace(item.localeName))
            .Select(LiquidRowMapper.Map)
            .OrderBy(row => row.LiquidId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<BlockRow> LoadBlockRows(CliOptions options)
    {
        var blocks = DataJson.LoadBlocks(options.DataPath);

        return blocks
            .Where(item => !string.IsNullOrWhiteSpace(item.name))
            .Select(BlockRowMapper.Map)
            .OrderBy(row => row.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<RecipeItemRow> LoadRecipeItemRows(CliOptions options)
    {
        var recipes = DataJson.LoadRecipes(options.DataPath);

        return recipes
            .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
            .SelectMany(RecipeItemRowMapper.Map)
            .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<RecipeResultRow> LoadRecipeResultRows(CliOptions options)
    {
        var recipes = DataJson.LoadRecipes(options.DataPath);

        return recipes
            .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
            .Select(RecipeResultRowMapper.Map)
            .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<RecipeRow> LoadRecipeRows(CliOptions options)
    {
        var recipes = DataJson.LoadRecipes(options.DataPath);

        return recipes
            .Where(m => !string.IsNullOrWhiteSpace(m.result.id))
            .Select(RecipeRowMapper.Map)
            .OrderBy(row => row.RecipeId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<MoodleRow> LoadMoodleRows(CliOptions options)
    {
        var moodles = DataJson.LoadMoodles(options.DataPath);

        return moodles
            .Where(m => !string.IsNullOrWhiteSpace(m.localeId))
            .Select(MoodleRowMapper.Map)
            .OrderBy(row => row.LocaleId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<GameFieldRow> LoadGameFields(CliOptions options)
    {
        var gameFields = DataJson.LoadFields(options.DataPath);

        return GameFieldRowMapper.Map(gameFields);
    }

    private static IReadOnlyList<BuildingEntityRow> LoadBuildingEntityRows(CliOptions options)
    {
        var buildings = DataJson.LoadBuildings(options.DataPath);

        return buildings
            .Where(m => !string.IsNullOrWhiteSpace(m.id))
            .Select(BuildingEntityRowMapper.Map)
            .OrderBy(row => row.Id, StringComparer.Ordinal)
            .ToList();
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
