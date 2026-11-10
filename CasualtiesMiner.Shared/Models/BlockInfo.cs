namespace CasualtiesMiner.Shared.Models;

public sealed partial class BlockInfo : IEquatable<BlockInfo>
{
    public bool Equals(BlockInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Math.Abs(health - other.health) < 0.00001 &&
               hitsound == other.hitsound &&
               metallic == other.metallic &&
               name == other.name &&
               noVariation == other.noVariation &&
               sleep == other.sleep &&
               slippery == other.slippery &&
               stepsound == other.stepsound &&
               Math.Abs(toxicity - other.toxicity) < 0.00001;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BlockInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(health);
        hash.Add(hitsound);
        hash.Add(metallic);
        hash.Add(name);
        hash.Add(noVariation);
        hash.Add(sleep);
        hash.Add(slippery);
        hash.Add(stepsound);
        hash.Add(toxicity);
        return hash.ToHashCode();
    }

    public static bool operator ==(BlockInfo? left, BlockInfo? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(BlockInfo? left, BlockInfo? right)
    {
        return !(left == right);
    }
}