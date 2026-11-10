using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:RecipeResult/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildRecipeResultDataModule(IReadOnlyList<RecipeResultRow> rows)
        => BuildTableDataModule(rows, EnumerateRecipeResultFields);

    private static IEnumerable<(string Key, string Value)> EnumerateRecipeResultFields(RecipeResultRow row)
    {
        yield return ("recipe_id", LuaFormat.String(row.RecipeId));
        yield return ("amount", LuaFormat.Int(row.Amount));
        yield return ("dont_drain_result_liquid", LuaFormat.Bool(row.DontDrainResultLiquid));
        yield return ("id", LuaFormat.String(row.Id));
        yield return ("is_liquid", LuaFormat.Bool(row.IsLiquid));
        yield return ("result_condition", LuaFormat.Num(row.ResultCondition));
    }
}
