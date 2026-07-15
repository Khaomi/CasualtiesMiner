using AngouriMath;
using TreeSitter;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static class DelegateParser
{
    private static readonly Language Language = new Language("c-sharp");

    public static void Parse(string[] lines)
    {
        using var parser = new Parser(Language);
        using var tree = parser.Parse(string.Join("\n", lines))!;
        Query query = new Query(Language, @"(local_function_statement body: (_) @body)");
        var body = query.Execute(tree.RootNode).Captures.First().Node;
        Entity expr = "x + sin(y)";
        Console.WriteLine(expr);
        Console.WriteLine($"Root node: {body.Text}");
    }
}