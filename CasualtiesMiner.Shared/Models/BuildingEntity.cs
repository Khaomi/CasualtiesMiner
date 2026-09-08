namespace CasualtiesMiner.Shared.Models;

public sealed partial class BuildingEntity : IEquatable<BuildingEntity>
{
    public string? localeId;
    public string? spriteName;

    public bool Equals(BuildingEntity? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (id != other.id ||
            fullName != other.fullName ||
            description != other.description ||
            requireGround != other.requireGround ||
            skipDescriptionSet != other.skipDescriptionSet ||
            guaranteedDropAmount != other.guaranteedDropAmount ||
            blockFootstepSoundId != other.blockFootstepSoundId ||
            cantHit != other.cantHit ||
            animal != other.animal ||
            ignoreBodyOptimize != other.ignoreBodyOptimize ||
            metallic != other.metallic ||
            spriteName != other.spriteName)
        {
            return false;
        }

        if (Math.Abs(health - other.health) >= 0.00001f ||
            Math.Abs(dropChanceMultiplier - other.dropChanceMultiplier) >= 0.00001f)
        {
            return false;
        }

        if (!SequenceEqual(itemsDropOnDestroy, other.itemsDropOnDestroy) ||
            !SequenceEqual(alwaysDrop, other.alwaysDrop) ||
            !SequenceEqual(itemCategoriesToAdd, other.itemCategoriesToAdd))
        {
            return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BuildingEntity);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(id);
        hash.Add(fullName);
        hash.Add(description);
        hash.Add(health);
        hash.Add(requireGround);
        hash.Add(skipDescriptionSet);
        hash.Add(dropChanceMultiplier);
        hash.Add(guaranteedDropAmount);
        hash.Add(GetSequenceHashCode(itemsDropOnDestroy));
        hash.Add(GetSequenceHashCode(alwaysDrop));
        hash.Add(GetSequenceHashCode(itemCategoriesToAdd));
        hash.Add(blockFootstepSoundId);
        hash.Add(cantHit);
        hash.Add(animal);
        hash.Add(ignoreBodyOptimize);
        hash.Add(metallic);
        hash.Add(spriteName);

        return hash.ToHashCode();
    }

    public static bool operator ==(BuildingEntity? left, BuildingEntity? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(BuildingEntity? left, BuildingEntity? right)
    {
        return !(left == right);
    }

    private static bool SequenceEqual<T>(T[]? left, T[]? right)
        where T : IEquatable<T>
    {
        if (left is null)
        {
            return right is null;
        }
        if (right is null)
        {
            return false;
        }

        return left.AsSpan().SequenceEqual(right);
    }

    private static int GetSequenceHashCode<T>(T[]? sequence)
        where T : IEquatable<T>
    {
        if (sequence is null)
        {
            return 0;
        }

        var hash = new HashCode();

        foreach (var item in sequence)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
