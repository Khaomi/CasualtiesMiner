namespace CasualtiesMiner.Shared.Models;

public sealed partial class LiquidType : IEquatable<LiquidType>
{
    public bool Equals(LiquidType? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (color != other.color ||
            healthUsable != other.healthUsable ||
            injectable != other.injectable ||
            Math.Abs(injectionSickness - other.injectionSickness) > 0.00001 ||
            localeFromItem != other.localeFromItem ||
            localeName != other.localeName ||
            Math.Abs(valuePerLiter - other.valuePerLiter) > 0.00001)
            return false;

        var onDrinkEqual = (onDrink == null && other.onDrink == null) ||
                           (onDrink != null && other.onDrink != null && onDrink.SequenceEqual(other.onDrink));

        var onHealthUseEqual = (onHealthUse == null && other.onHealthUse == null) ||
                               (onHealthUse != null && other.onHealthUse != null &&
                                onHealthUse.SequenceEqual(other.onHealthUse));

        var qualitiesEqual = (qualities == null && other.qualities == null) ||
                             (qualities != null && other.qualities != null && qualities.SequenceEqual(other.qualities));

        return onDrinkEqual && onHealthUseEqual && qualitiesEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LiquidType);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(color);
        hash.Add(healthUsable);
        hash.Add(injectable);
        hash.Add(injectionSickness);
        hash.Add(localeFromItem);
        hash.Add(localeName);
        hash.Add(valuePerLiter);
        hash.Add(GetSequenceHashCode(onDrink));
        hash.Add(GetSequenceHashCode(onHealthUse));
        hash.Add(GetSequenceHashCode(qualities));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(LiquidType? left, LiquidType? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(LiquidType? left, LiquidType? right)
    {
        return !(left == right);
    }
}