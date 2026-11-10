using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data;

/// <summary>
/// Stable keys for <see cref="GameFieldRow"/> rows (match <c>GameFields</c> property names, lowercased).
/// </summary>
internal static class GameFieldIds
{
    public const string BoneHealTimerMax = "bonehealtimermax";
    public const string BoneHealSpeed = "bonehealspeed";
    public const string BoneSplintMultiplier = "bonesplintmultiplier";
    public const string DislocationHealSpeed = "dislocationhealspeed";
    public const string DislocationSplintMultiplier = "dislocationsplintmultiplier";
    public const string IntensityScale = "intensityscale";
}
