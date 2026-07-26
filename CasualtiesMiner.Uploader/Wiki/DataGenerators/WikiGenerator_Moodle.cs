using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Moodle/data</c> as an array for Bucket bulk upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildMoodleDataModule(IReadOnlyList<MoodleRow> rows)
        => BuildTableDataModule(rows, EnumerateMoodleFields);

    private static IEnumerable<(string Key, string Value)> EnumerateMoodleFields(MoodleRow row)
    {
        yield return ("icon", LuaFormat.String(row.Icon));
        yield return ("icon_src_size", LuaFormat.Int(row.IconSrcSize));
        yield return ("locale_id", LuaFormat.String(row.LocaleId));

        if (!string.IsNullOrWhiteSpace(row.DescLocaleKey))
        {
            yield return ("desc_locale_key", LuaFormat.String(row.DescLocaleKey));
        }

        yield return ("precondition_for_moodle", LuaFormat.String(row.PreconditionForMoodle));

        if (!string.IsNullOrWhiteSpace(row.PreconditionDisplay))
        {
            yield return ("precondition_display", LuaFormat.String(row.PreconditionDisplay));
        }

        if (row.Intensity is int intensity)
        {
            yield return ("intensity", LuaFormat.Int(intensity));
        }

        if (!string.IsNullOrWhiteSpace(row.IntensityBodyFieldId))
        {
            yield return ("intensity_body_field_id", LuaFormat.String(row.IntensityBodyFieldId));
        }

        yield return ("critical", LuaFormat.Bool(row.Critical));

        if (!string.IsNullOrWhiteSpace(row.CriticalExpr))
        {
            yield return ("critical_expr", LuaFormat.String(row.CriticalExpr));
        }

        yield return ("chipped_only", LuaFormat.Bool(row.ChippedOnly));
    }
}
