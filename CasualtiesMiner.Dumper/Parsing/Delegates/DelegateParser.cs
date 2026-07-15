using System.Collections.Immutable;
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
        ParseBlock(body);
    }

    private static void ParseBlock(Node block, Dictionary<Entity, Entity>? variables = null)
    {
        variables ??= [];
        foreach (var decl in block.NamedChildren.Where(x => x.Type == "local_declaration_statement"))
        {
            var parsed = CapturesToDictionary(new Query(Language,
                """
                (local_declaration_statement 
                                  (variable_declaration type: (predefined_type) @type (variable_declarator name: (identifier) @name (_) @content)))
                """).Execute(decl).Captures);
            if (parsed.Count == 0)
            {
                AddWarning($"Failed to parse assignment {decl.Text}");
                continue;
            }

            var type = parsed["type"];
            if (type.Text == "float" || type.Text == "bool")
            {
                variables[parsed["name"].Text] = MathParser.ToMath(parsed["content"]);
            }
        }
    }

    private static void AddWarning(string warning)
    {
    }

    private static Dictionary<string, Node> CapturesToDictionary(IEnumerable<QueryCapture> captures)
    {
        return captures.ToDictionary(x => x.Name, x => x.Node);
    }
}