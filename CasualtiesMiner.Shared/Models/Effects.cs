using System.Text.Json.Serialization;
using AngouriMath;
using CasualtiesMiner.Shared.Json;

namespace CasualtiesMiner.Shared.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NumericEffect), "numeric")]
[JsonDerivedType(typeof(CallEffect), "call")]
public abstract class Effect
{
    [JsonIgnore]
    public string? Key { get; set; }

    [JsonConverter(typeof(EntityJsonConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Entity? Condition { get; set; }

    [JsonConverter(typeof(EntityJsonConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Entity? Timer { get; set; }
}

public class NumericEffect : Effect
{
    public required string Field { get; set; }
    public required Holder Holder { get; set; }

    [JsonConverter(typeof(EntityJsonConverter))]
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

public class CallEffect : Effect
{
    public required string Method { get; set; }
    public required Holder Holder { get; set; }
}