using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Recipe/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildRecipeDataModule(IReadOnlyList<RecipeRow> rows)
        => BuildTableDataModule(rows, EnumerateRecipeFields);

    private static IEnumerable<(string Key, string Value)> EnumerateRecipeFields(RecipeRow row)
    {
        yield return ("recipe_id", LuaFormat.String(row.RecipeId));
        yield return ("int", LuaFormat.Int(row.Intelligence));
        // yield return ("items_id", LuaFormat.String(row.Color));
        // yield return ("result_id", LuaFormat.Num(row.ValuePerLiter));
        yield return ("category", LuaFormat.String(row.Category));
        yield return ("is_repair", LuaFormat.Bool(row.IsRepair));
        yield return ("index", LuaFormat.Int(row.Index));
    }
}
