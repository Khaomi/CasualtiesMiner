using System.Text.Json.Serialization;

namespace CasualtiesMiner.Shared.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(None), "none")]
[JsonDerivedType(typeof(Item), "item")]
[JsonDerivedType(typeof(WaterContainerItem), "waterContainerItem")]
[JsonDerivedType(typeof(Body), "body")]
[JsonDerivedType(typeof(Vomiter), "vomiter")]
[JsonDerivedType(typeof(Talker), "talker")]
[JsonDerivedType(typeof(Antidepressants), "antidepressants")]
[JsonDerivedType(typeof(Painkillers), "painkillers")]
[JsonDerivedType(typeof(SleepingPills), "sleepingPills")]
[JsonDerivedType(typeof(Battery), "battery")]
[JsonDerivedType(typeof(Sound), "sound")]
[JsonDerivedType(typeof(Limb), "limb")]
[JsonDerivedType(typeof(Unknown), "unknown")]
public abstract record Holder
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