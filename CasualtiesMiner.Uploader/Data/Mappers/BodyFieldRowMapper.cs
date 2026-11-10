using CasualtiesMiner.Uploader.Data.BucketRows;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader.Data.Mappers;

internal static class BodyFieldRowMapper
{
    public static BodyFieldRow[] Map() =>
    [
        Timer("body.limbs.max.boneHealTimer",
            GameFieldIds.BoneHealSpeed, GameFieldIds.BoneSplintMultiplier),
        Timer("body.limbs[0].boneHealTimer",
            GameFieldIds.BoneHealSpeed, GameFieldIds.BoneSplintMultiplier),
        Timer("body.limbs[1].boneHealTimer",
            GameFieldIds.BoneHealSpeed, GameFieldIds.BoneSplintMultiplier),
        Timer("body.limbs.max.dislocationTimer",
            GameFieldIds.DislocationHealSpeed, GameFieldIds.DislocationSplintMultiplier),
        Timer("body.limbs[0].dislocationTimer",
            GameFieldIds.DislocationHealSpeed, GameFieldIds.DislocationSplintMultiplier),
        Timer("body.limbs[1].dislocationTimer",
            GameFieldIds.DislocationHealSpeed, GameFieldIds.DislocationSplintMultiplier),
    ];

    public static bool IsTimerField(string bodyFieldId) =>
        Map().Any(row => row.BodyFieldId == bodyFieldId && row.Kind == "timer");

    private static BodyFieldRow Timer(
        string bodyFieldId,
        string healSpeedFieldId,
        string splintMultiplierFieldId) =>
        new()
        {
            BodyFieldId = bodyFieldId,
            Label = WikiUiLabels.BodyFields.TryGetValue(bodyFieldId, out var label) ? label : bodyFieldId,
            Kind = "timer",
            HealSpeedFieldId = healSpeedFieldId,
            MaxTimerFieldId = GameFieldIds.BoneHealTimerMax,
            IntensityScaleFieldId = GameFieldIds.IntensityScale,
            SplintMultiplierFieldId = splintMultiplierFieldId,
        };
}
