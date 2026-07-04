using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Globalization;

namespace CasualtiesMiner.Uploader.Data.Mappers;

internal static class GameFieldRowMapper
{
    public static IReadOnlyDictionary<string, double> ToLookup(GameFields? item)
    {
        if (item is null)
        {
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }

        return new Dictionary<string, double>(StringComparer.Ordinal)
        {
            [GameFieldIds.BoneHealTimerMax] = item.BoneHealTimerMax,
            [GameFieldIds.BoneHealSpeed] = item.BoneHealSpeed,
            [GameFieldIds.BoneSplintMultiplier] = item.BoneSplintMultiplier,
            [GameFieldIds.DislocationHealSpeed] = item.DislocationHealSpeed,
            [GameFieldIds.DislocationSplintMultiplier] = item.DislocationSplintMultiplier,
            [GameFieldIds.IntensityScale] = item.IntensityScale,
        };
    }

    public static GameFieldRow[] Map(GameFields? item)
    {
        if (item is null)
        {
            return [];
        }

        return
        [
            Row(GameFieldIds.BoneHealTimerMax, item.BoneHealTimerMax),
            Row(GameFieldIds.BoneHealSpeed, item.BoneHealSpeed),
            Row(GameFieldIds.BoneSplintMultiplier, item.BoneSplintMultiplier),
            Row(GameFieldIds.DislocationHealSpeed, item.DislocationHealSpeed),
            Row(GameFieldIds.DislocationSplintMultiplier, item.DislocationSplintMultiplier),
            Row(GameFieldIds.IntensityScale, item.IntensityScale),
        ];
    }

    private static GameFieldRow Row(string id, double value) =>
        new()
        {
            GameFieldId = id,
            Value = value.ToString(CultureInfo.InvariantCulture),
        };
}
