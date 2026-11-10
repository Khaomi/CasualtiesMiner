using System.Text.Json;
using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;

namespace CasualtiesMiner.Cli;

public class ExtractCommand : Program.Command
{
    public ExtractCommand()
    {
        Usage =
            """
            Usage:
              ./CasualtiesMiner.Cli extract [input-path] [output-path]

            Options:
              input-path   Path to Assembly-CSharp.dll or the game install path.
                           Defaults to "Assembly-CSharp.dll", i.e. the current directory.
              output-path  Path to write the extracted JSON data to. Defaults to "data.json".
            """;
    }
    
    public override async Task<bool> RunAsync(Memory<string> args)
    {
        if (args.Length > 2)
        {
            Console.WriteLine(Usage);
            return false;
        }
        
        var inputPath = args.Length > 0 ? args.Span[0] : "Assembly-CSharp.dll";
        var outputPath = args.Length > 1 ? args.Span[1] : "data.json";

        if (!await Dumper.Dumper.ExtractToFileAsync(inputPath, outputPath))
        {
            Console.WriteLine("Failed to extract game data.");
            Console.WriteLine(Usage);
            return false;
        }
        
        return true;
    }
}