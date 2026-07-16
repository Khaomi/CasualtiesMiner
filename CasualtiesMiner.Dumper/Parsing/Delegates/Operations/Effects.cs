using AngouriMath;

namespace CasualtiesMiner.Dumper.Parsing.Delegates.Operations;

internal abstract class Effect
{
    public string? Key { get; set; }
}

internal class NumericEffect : Effect
{
    public required string Field { get; set; }
    public required Holder Holder { get; set; }

    public required Entity Value { get; set; }
    public required OperationType Type { get; set; }

    public enum OperationType
    {
        Add,
        Multiply,
        Set
    }

    // other is to the right
    public NumericEffect MergeWith(NumericEffect other)
    {
        switch (Type, other.Type)
        {
            case (OperationType.Add or OperationType.Set, OperationType.Add):
            {
                Value += other.Value;
                break;
            }
            case (OperationType.Multiply or OperationType.Set, OperationType.Multiply):
            {
                Value *= other.Value;
                break;
            }
            case (_, OperationType.Set):
            {
                Type = OperationType.Set;
                Value = other.Value;
                break;
            }
            case (OperationType.Add, OperationType.Multiply):
            {
                Type = OperationType.Set;
                Value = ("current" + Value) * other.Value;
                break;
            }
            case (OperationType.Multiply, OperationType.Add):
            {
                Type = OperationType.Set;
                Value = ("current" * Value) + other.Value;
                break;
            }
        }

        return this;
    }
}

internal class CallEffect : Effect
{
    public required string Method { get; set; }
    public required Holder Holder { get; set; }
}

internal abstract record Holder
{
    public sealed record None : Holder;
    public sealed record Item : Holder;
    public sealed record WaterContainerItem : Holder;
    public sealed record Body : Holder;
    public sealed record Vomiter : Holder;
    public sealed record Talker : Holder;
    public sealed record Antidepressants : Holder;
    public sealed record Painkillers : Holder;
    public sealed record SleepingPills : Holder;
    public sealed record Battery : Holder;
    public sealed record Sound : Holder;
    public sealed record Limb(int Index) : Holder;
    public sealed record Unknown(string holder) : Holder;
}