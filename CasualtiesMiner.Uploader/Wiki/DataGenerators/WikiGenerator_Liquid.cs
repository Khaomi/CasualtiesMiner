using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Liquid/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildLiquidDataModule(IReadOnlyList<LiquidRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateLiquidFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static IEnumerable<(string Key, string Value)> EnumerateLiquidFields(LiquidRow row)
    {
        yield return ("liquid_id", LuaFormat.String(row.LiquidId));
        yield return ("locale_name", LuaFormat.String(row.LocaleName));
        yield return ("color", LuaFormat.String(row.Color));
        yield return ("value_per_liter", LuaFormat.Num(row.ValuePerLiter));
        yield return ("injection_sickness", LuaFormat.Num(row.InjectionSickness));
        yield return ("health_usable", LuaFormat.Bool(row.HealthUsable));
        yield return ("injectable", LuaFormat.Bool(row.Injectable));
        yield return ("locale_from_item", LuaFormat.Bool(row.LocaleFromItem));
        yield return ("qualities", LuaList(row.Qualities));
    }
}
