using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Building/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildBuildingDataModule(IReadOnlyList<BuildingEntityRow> rows)
        => BuildTableDataModule(rows, EnumerateBuildingFields);

    private static IEnumerable<(string Key, string Value)> EnumerateBuildingFields(BuildingEntityRow row)
    {
        yield return ("building_id", LuaFormat.String(row.Id));
        yield return ("sprite_name", LuaFormat.String(row.SpriteName));
        yield return ("items_drop_on_destroy", LuaList(row.ItemsDropOnDestroy));
        yield return ("health", LuaFormat.Num(row.Health));
        yield return ("require_ground", LuaFormat.Bool(row.RequireGround));
        yield return ("skip_description_set", LuaFormat.Bool(row.SkipDescriptionSet));
        yield return ("drop_chance_multiplier", LuaFormat.Num(row.DropChanceMultiplier));
        yield return ("guaranteed_drop_amount", LuaFormat.Num(row.GuaranteedDropAmount));
        yield return ("always_drop", LuaList(row.AlwaysDrop));
        yield return ("item_categories_to_add", LuaList(row.ItemCategoriesToAdd));
        yield return ("block_footstep_sound_id", LuaFormat.Num(row.BlockFootstepSoundId));
        yield return ("cant_hit", LuaFormat.Bool(row.CantHit));
        yield return ("animal", LuaFormat.Bool(row.Animal));
        yield return ("ignore_body_optimize", LuaFormat.Bool(row.IgnoreBodyOptimize));
        yield return ("metallic", LuaFormat.Bool(row.Metallic));
    }
}
