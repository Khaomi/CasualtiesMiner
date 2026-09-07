namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// A flattened, wiki-ready representation of a single item. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
internal sealed record ItemRow
{
    public required string ItemId { get; init; }
    public required string SpriteName { get; init; }
    public required string Category { get; init; }

    /// <summary>
    /// One of <c>base</c>, <c>liquid</c>, <c>battery</c>.
    /// </summary>
    public required string Subtype { get; init; }

    public bool Obtainable { get; init; } = true;

    public double Weight { get; init; }
    public int Value { get; init; }
    public double SlotRotation { get; init; }

    public bool Usable { get; init; }
    public bool UsableOnLimb { get; init; }
    public bool UsableWithLmb { get; init; }
    public bool AutoAttack { get; init; }
    public bool OnlyHoldInHands { get; init; }
    public bool Combineable { get; init; }
    public bool DestroyAtZeroCondition { get; init; }
    public bool ScaleWeightWithCondition { get; init; }
    public bool IgnoreDepression { get; init; }

    public double RotSpeed { get; init; }
    public double DecayMinutes { get; init; }
    public int DecayInfo { get; init; }
    public int Rec { get; init; }

    public bool Wearable { get; init; }
    public bool WearableCanBeHeld { get; init; }
    public string WearSlotId { get; init; } = string.Empty;
    public string DesiredWearLimb { get; init; } = string.Empty;
    public double WearableArmor { get; init; }
    public double WearableIsolation { get; init; }
    public double WearableHitDurabilityLossMultiplier { get; init; }
    public double JumpHeightMultChange { get; init; }
    public int WearableVisualOffset { get; init; } = 5;

    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> Qualities { get; init; } = [];

    // LiquidItemInfo
    public double Capacity { get; init; }
    public bool AutoFill { get; init; }
    public IReadOnlyList<string> DefaultContents { get; init; } = [];

    // BatteryInfo
    public double MaxCharge { get; init; }
    
    // Container
    public bool IsContainer { get; init; }
    public double MaxWeight { get; init; }
    public double MaxWeightPerItem { get; init; }
    public double EncumberanceMult { get; init; }
    public bool ItemsVisible { get; init; }
    public IReadOnlyList<string> TagRestriction { get; init; } = [];
    
    // Gun script
    public bool IsGun { get; init; }
    public string AmmoType { get; init; } = string.Empty;
    public string FiringMode { get; init; } = string.Empty;
    public string FeedType { get; init; } = string.Empty;
    public int MagCapacity { get; init; }
    public double Knockback { get; init; }
    public double StructureDamage { get; init; }
    public double AnimalDamage { get; init; }
    public double Loudness { get; init; }
    public double DesiredGasTime { get; init; }
    public int ShotsPerFire { get; init; }
    public double VerticalSpread { get; init; }
    public double ConditionLossPerShot { get; init; }
}
