using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:RecipeItem/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildRecipeItemDataModule(IReadOnlyList<RecipeItemRow> rows)
        => BuildTableDataModule(rows, EnumerateRecipeItemFields);

    private static IEnumerable<(string Key, string Value)> EnumerateRecipeItemFields(RecipeItemRow row)
    {
        yield return ("recipe_id", LuaFormat.String(row.RecipeId));
        yield return ("specific", LuaFormat.Bool(row.Specific));
        yield return ("specific_id", LuaFormat.String(row.SpecificId));
        yield return ("is_liquid", LuaFormat.Bool(row.IsLiquid));
        yield return ("quality", LuaList(row.Quality));
        yield return ("minimum_condition", LuaFormat.Num(row.MinimumCondition));
        yield return ("destroy_item", LuaFormat.Bool(row.DestroyItem));
        yield return ("ignored_id", LuaFormat.String(row.IgnoredId));
    }
}
