using System.Globalization;
using System.Text.RegularExpressions;
using CasualtiesMiner.Dumper.Parsing.Delegates.Operations;
using CasualtiesMiner.Shared.Models;
using static CasualtiesMiner.Shared.Models.NumericEffect.OperationType;

namespace CasualtiesMiner.Dumper.Parsing.Delegates;

internal static partial class EffectParser
{
    public static List<Effect> FromOperation(Operation operation)
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
                    "*=" => (Multiply, op.Value),
                    "/=" => (Multiply, 1 / op.Value),
                    "=" => (Set, op.Value),
                    _ => throw new Exception($"Unknown operator {op.Operator}")
                };

                return
                [
                    new NumericEffect
                    {
                        Field = op.Field,
                        Type = type,
                        Value = value,
                        Holder = holder,
                        Condition = op.Condition,
                        Timer = op.Timer
                    }
                ];
            }
            case MethodCallOperation op:
            {
                var holder = ParseRawHolder(op.Holder, op.Method);
                switch (holder)
                {
                    case Holder.Body when op.Method == "Drink":
                        return
                        [
                            new NumericEffect
                            {
                                Field = "thirst",
                                Holder = holder,
                                Value = op.Arguments[0],
                                Type = Add,
                                Condition = op.Condition,
                                Timer = op.Timer
                            }
                        ];
                    case Holder.Body when op.Method == "Eat":
                        return
                        [
                            new NumericEffect
                            {
                                Field = "hunger",
                                Holder = holder,
                                Value = op.Arguments[0],
                                Type = Add,
                                Condition = op.Condition,
                                Timer = op.Timer
                            },
                            new NumericEffect
                            {
                                Field = "weightOffset",
                                Holder = holder,
                                Value = op.Arguments[1],
                                Type = Add,
                                Condition = op.Condition,
                                Timer = op.Timer
                            }
                        ];
                    case var _ when op.Method == "SetDisinfect":
                        return
                        [
                            new NumericEffect
                            {
                                Field = "disinfect",
                                Holder = new Holder.Body(),
                                Value = op.Arguments[0],
                                Type = Set,
                                Condition = op.Condition,
                                Timer = op.Timer
                            }
                        ];
                }

                return
                [
                    new CallEffect
                    {
                        Method = op.Method,
                        Holder = holder,
                        Condition = op.Condition,
                        Timer = op.Timer,
                        Arguments = op.Arguments
                    }
                ];
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

        if (raw.Contains("Component<WaterContainerItem>()"))
        {
            return new Holder.WaterContainerItem();
        }

        if (raw.Contains("Component<Antidepressants>(body)"))
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