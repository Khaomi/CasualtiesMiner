using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using System.Text.Json;

namespace CasualtiesMiner.Cli;

public static class Program
{
    private static Command _commands = new CommandProvider()
    {
        ChildCommands =
        {
            ["upload"] = new UploadCommand(),
            ["extract"] = new ExtractCommand()
        }
    };
    
    public static async Task Main(string[] args)
    {
        if (!await _commands.RunAsync(args))
            Environment.Exit(1);
    }

    public abstract class Command
    {
        protected string[] Flags { get; set; } = [];
        protected string[] KeyValuePairs { get; set; } = [];
        protected string Usage { get; set; } = "";
        
        public abstract Task<bool> RunAsync(Memory<string> args);
        
        public string? Get(Memory<string> args, string name)
        {
            var index = args.Span.IndexOf(name);
            return index >= 0 && index + 1 < args.Length ? args.Span[index + 1] : null;
        }

        public bool ParseArgs(ReadOnlySpan<string> args,
            out List<string> positionalArgs,
            out Dictionary<string, string> keyValueArgs,
            out HashSet<string> flags)
        {
            positionalArgs = [];
            keyValueArgs = [];
            flags = [];

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    var cleanArg = args[i][2..];
                    if (Flags.Contains(cleanArg))
                    {
                        flags.Add(cleanArg);
                        continue;
                    }

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Expected argument for --{cleanArg}.");
                        return false;
                    }

                    keyValueArgs[cleanArg] = args[i + 1];
                    i++;
                }
                else
                {
                    positionalArgs.Add(args[i]);
                }
            }

            return true;
        }
    }
    
    public class CommandProvider : Command
    {
        public Dictionary<string, Command> ChildCommands { get;  } = [];

        public override async Task<bool> RunAsync(Memory<string> args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Expected subcommand argument. Available subcommands: {string.Join(", ", ChildCommands.Keys)}");
                return false;
            }

            if (ChildCommands.TryGetValue(args.Span[0], out var childProvider))
                return await childProvider.RunAsync(args[1..]);

            Console.WriteLine($"Unrecognized subcommand. Available subcommands: {string.Join(", ", ChildCommands.Keys)}");
            return false;
        }
    }
}
