using CasualtiesMiner.Dumper.Parsing;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public GameFields? DumpGameFields()
    {
        var limbType = _module.Types.FirstOrDefault(t => t.FullName == "Limb");
        if (limbType is null)
        {
            return null;
        }

        var splintType = _module.Types.FirstOrDefault(t => t.FullName == "SplintLimb");
        var moodleManagerType = _module.Types.FirstOrDefault(t => t.FullName == "MoodleManager");

        var boneHealSpeed = ILGameFieldReader.ReadStaticFloatInitializer(limbType, "boneHealSpeed");
        var dislocationHealSpeed = ILGameFieldReader.ReadStaticFloatInitializer(limbType, "dislocationHealSpeed");
        var boneHealTimerMax = ILGameFieldReader.ReadInstanceFieldStore(limbType, "BreakBone", "boneHealTimer");

        // can be broken after next update
        var intensityScale = moodleManagerType is not null
            ? ILGameFieldReader.ReadMoodleIntensityScale(moodleManagerType)
            : null;

        var boneSplintExtra = splintType is not null
            ? ILGameFieldReader.ReadSplintBoneHealExtraMultiplier(splintType)
            : null;

        var boneSplintDivisor = ILGameFieldReader.ReadSplintHealDivisor(limbType, "get_injuryHealTime", 2.5f);
        var dislocationSplintDivisor = ILGameFieldReader.ReadSplintHealDivisor(limbType, "get_injuryHealTime", 2f);

        var boneSplintMultiplier = boneSplintDivisor
            ?? (boneSplintExtra is float extra ? 1f + extra : null);
        var dislocationSplintMultiplier = dislocationSplintDivisor ?? 2f;

        if (boneHealSpeed is null
            || dislocationHealSpeed is null
            || boneHealTimerMax is null
            || intensityScale is null
            || boneSplintMultiplier is null)
        {
            Console.WriteLine("[WARNING] DumpGameFields: incomplete Limb/Moodle constants.");
            return null;
        }

        return new GameFields
        {
            BoneHealSpeed = (double)(decimal)boneHealSpeed.Value,
            DislocationHealSpeed = (double)(decimal)dislocationHealSpeed.Value,
            BoneHealTimerMax = (double)(decimal)boneHealTimerMax.Value,
            IntensityScale = (double)(decimal)intensityScale.Value,
            BoneSplintMultiplier = (double)(decimal)boneSplintMultiplier.Value,
            DislocationSplintMultiplier = (double)(decimal)dislocationSplintMultiplier,
        };
    }
}
