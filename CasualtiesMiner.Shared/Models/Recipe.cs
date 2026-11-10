namespace CasualtiesMiner.Shared.Models;

public sealed partial class RecipeItem : IEquatable<RecipeItem>
{
    public bool Equals(RecipeItem? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return destroyItem == other.destroyItem &&
               ignoredId == other.ignoredId &&
               isLiquid == other.isLiquid &&
               Math.Abs(minimumCondition - other.minimumCondition) < 0.00001 &&
               quality == other.quality &&
               specific == other.specific &&
               specificId == other.specificId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RecipeItem);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(destroyItem, ignoredId, isLiquid, minimumCondition, quality, specific, specificId);
    }

    public static bool operator ==(RecipeItem? left, RecipeItem? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RecipeItem? left, RecipeItem? right)
    {
        return !(left == right);
    }
}

public sealed partial class RecipeResult : IEquatable<RecipeResult>
{
    public bool Equals(RecipeResult? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return amount == other.amount &&
               dontDrainResultLiquid == other.dontDrainResultLiquid &&
               id == other.id &&
               isLiquid == other.isLiquid &&
               Math.Abs(resultCondition - other.resultCondition) < 0.00001;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RecipeResult);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(amount, dontDrainResultLiquid, id, isLiquid, resultCondition);
    }

    public static bool operator ==(RecipeResult? left, RecipeResult? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RecipeResult? left, RecipeResult? right)
    {
        return !(left == right);
    }
}

public sealed partial class Recipe : IEquatable<Recipe>
{
    public bool Equals(Recipe? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (category != other.category ||
            // hasMadeBefore != other.hasMadeBefore ||
            index != other.index ||
            INT != other.INT ||
            isRepair != other.isRepair ||
            result != other.result ||
            specialKnown != other.specialKnown)
            return false;

        var itemsEqual = (items == null && other.items == null) ||
                         (items != null && other.items != null && items.SequenceEqual(other.items));

        return itemsEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Recipe);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(category);
        // hash.Add(hasMadeBefore);
        hash.Add(index);
        hash.Add(INT);
        hash.Add(isRepair);
        hash.Add(result);
        hash.Add(specialKnown);
        hash.Add(GetSequenceHashCode(items));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(Recipe? left, Recipe? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Recipe? left, Recipe? right)
    {
        return !(left == right);
    }
}