using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:GameField/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildGameFieldDataModule(IReadOnlyList<GameFieldRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");
            sb.Append("    game_field_id = ").Append(LuaFormat.String(row.GameFieldId)).AppendLine(",");
            sb.Append("    value = ").Append(LuaFormat.Num(double.Parse(row.Value, System.Globalization.CultureInfo.InvariantCulture))).AppendLine(",");
            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string BuildBodyFieldDataModule(IReadOnlyList<BodyFieldRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");
            sb.Append("    body_field_id = ").Append(LuaFormat.String(row.BodyFieldId)).AppendLine(",");
            sb.Append("    label = ").Append(LuaFormat.String(row.Label)).AppendLine(",");
            sb.Append("    kind = ").Append(LuaFormat.String(row.Kind)).AppendLine(",");
            AppendOptionalString(sb, "heal_speed_field_id", row.HealSpeedFieldId);
            AppendOptionalString(sb, "max_timer_field_id", row.MaxTimerFieldId);
            AppendOptionalString(sb, "intensity_scale_field_id", row.IntensityScaleFieldId);
            AppendOptionalString(sb, "splint_multiplier_field_id", row.SplintMultiplierFieldId);
            sb.AppendLine("  },");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendOptionalString(StringBuilder sb, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.Append("    ").Append(key).Append(" = ").Append(LuaFormat.String(value)).AppendLine(",");
        }
    }
}
