using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:RecipeResult/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildRecipeResultDataModule(IReadOnlyList<RecipeResultRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateRecipeResultFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

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
