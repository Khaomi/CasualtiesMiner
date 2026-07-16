using System.Globalization;
using System.Text.RegularExpressions;
using CasualtiesMiner.Dumper.Parsing.Delegates.Operations;
using static CasualtiesMiner.Dumper.Parsing.Delegates.Operations.NumericEffect.OperationType;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static partial class EffectParser
{
    public static Effect FromOperation(Operation operation)
    {
        switch (operation)
        {
            case AssignmentOperation op:
            {
                var holder = ParseRawHolder(op.Holder, op.Field);
                var (type, value) = op.Operator switch
                {
                    "+=" => (Add, op.Value),
                    "-=" => (Add, -op.Value),
                    "*=" => (Multiply, -op.Value),
                    "/=" => (Multiply, 1 / op.Value),
                    "=" => (Set, op.Value),
                    _ => throw new Exception($"Unknown operator {op.Operator}")
                };

                return new NumericEffect
                {
                    Field = op.Field,
                    Type = type,
                    Value = value,
                    Holder = holder
                };
            }
            case MethodCallOperation op:
            {
                var holder = ParseRawHolder(op.Holder, op.Method);
                return new CallEffect
                {
                    Method = op.Method,
                    Holder = new Holder.None()
                };
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static Holder ParseRawHolder(string? raw, string field)
    {
        if (raw == null)
        {
            return new Holder.None();
        }

        if (raw is "body" or "limb.body" or "PlayerCamera.main.body")
        {
            return new Holder.Body();
        }

        if (raw == "limb")
        {
            return new Holder.Limb(-1);
        }

        if (raw == "item")
        {
            return new Holder.Item();
        }

        if (raw == "Sound")
        {
            return new Holder.Sound();
        }

        if (raw == "item.battery")
        {
            return new Holder.Battery();
        }
        if (raw == "body.vomiter")
        {
            return new Holder.Vomiter();
        }
        if (raw.Contains("Component<Talker>()") || raw == "body.talker")
        {
            return new Holder.Talker();
        }

        if (raw.Contains("Component<Painkillers>(body)") ||
            field is "antagonistAmount" or "opiateAmount" or "opiateTolerance")
        {
            return new Holder.Painkillers();
        }
        if (raw.Contains("Component<WaterContainerItem>()") )
        {
            return new Holder.WaterContainerItem();
        }
        if (raw.Contains("Component<Antidepressants>(body)") )
        {
            return new Holder.Antidepressants();
        }

        if (raw.Contains("Component<SleepingPills>(body)"))
        {
            return new Holder.SleepingPills();
        }

        var match = LimbRegex().Match(raw);
        if (match.Success)
        {
            return new Holder.Limb(int.Parse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture));
        }

        // Console.WriteLine($"Warning: Unknown holder {raw} for field {field}");
        return new Holder.Unknown(raw);
    }

    [GeneratedRegex(@"limbs\[(\d+)\]")]
    private static partial Regex LimbRegex();
}