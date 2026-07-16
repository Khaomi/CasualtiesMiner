using System.Globalization;
using System.Reflection;
using AngouriMath;
using TreeSitter;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static class MathParser
{
    private static readonly MethodInfo CreateVariableUncheckedMethod = typeof(Entity.Variable).GetMethod(
        "CreateVariableUnchecked",
        BindingFlags.Static | BindingFlags.NonPublic
    )!;

    public static Entity ToMath(Node node)
    {
        switch (node.Type)
        {
            case "identifier":
            case "member_access_expression":
            case "element_access_expression":
            case "string_literal":
            case "interpolated_string_expression":
                return CreateVar(MathName(node.Text));
            case "integer_literal":
                return MathS.Numbers.Create(int.Parse(node.Text, CultureInfo.InvariantCulture));
            case "real_literal":
                return MathS.Numbers.Create(float.Parse(node.Text.Replace("f", ""), CultureInfo.InvariantCulture));
            case "boolean_literal":
                return MathS.Boolean.Create(node.Text == "true");
            case "null_literal":
                return CreateVar("null");
            case "binary_expression":
            {
                var left = ToMath(node.GetChildForField("left")!);
                var right = ToMath(node.GetChildForField("right")!);
                var op = node.Children[1].Text;
                return op switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => left / right,
                    "==" => MathS.Equality(left, right),
                    "!=" => !MathS.Equality(left, right),
                    "&&" => left & right,
                    "||" => left | right,
                    "<" => left < right,
                    "<=" => left <= right,
                    ">" => left > right,
                    ">=" => left >= right,
                    _ => throw new Exception($"Unknown operator {op}")
                };
            }
            case "prefix_unary_expression":
            {
                var op = node.Children[0].Text;
                var value = ToMath(node.Children[1]);
                return op switch
                {
                    "!" => !value,
                    "-" => -value,
                    "+" => +value,
                    _ => throw new Exception($"Unknown unary operator {op}")
                };
            }
            case "invocation_expression":
                var func = node.NamedChildren[0].Text;
                var args = node.NamedChildren[1].NamedChildren.Select(x =>
                    ToMath(x.Children.Count > 1 ? FindIdentifier(x)! : x.Children[0]));
                return MathS.Apply(CreateVar(MathName(func)), args.ToArray());
            case "parenthesized_expression":
                return ToMath(node.NamedChildren[0]);
            case "conditional_expression":
                return MathS.Apply("if",
                    ToMath(node.GetChildForField("condition")!),
                    ToMath(node.GetChildForField("consequence")!),
                    ToMath(node.GetChildForField("alternative")!));
            case "cast_expression":
                return ToMath(node.GetChildForField("value")!);
            case "object_creation_expression":
            case "array_creation_expression":
                return CreateVar("<object>"); // TODO parse these
            case "lambda_expression":
                return CreateVar("<lambda>"); // TODO parse these
            default:
                throw new Exception($"Unknown identifier of type {node.Type}: {node.Text}");
        }
    }

    private static string MathName(string name)
    {
        return name; //.Replace('.', '_').Replace('(', '_').Replace(')', '_');
    }

    private static Node? FindIdentifier(Node node)
    {
        if (node.Type == "identifier")
        {
            return node;
        }

        return node.NamedChildren.Select(FindIdentifier).FirstOrDefault(x => x != null);
    }

    public static Entity.Variable CreateVar(string name)
    {
        return (Entity.Variable)CreateVariableUncheckedMethod.Invoke(null, [name])!;
    }
}