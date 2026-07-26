using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Liquid/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildLiquidDataModule(IReadOnlyList<LiquidRow> rows)
        => BuildTableDataModule(rows, EnumerateLiquidFields);

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
