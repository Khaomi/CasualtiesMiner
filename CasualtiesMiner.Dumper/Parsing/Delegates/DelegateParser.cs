using System.Reflection;
using System.Runtime.InteropServices;
using AngouriMath;
using CasualtiesMiner.Dumper.Parsing.Delegates.Operations;
using TreeSitter;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static class DelegateParser
{
    private static readonly Language Language = new Language("c-sharp");
    private static readonly Dictionary<string, Query> cachedQueries = [];

    private static readonly PropertyInfo queryCursorSelf = typeof(QueryCursor).GetProperty(
        "Self",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    [DllImport("tree-sitter", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ts_query_cursor_set_max_start_depth(IntPtr cursor, uint max_start_depth);

    public static List<Operation> Parse(string[] lines)
    {
        using var parser = new Parser(Language);
        using var tree = parser.Parse(string.Join("\n", lines))!;
        Query query = new Query(Language, @"(local_function_statement body: (_) @body)");
        var body = query.Execute(tree.RootNode).Captures.First().Node;
        return ParseBlock(body);
    }

    private static List<Operation> ParseBlock(Node block, Dictionary<Entity, Entity>? variables = null)
    {
        List<Operation> operations = [];
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
            if (type.Text is "float" or "bool")
            {
                variables[parsed["name"].Text] = MathParser.ToMath(parsed["content"]).Substitute(variables);
            }
        }

        foreach (var exp in block.NamedChildren.Where(x => x.Type == "expression_statement"))
        {
            operations.AddRange(ParseExpression(exp, variables));
        }

        List<Operation> processIf(Node exp)
        {
            List<Operation> result = [];
            var ifStatement = QueryRoot(exp,
                """
                    (if_statement 
                    condition: (_) @condition consequence: (_) @consequence alternative: (_)? @alternative)
                """);
            if (ifStatement.Count == 0)
            {
                return [];
            }

            var cond = ifStatement["condition"];
            var cons = ifStatement["consequence"];
            var mathCond = MathParser.ToMath(cond).Substitute(variables);
            var consOperations = ParseBlock(cons, variables);
            foreach (var op in consOperations)
            {
                if (op.Condition == null)
                {
                    op.Condition = mathCond;
                }
                else
                {
                    op.Condition &= mathCond;
                }
            }

            result.AddRange(consOperations);

            if (ifStatement.TryGetValue("alternative", out var alt))
            {
                var elseOperations = alt.Type switch
                {
                    "block" => ParseBlock(alt, variables),
                    "if_statement" => processIf(alt),
                    _ => []
                };

                foreach (var op in elseOperations)
                {
                    if (op.Condition == null)
                    {
                        op.Condition = !mathCond;
                    }
                    else
                    {
                        op.Condition = !mathCond & op.Condition;
                    }
                }

                result.AddRange(elseOperations);
            }

            return result;
        }

        foreach (var exp in block.NamedChildren.Where(x => x.Type == "if_statement"))
        {
            operations.AddRange(processIf(exp));
        }

        return operations;
    }

    private static List<Operation> ParseExpression(Node exp, Dictionary<Entity, Entity> variables)
    {
        List<Operation> operations = [];
        var assignment = QueryRoot(exp,
            """
            (expression_statement
               (assignment_expression 
            	 left: (_) @left
            	 _ @operator
            	 right: (_) @right))
            """);
        if (assignment.Count != 0)
        {
            var left = assignment["left"];
            if (left.Text == "_")
            {
                return [];
            }

            var right = assignment["right"];
            var holder = left.NamedChildren.Count >= 2 ? left.NamedChildren[0].Text : null;
            var field = left.NamedChildren.Count >= 2 ? left.NamedChildren[1].Text : left.Text;
            operations.Add(new AssignmentOperation
            {
                Holder = holder,
                Field = field,
                Operator = assignment["operator"].Text,
                Value = MathParser.ToMath(right).Substitute(variables)
            });
        }

        var methodCall = QueryRoot(exp,
            """
            (expression_statement 
                (invocation_expression 
            	    function: (_) @method
            	    arguments: (argument_list) @args))
            """);
        if (methodCall.Count != 0)
        {
            var holder = methodCall["method"].NamedChildren.Count > 0
                ? methodCall["method"].NamedChildren[0].Text
                : null;
            var method = methodCall["method"].NamedChildren.Count > 0
                ? methodCall["method"].NamedChildren[1].Text
                : methodCall["method"].Text;
            // todo check class methods
            var args = methodCall["args"].NamedChildren.Select(x => x.Children[0]).ToArray();
            if (holder == "CoUtils.instance" && method == "DoTimedOp")
            {
                var callback = args[1];
                var duration = MathParser.ToMath(args[2]).Substitute(variables);
                var timerOperations = ParseBlock(callback.NamedChildren[0], variables);
                foreach (var op in timerOperations)
                {
                    op.Timer = duration;
                }

                operations.AddRange(timerOperations);
                return operations;
            }

            operations.Add(new MethodCallOperation
            {
                Holder = holder,
                Method = method,
                Arguments = args.Select(x => MathParser.ToMath(x).Substitute(variables)).ToList()
            });
        }

        return operations;
    }

    private static void AddWarning(string warning)
    {
    }

    private static Dictionary<string, Node> CapturesToDictionary(IEnumerable<QueryCapture> captures)
    {
        return captures.ToDictionary(x => x.Name, x => x.Node);
    }

    private static Dictionary<string, Node> QueryRoot(Node node, string queryString)
    {
        var query = cachedQueries.GetValueOrDefault(queryString) ?? new Query(Language, queryString);
        using var cursor = new QueryCursor();
        ts_query_cursor_set_max_start_depth((IntPtr)queryCursorSelf.GetValue(cursor)!, 0);
        cursor.Execute(query, node);
        return CapturesToDictionary(cursor.Captures);
    }
}