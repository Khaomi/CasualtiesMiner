using CasualtiesMiner.Uploader.Data;
using System.Text;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Building/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildBuildingDataModule(IReadOnlyList<BuildingEntityRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateBuildingFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<(string Key, string Value)> EnumerateBuildingFields(BuildingEntityRow row)
    {
        yield return ("building_id", LuaFormat.String(row.Id));
        yield return ("items_drop_on_destroy", LuaList(row.ItemsDropOnDestroy));
        yield return ("health", LuaFormat.Num(row.Health));
        yield return ("require_ground", LuaFormat.Bool(row.RequireGround));
        yield return ("skip_description_set", LuaFormat.Bool(row.SkipDescriptionSet));
        yield return ("drop_chance_multiplier", LuaFormat.Num(row.DropChanceMultiplier));
        yield return ("guaranteed_drop_amount", LuaFormat.Num(row.GuaranteedDropAmount));
        yield return ("always_drop", LuaList(row.AlwaysDrop));
        yield return ("item_categories_to_add", LuaList(row.ItemCategoriesToAdd));
        yield return ("block_footstep_sound_id", LuaFormat.Num(row.BlockFootstepSoundId));
        yield return ("cant_hit", LuaFormat.Bool(row.Metallic));
        yield return ("animal", LuaFormat.Bool(row.Animal));
        yield return ("ignore_body_optimize", LuaFormat.Bool(row.IgnoreBodyOptimize));
        yield return ("metallic", LuaFormat.Bool(row.Metallic));
    }
}
