using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data.Mappers;

/// <summary>
/// Converts dumped <see cref="ItemInfo"/> instances into wiki-ready <see cref="ItemRow"/>s.
/// </summary>
internal static class ItemRowMapper
{
    /// <summary>
    /// All item categories used by the game (see <c>Item.SetupItems</c>).
    /// </summary>
    public static readonly IReadOnlyList<string> Categories =
    [
        "medical", "drug", "food", "water", "tool",
        "utility", "container", "trash", "custom", "unobtainable"
    ];

    public static ItemRow Map(ItemInfo item)
    {
        var id = item.fullName ?? string.Empty;
        var category = NormalizeCategory(item.category);

        var subtype = item switch
        {
            LiquidItemInfo => "liquid",
            BatteryInfo => "battery",
            _ => "base"
        };

        return new ItemRow
        {
            ItemId = id,
            Category = category,
            Subtype = subtype,
            Obtainable = category != "unobtainable",

            //we do this, bcause float number itself is not precise enough
            Weight = (double)(decimal)item.weight,
            Value = item.value,
            SlotRotation = (double)(decimal)item.slotRotation,

            Usable = item.usable,
            UsableOnLimb = item.usableOnLimb,
            UsableWithLmb = item.usableWithLMB,
            AutoAttack = item.autoAttack,
            OnlyHoldInHands = item.onlyHoldInHands,
            Combineable = item.combineable,
            DestroyAtZeroCondition = item.destroyAtZeroCondition,
            ScaleWeightWithCondition = item.scaleWeightWithCondition,
            IgnoreDepression = item.ignoreDepression,

            RotSpeed = (double)(decimal)item.rotSpeed,
            DecayMinutes = (double)(decimal)item.decayMinutes,
            DecayInfo = item.decayInfo,
            Rec = item.rec?.min ?? 0,

            Wearable = item.wearable,
            WearableCanBeHeld = item.wearableCanBeHeld,
            WearSlotId = item.wearSlotId ?? string.Empty,
            DesiredWearLimb = item.desiredWearLimb ?? string.Empty,
            WearableArmor = (double)(decimal)item.wearableArmor,
            WearableIsolation = (double)(decimal)item.wearableIsolation,
            WearableHitDurabilityLossMultiplier = (double)(decimal)item.wearableHitDurabilityLossMultiplier,
            JumpHeightMultChange = (double)(decimal)item.jumpHeightMultChange,
            WearableVisualOffset = item.wearableVisualOffset,

            Tags = ParseTags(item.tags),
            Qualities = MapQualities(item.qualities),

            Capacity = (item as LiquidItemInfo)?.capacity ?? 0,
            AutoFill = (item as LiquidItemInfo)?.autoFill ?? false,
            DefaultContents = MapLiquidStacks((item as LiquidItemInfo)?.defaultContents),

            MaxCharge = (item as BatteryInfo)?.maxCharge ?? 0
        };
    }

    private static string NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return "custom";
        }

        var normalized = category.Trim().ToLowerInvariant();

        return Categories.Contains(normalized) ? normalized : "custom";
    }

    private static string[] ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !tag.Contains("System.String", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] MapQualities(List<CraftingQuality>? qualities)
    {
        if (qualities is null || qualities.Count == 0) return [];

        return qualities
            .Where(q => !string.IsNullOrWhiteSpace(q.id))
            .Select(q => Math.Abs(q.amount - 1f) < 0.00001f ? q.id : $"{q.id}:{Format(q.amount)}")
            .ToArray();
    }

    private static string[] MapLiquidStacks(List<LiquidStack>? stacks)
    {
        if (stacks is null || stacks.Count == 0) return [];

        return stacks
            .Where(s => !string.IsNullOrWhiteSpace(s.liquidId))
            .Select(s => $"{s.liquidId}:{Format(s.amount)}")
            .ToArray();
    }

    private static string Format(float value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
