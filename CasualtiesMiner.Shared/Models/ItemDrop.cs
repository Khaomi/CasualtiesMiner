namespace CasualtiesMiner.Shared.Models;

public sealed partial class ItemDrop : IEquatable<ItemDrop>
{
    public bool Equals(ItemDrop? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return id == other.id &&
               Math.Abs(chance - other.chance) < 0.00001f &&
               Math.Abs(conditionMin - other.conditionMin) < 0.00001f &&
               Math.Abs(conditionMax - other.conditionMax) < 0.00001f;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ItemDrop);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(id, chance, conditionMin, conditionMax);
    }

    public static bool operator ==(ItemDrop? left, ItemDrop? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(ItemDrop? left, ItemDrop? right)
    {
        return !(left == right);
    }
}
