using AngouriMath;
using TreeSitter;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static class MathParser
{
    public static Entity ToMath(Node node)
    {
        switch (node.Type)
        {
            case "identifier":
                return MathS.Var(node.Text);
            case "integer_literal":
                return MathS.Numbers.Create(int.Parse(node.Text));
            case "real_literal":
                return MathS.Numbers.Create(float.Parse(node.Text.Replace("f", "")));
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
            default:
                throw new Exception($"Unknown identifier of type {node.Type}: {node.Text}");
        }
    }
}