using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Recipe/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildRecipeDataModule(IReadOnlyList<RecipeRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateRecipeFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

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
