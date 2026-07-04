using CasualtiesMiner.Shared;
using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader.Data.Mappers;

/// <summary>
/// Converts dumped <see cref="MoodleRow"/> instances into wiki-ready <see cref="MoodleRow"/>s.
/// </summary>
internal static class MoodleRowMapper
{
    public static MoodleRow Map(MoodleInfo moodle)
    {
        var (causeKind, causeField) = MoodleCauseClassifier.ClassifyIntensityExpr(moodle.intensityExpr);
        var preconditionDisplay = MoodleCauseFormatter.FormatPrecondition(moodle.preconditionForMoodle);

        return new MoodleRow
        {
            Icon = moodle.icon,
            LocaleId = moodle.localeId,
            DescLocaleKey = moodle.descLocaleKey,
            PreconditionForMoodle = moodle.preconditionForMoodle,
            PreconditionDisplay = preconditionDisplay,
            Intensity = moodle.intensity,
            IntensityBodyFieldId = causeKind == "timer" ? causeField : null,
            Critical = moodle.critical,
            CriticalExpr = moodle.criticalExpr,
            ChippedOnly = moodle.chippedOnly,
            IconSrcSize = MoodleIconSizes.GetSourceSize(moodle.icon),
        };
    }
}
