using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Item/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildItemDataModule(IReadOnlyList<ItemRow> rows)
        => BuildTableDataModule(rows, EnumerateItemFields);
    
    public static string BuildItemBatteryDataModule(IReadOnlyList<ItemRow> rows)
        => BuildTableDataModule(rows.Where(x => x.Subtype == "battery"), EnumerateItemBatteryFields);
    
    public static string BuildItemLiquidDataModule(IReadOnlyList<ItemRow> rows)
        => BuildTableDataModule(rows.Where(x => x.Subtype == "liquid"), EnumerateItemLiquidFields);

    private static IEnumerable<(string Key, string Value)> EnumerateItemFields(ItemRow row)
    {
        yield return ("item_id", LuaFormat.String(row.ItemId));
        yield return ("sprite_name", LuaFormat.String(row.SpriteName));
        yield return ("category", LuaFormat.String(row.Category));
        yield return ("subtype", LuaFormat.String(row.Subtype));
        yield return ("obtainable", LuaFormat.Bool(row.Obtainable));
        yield return ("weight", LuaFormat.Num(row.Weight));
        yield return ("value", LuaFormat.Int(row.Value));
        yield return ("slot_rotation", LuaFormat.Num(row.SlotRotation));
        yield return ("usable", LuaFormat.Bool(row.Usable));
        yield return ("usable_on_limb", LuaFormat.Bool(row.UsableOnLimb));
        yield return ("usable_with_lmb", LuaFormat.Bool(row.UsableWithLmb));
        yield return ("auto_attack", LuaFormat.Bool(row.AutoAttack));
        yield return ("only_hold_in_hands", LuaFormat.Bool(row.OnlyHoldInHands));
        yield return ("combineable", LuaFormat.Bool(row.Combineable));
        yield return ("destroy_at_zero_condition", LuaFormat.Bool(row.DestroyAtZeroCondition));
        yield return ("scale_weight_with_condition", LuaFormat.Bool(row.ScaleWeightWithCondition));
        yield return ("ignore_depression", LuaFormat.Bool(row.IgnoreDepression));
        yield return ("rot_speed", LuaFormat.Num(row.RotSpeed));
        yield return ("decay_minutes", LuaFormat.Num(row.DecayMinutes));
        yield return ("decay_info", LuaFormat.Int(row.DecayInfo));
        yield return ("rec", LuaFormat.Int(row.Rec));
        yield return ("wearable", LuaFormat.Bool(row.Wearable));
        yield return ("wearable_can_be_held", LuaFormat.Bool(row.WearableCanBeHeld));
        yield return ("wear_slot_id", LuaFormat.String(row.WearSlotId));
        yield return ("desired_wear_limb", LuaFormat.String(row.DesiredWearLimb));
        yield return ("wearable_armor", LuaFormat.Num(row.WearableArmor));
        yield return ("wearable_isolation", LuaFormat.Num(row.WearableIsolation));
        yield return ("wear_hit_dur_loss_mult", LuaFormat.Num(row.WearableHitDurabilityLossMultiplier));
        yield return ("jump_height_mult_change", LuaFormat.Num(row.JumpHeightMultChange));
        yield return ("wearable_visual_offset", LuaFormat.Int(row.WearableVisualOffset));
        yield return ("tags", LuaList(row.Tags));
        yield return ("qualities", LuaList(row.Qualities));

        if (row.Subtype == "liquid")
        {
            yield return ("capacity", LuaFormat.Num(row.Capacity));
            yield return ("auto_fill", LuaFormat.Bool(row.AutoFill));
            yield return ("default_contents", LuaList(row.DefaultContents));
        }

        if (row.Subtype == "battery")
        {
            yield return ("max_charge", LuaFormat.Num(row.MaxCharge));
        }
    }
    
    private static IEnumerable<(string Key, string Value)> EnumerateItemBatteryFields(ItemRow row)
    {
        yield return ("item_id", LuaFormat.String(row.ItemId));
        yield return ("max_charge", LuaFormat.Num(row.MaxCharge));
    }
    
    private static IEnumerable<(string Key, string Value)> EnumerateItemLiquidFields(ItemRow row)
    {
        yield return ("item_id", LuaFormat.String(row.ItemId));
        yield return ("capacity", LuaFormat.Num(row.Capacity));
        yield return ("auto_fill", LuaFormat.Bool(row.AutoFill));
        yield return ("default_contents", LuaList(row.DefaultContents));
    }
}
