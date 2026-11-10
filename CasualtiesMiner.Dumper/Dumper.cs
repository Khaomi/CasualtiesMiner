using System.Text.Json;
using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    private const string DllName = "Assembly-CSharp.dll";

    private readonly ModuleDefinition _module;

    public Dumper(ModuleDefinition module)
    {
        _module = module;
    }
    
    private static string? DeduceGameAssemblyPath(string inputPath)
    {
        var target = Path.GetFileName(inputPath);

        var assemblyPath = target switch
        {
            DllName => inputPath,
            "Managed" => Path.Combine(inputPath, DllName),
            "CasualtiesUnknown_Data" => Path.Combine(inputPath, "Managed", DllName),
            _ => Path.Combine(inputPath, "CasualtiesUnknown_Data", "Managed", DllName)
        };

        return Path.Exists(assemblyPath) ? assemblyPath : null;
    }
    
    public static async Task<bool> ExtractToFileAsync(string inputPath, string outputPath)
    {
        var dumpedData = await ExtractAsync(inputPath);

        if (dumpedData == null)
            return false;

        await File.WriteAllTextAsync(outputPath,
            JsonSerializer.Serialize(dumpedData, DumpedData.SerializationOptions));
        
        Console.WriteLine("Done.");
        return true;
    }
    
    public static async Task<DumpedData?> ExtractAsync(string path)
    {
        var assemblyPath = DeduceGameAssemblyPath(path);

        if (assemblyPath == null)
        {
            Console.WriteLine($"Could not find Assembly-CSharp.dll starting from {path}.");
            Console.WriteLine($"Pass the path to the game's assembly or its installation directory.");
            return null;
        }
        
        ModuleDefinition? module;
        try
        {
            module = ModuleDefinition.ReadModule(assemblyPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read module: {ex.Message}");
            return null;
        }

        if (module.Name != "Assembly-CSharp.dll")
        {
            Console.WriteLine("Invalid file! Expecting Assembly-CSharp!");
            return null;
        }
        
        Console.WriteLine($"Extracting data from {assemblyPath}");

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
        BuildingEntity[] buildings;

        using var assets = new AssetsParser(Path.GetDirectoryName(Path.GetDirectoryName(assemblyPath))!);

        // TODO: AssetsTools.NET thread safety?
        buildings = DumpBuildingEntities(assets);

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

        return new DumpedData
        {
            Items = items,
            Recipes = recipes,
            Liquids = liquids,
            Tiles = tiles,
            Moodles = moodles,
            Fields = fields ?? new GameFields(),
            Buildings = buildings,
        };
    }
}
