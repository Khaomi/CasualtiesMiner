using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text.RegularExpressions;

namespace CasualtiesMiner.Uploader.Wiki;

internal static partial class MoodleCauseFormatter
{
    public static string? FormatPrecondition(string? precondition)
    {
        if (string.IsNullOrWhiteSpace(precondition) || precondition.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var formatted = GuardClauseSplit().Split(precondition)
                                    .Select(static part => part.Trim())
                                    .Where(static part => part.Length > 0)
                                    .Select(FormatCommaSeparatedClause)
                                    .Where(clause => clause.Length > 0)
                                    .ToArray();

        return formatted.Length == 0 ? null : string.Join(", ", formatted);
    }

    private static string FormatCommaSeparatedClause(string clause)
    {
        var orParts = OrClauseSplit().Split(clause.Trim());
        var formatted = orParts.Select(FormatOrClausePart)
                               .Where(part => part.Length > 0)
                               .ToArray();

        return formatted.Length == 0 ? "" : string.Join(" OR ", formatted);
    }

    private static string FormatOrClausePart(string clause)
    {
        var andParts = AndClauseSplit().Split(clause.Trim());
        var formatted = andParts.Select(FormatClause)
                                .Where(part => part.Length > 0)
                                .ToArray();

        return formatted.Length == 0 ? "" : string.Join(" AND ", formatted);
    }

    public static string? FormatIntensity(
        string? causeKind,
        string? bodyFieldId,
        IReadOnlyDictionary<string, BodyFieldRow> bodyFields,
        IReadOnlyDictionary<string, double> gameFields)
    {
        if (causeKind != "timer" || string.IsNullOrWhiteSpace(bodyFieldId))
        {
            return null;
        }

        if (!bodyFields.TryGetValue(bodyFieldId, out var bodyField))
        {
            return null;
        }

        if (!TryResolveTimerConstants(bodyField, gameFields, out var healSpeed, out var intensityScale, out var splintMultiplier))
        {
            return null;
        }

        return FormatTimerIntensityBands(bodyField.Label, healSpeed, intensityScale, splintMultiplier);
    }

    private static string FormatClause(string clause)
    {
        clause = clause.Trim();
        if (clause.Length == 0)
        {
            return "";
        }

        var comparison = ComparisonExpr().Match(clause);
        if (comparison.Success)
        {
            var field = comparison.Groups[1].Value.Trim();
            var op = FormatComparisonOp(comparison.Groups[2].Value);
            var valueF = FormatThreshold(comparison.Groups[3].Value.Trim());
            var label = LocalizeField(field);
            var value = FormatValueType(valueF, field);

            return $"{label} {op} {value}";
        }

        var negated = NegatedBoolExpr().Match(clause);
        if (negated.Success)
        {
            return $"{LocalizeField(negated.Groups[1].Value.Trim())} == false";
        }

        var affirmative = AffirmativeBoolExpr().Match(clause);
        if (affirmative.Success)
        {
            return LocalizeField(affirmative.Groups[1].Value.Trim());
        }

        return LocalizeField(clause);
    }

    private static string LocalizeField(string field) =>
        WikiUiLabels.BodyFields.TryGetValue(field, out var localized) ? localized : field;

    private static string FormatValueType(string value, string key)
    {
        if (!WikiUiLabels.BodyConvertFields.TryGetValue(key, out var func)
            || !TryParseThresholdNumber(value, out _))
        {
            return value;
        }

        return func(value);
    }

    private static bool TryParseThresholdNumber(string value, out double number)
    {
        number = 0;

        if (value.Length == 0)
        {
            return false;
        }

        var normalized = FormatThreshold(value);
        return double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number);
    }

    private static bool TryResolveTimerConstants(
        BodyFieldRow bodyField,
        IReadOnlyDictionary<string, double> gameFields,
        out double healSpeed,
        out double intensityScale,
        out double splintMultiplier)
    {
        healSpeed = 0;
        intensityScale = 0;
        splintMultiplier = 0;

        return bodyField.HealSpeedFieldId is not null
               && bodyField.IntensityScaleFieldId is not null
               && bodyField.SplintMultiplierFieldId is not null
               && gameFields.TryGetValue(bodyField.HealSpeedFieldId, out healSpeed)
               && gameFields.TryGetValue(bodyField.IntensityScaleFieldId, out intensityScale)
               && gameFields.TryGetValue(bodyField.SplintMultiplierFieldId, out splintMultiplier);
    }

    private static string FormatTimerIntensityBands(
        string label,
        double healSpeed,
        double intensityScale,
        double splintMultiplier)
    {
        var thresholds = new double[4];
        for (var intensity = 3; intensity >= 1; intensity--)
        {
            thresholds[intensity] = (intensity - 0.5) / intensityScale;
        }

        var lines = new List<string>();
        for (var intensity = 3; intensity >= 0; intensity--)
        {
            string text;
            if (intensity == 3)
            {
                var seconds = thresholds[3] / healSpeed;
                var splintSeconds = thresholds[3] / (healSpeed * splintMultiplier);
                text = $"{label} > {FormatDuration(seconds)} (~{FormatDuration(splintSeconds)} with splint)";
            }
            else if (intensity == 0)
            {
                text = $"{label} ≤ {FormatDuration(thresholds[1] / healSpeed)}";
            }
            else
            {
                text = $"{label} {FormatDuration(thresholds[intensity] / healSpeed)}"
                       + $" – {FormatDuration(thresholds[intensity + 1] / healSpeed)}";
            }

            lines.Add($"Intensity {intensity}: {text}");
        }

        return string.Join("<br />", lines);
    }

    private static string FormatDuration(double seconds)
    {
        seconds = Math.Max(0, Math.Round(seconds));
        var mins = (int)(seconds / 60);
        var secs = (int)(seconds % 60);

        return mins > 0 && secs > 0
            ? $"{mins} min {secs} s"
            : mins > 0
                ? $"{mins} min"
                : $"{secs} s";
    }

    private static string FormatComparisonOp(string op) => op switch
    {
        ">=" => "≥",
        "<=" => "≤",
        _ => op,
    };

    private static string FormatThreshold(string value)
    {
        if (value.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^1];
        }

        return value.Replace(',', '.');
    }

    [GeneratedRegex(@"^(.+?)\s*(>=|<=|>|<)\s*(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ComparisonExpr();

    [GeneratedRegex(@"\s*\|\|\s*", RegexOptions.CultureInvariant)]
    private static partial Regex OrClauseSplit();

    [GeneratedRegex(@"\s*&&\s*", RegexOptions.CultureInvariant)]
    private static partial Regex AndClauseSplit();

    [GeneratedRegex(@"^(.+?)\s*==\s*false$", RegexOptions.CultureInvariant)]
    private static partial Regex NegatedBoolExpr();

    [GeneratedRegex(@"^(.+?)\s*==\s*true$", RegexOptions.CultureInvariant)]
    private static partial Regex AffirmativeBoolExpr();

    [GeneratedRegex(@",(?![0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex GuardClauseSplit();
}
