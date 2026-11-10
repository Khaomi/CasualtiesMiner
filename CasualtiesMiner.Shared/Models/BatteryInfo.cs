using System.Diagnostics.CodeAnalysis;

namespace CasualtiesMiner.Shared.Models;

public sealed partial class BatteryInfo : ItemInfo, IEquatable<BatteryInfo>
{
    [SetsRequiredMembers]
    public BatteryInfo()
    {
    }

    public bool Equals(BatteryInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return base.Equals(other) && Math.Abs(maxCharge - other.maxCharge) < 0.00001;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BatteryInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(base.GetHashCode());
        hash.Add(maxCharge);

        return hash.ToHashCode();
    }

    public static bool operator ==(BatteryInfo? left, BatteryInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(BatteryInfo? left, BatteryInfo? right)
    {
        return !(left == right);
    }
}