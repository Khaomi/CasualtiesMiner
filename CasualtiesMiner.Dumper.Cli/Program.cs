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
        var fileName = args.Length > 0 ? args[0] : "Assembly-CSharp.dll";
        if (!File.Exists(fileName))
        {
            Console.WriteLine($"Can't find {fileName}.");

            return;
        }

        ModuleDefinition? module;
        try
        {
            module = ModuleDefinition.ReadModule(fileName);
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
        RecipeInfo[] recipes = [];
        LiquidType[] liquids = [];
        BlockInfo[] tiles = [];

        await dumper.FetchEnglishLocaleAsync();

        await Task.WhenAll(
            Task.Run(() => items = dumper.DumpItems(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => recipes = dumper.DumpRecipes(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => liquids = dumper.DumpLiquids(new CSharpDecompiler(fileName, decompilerSettings))),
            Task.Run(() => tiles = dumper.DumpTiles(new CSharpDecompiler(fileName, decompilerSettings)))
        );

        var dumpedData = new DumpedData
        {
            Items = items,
            Recipes = recipes,
            Liquids = liquids,
            Tiles = tiles
        };

        await File.WriteAllTextAsync("data.json",
            JsonSerializer.Serialize(dumpedData, DumperJsonOptions.CamelCaseOptions));
    }
}