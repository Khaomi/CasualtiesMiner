namespace CasualtiesMiner.Shared.Models;

public sealed partial class LiquidType
{
    public string liquidId = "";
}

public sealed partial class Color : IEquatable<Color>
{
    public byte a;
    public byte b;
    public byte g;
    public byte r;

    public bool Equals(Color? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return a == other.a &&
               b == other.b &&
               g == other.g &&
               r == other.r;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Color);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public static bool operator ==(Color? left, Color? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Color? left, Color? right)
    {
        return !(left == right);
    }
}