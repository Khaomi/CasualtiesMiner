using CasualtiesMiner.Uploader.Data.Mappers;
using System.Text.RegularExpressions;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Classifies dumper <c>intensity_expr</c> strings into wiki-friendly cause kinds.
/// </summary>
internal static partial class MoodleCauseClassifier
{
    [GeneratedRegex(
        @"^Mathf\.RoundToInt\(\s*\(?\s*(?<field>body(?:\.\w+|\[\d+\])+)\s*\*\s*0[,.]03f\s*\)?\s*\)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TimerIntensityExpr();

    public static (string? Kind, string? Field) ClassifyIntensityExpr(string? expr)
    {
        if (string.IsNullOrWhiteSpace(expr))
        {
            return (null, null);
        }

        var match = TimerIntensityExpr().Match(expr.Trim());
        if (!match.Success)
        {
            return (null, null);
        }

        var field = match.Groups["field"].Value;
        if (!BodyFieldRowMapper.IsTimerField(field))
        {
            return (null, null);
        }

        return ("timer", field);
    }
}
