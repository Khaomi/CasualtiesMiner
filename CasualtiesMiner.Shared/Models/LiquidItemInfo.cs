using System.Diagnostics.CodeAnalysis;

namespace CasualtiesMiner.Shared.Models;

public sealed partial class LiquidStack : IEquatable<LiquidStack>
{
    public bool Equals(LiquidStack? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Math.Abs(amount - other.amount) < 0.00001 && liquidId == other.liquidId;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LiquidStack);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(liquidId, amount);
    }

    public static bool operator ==(LiquidStack? left, LiquidStack? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(LiquidStack? left, LiquidStack? right)
    {
        return !(left == right);
    }
}

public sealed partial class LiquidItemInfo : ItemInfo, IEquatable<LiquidItemInfo>
{
    [SetsRequiredMembers]
    public LiquidItemInfo()
    {
    }

    public bool Equals(LiquidItemInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        var defaultContentsEqual = (defaultContents == null && other.defaultContents == null) ||
                                   (defaultContents != null && other.defaultContents != null &&
                                    defaultContents.SequenceEqual(other.defaultContents));

        return base.Equals(other) &&
               Math.Abs(capacity - other.capacity) < 0.00001 &&
               autoFill == other.autoFill &&
               defaultContentsEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LiquidItemInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(base.GetHashCode());
        hash.Add(capacity);
        hash.Add(autoFill);
        hash.Add(GetSequenceHashCode(defaultContents));

        return hash.ToHashCode();
    }

    private static int GetSequenceHashCode<T>(IEnumerable<T>? sequence)
    {
        if (sequence is null) return 0;

        var hash = new HashCode();
        foreach (var item in sequence) hash.Add(item);
        return hash.ToHashCode();
    }

    public static bool operator ==(LiquidItemInfo? left, LiquidItemInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(LiquidItemInfo? left, LiquidItemInfo? right)
    {
        return !(left == right);
    }
}