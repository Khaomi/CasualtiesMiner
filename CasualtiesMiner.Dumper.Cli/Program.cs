using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using System.Text.Json;

namespace CasualtiesMiner.Dumper.Cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var assemblyPath = args.Length > 0 ? args[0] : "Assembly-CSharp.dll";
        if (!File.Exists(assemblyPath))
        {
            Console.WriteLine($"Can't find {assemblyPath}.");
            return;
        }

        ModuleDefinition? module;
        try
        {
            module = ModuleDefinition.ReadModule(assemblyPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read module: {ex.Message}");
            return;
        }

        if (module.Name != "Assembly-CSharp.dll")
        {
            Console.WriteLine("Invalid file! Expecting Assembly-CSharp!");
            return;
        }

        var dumper = new Dumper(module);

        var decompilerSettings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            UsingDeclarations = false
        };

        ItemInfo[] items = [];
        Recipe[] recipes = [];
        LiquidType[] liquids = [];
        BlockInfo[] tiles = [];
        MoodleInfo[] moodles = [];
        GameFields? fields = null;
        BuildingEntity[] buildings = [];

        using var assets = new AssetsParser(Path.GetDirectoryName(Path.GetDirectoryName(assemblyPath))!);

        assets.LoadResources();

        // TODO: AssetsTools.NET thread safety?
        buildings = Dumper.DumpBuildingEntities(assets);

        await Task.WhenAll(
            Task.Run(() => fields = dumper.DumpGameFields()),
            Task.Run(() => items = dumper.DumpItems(new CSharpDecompiler(assemblyPath, decompilerSettings), assets)),
            Task.Run(() => recipes = dumper.DumpRecipes(new CSharpDecompiler(assemblyPath, decompilerSettings))),
            Task.Run(() => liquids = dumper.DumpLiquids(new CSharpDecompiler(assemblyPath, decompilerSettings))),
            Task.Run(() => tiles = dumper.DumpTiles(new CSharpDecompiler(assemblyPath, decompilerSettings))),
            Task.Run(() => moodles = dumper.DumpMoodles())
        );

        Console.WriteLine($"Dumped {items.Length} items.");
        Console.WriteLine($"Dumped {recipes.Length} recipes.");
        Console.WriteLine($"Dumped {liquids.Length} liquids.");
        Console.WriteLine($"Dumped {tiles.Length} tiles.");
        Console.WriteLine($"Dumped {moodles.Length} moodles.");
        Console.WriteLine($"Dumped {buildings.Length} buildings.");

        if (fields is not null)
        {
            Console.WriteLine(
                $"Dumped game fields: boneHealTimerMax={fields.BoneHealTimerMax}, "
                    + $"boneHealSpeed={fields.BoneHealSpeed}, intensityScale={fields.IntensityScale}.");
        }
        else
        {
            Console.WriteLine(
                $"Game fields were not dumped.");
        }

        var dumpedData = new DumpedData
        {
            Items = items,
            Recipes = recipes,
            Liquids = liquids,
            Tiles = tiles,
            Moodles = moodles,
            Fields = fields ?? new GameFields(),
            Buildings = buildings,
        };

        await File.WriteAllTextAsync("data.json",
            JsonSerializer.Serialize(dumpedData, DumpedData.SerializationOptions));
    }
}
