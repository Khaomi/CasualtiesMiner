using System.Globalization;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Wiki-only labels for game expression paths (<c>body.*</c>) shown in moodle cause columns.
/// </summary>
internal static class WikiUiLabels
{
    public static readonly IReadOnlyDictionary<string, string> BodyFields =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["body.averagePain"] = "Pain",
            ["body.badSleepAmount"] = "Bad sleep",
            ["body.bothEyesGone"] = "Both eyes have been ripped/missing",
            ["body.bloodOxygen"] = "Blood oxygen",
            ["body.bloodPressure"] = "Blood pressure",
            ["body.brainGrowSickness"] = "Brain regrowth sickness",
            ["body.brainHealth"] = "Brain integrity",
            ["body.caffeinated"] = "Caffeine",
            ["body.clawHealth"] = "Claw damage",
            ["body.clawRegrowTime"] = "Claw regrowth",
            ["body.conscious"] = "Conscious",
            ["body.consciousness"] = "Consciousness",
            ["body.curAdrenaline"] = "Adrenaline",
            ["body.dirtyness"] = "Dirtiness",
            ["body.disfigured"] = "Jaw has been broken/missing",
            ["body.energy"] = "Energy",
            ["body.eyeGone"] = "One eye has been ripped/missing",
            ["body.fibrillationProgress"] = "Fibrillation",
            ["body.focusedLevel"] = "Focus",
            ["body.harmer.timeWasStill"] = "Time stood still",
            ["body.hasPulmonaryEmbolism"] = "Pulmonary embolism is being active",
            ["body.heartRate"] = "Heart rate",
            ["body.hearingLoss"] = "Hearing loss",
            ["body.hemothorax"] = "Hemothorax",
            ["body.horrifiedLevel"] = "Horror",
            ["body.hunger"] = "Hunger",
            ["body.immunity"] = "Immunity",
            ["body.inCardiacArrest"] = "Cardiac arrest",
            ["body.internalBleeding"] = "Internal bleeding",
            ["body.lastStandTime"] = "Last stand",
            ["body.limbs.any.dismembered"] = "Any limb dismembered",
            ["body.limbs.any.showInfection"] = "Any limb infection visible",
            ["body.limbs.max.boneHealTimer"] = "Max bone heal time",
            ["body.limbs.max.dislocationTimer"] = "Max dislocation time",
            ["body.limbs.max.infectionAmount"] = "Max limb infection",
            ["body.limbs[0].boneHealTimer"] = "Bone heal (neck)",
            ["body.limbs[0].dislocationTimer"] = "Dislocation (jaw)",
            ["body.limbs[1].boneHealTimer"] = "Bone heal (ribs)",
            ["body.limbs[1].dislocationTimer"] = "Dislocation (spine)",
            ["body.mindWipe"] = "Mind wipe",
            ["body.mindWipe.active"] = "Mind wipe is active",
            ["body.onHardStimulants"] = "On hard stimulants",
            ["body.overEncumberance"] = "Encumbrance",
            ["body.overdoseIndex"] = "Drug overdose",
            ["body.painShock"] = "Pain shock",
            ["body.radiationSickness"] = "Radiation sickness",
            ["body.respiratoryRate"] = "Respiratory rate",
            ["body.septicShock"] = "Septic shock",
            ["body.sicknessAmount"] = "Sickness",
            ["body.stamina"] = "Stamina",
            ["body.strokeAmount"] = "Stroke",
            ["body.talker.impairedSpeech"] = "Conscious while item in mouth, dislocated jaw, disfigured or low brain integrity",
            ["body.temperature"] = "Temperature",
            ["body.thirst"] = "Hydration",
            ["body.totalBleedSpeed"] = "Bleeding",
            ["body.totalHappiness"] = "Happiness",
            ["body.traumaAmount"] = "Trauma",
            ["body.venomCurrent"] = "Venom",
            ["body.weightOffset"] = "Weight",
            ["body.wetness"] = "Wetness",
            ["WorldGeneration.unchipped"] = "Unchipped mode"
        };

    // do not include standard $"{str:0.0}" because it's a fallback in FormatValueType
    public static readonly IReadOnlyDictionary<string, Func<string, string>> BodyConvertFields =
        new Dictionary<string, Func<string, string>>
        {
            ["body.averagePain"] = str => $"{str:0}%",
            ["body.bloodOxygen"] = str => $"{str:0}%",
            ["body.bloodPressure"] = str => $"{(int)Math.Round(Convert.ToSingle(str))}/{(int)Math.Round(Convert.ToSingle(str) * 0.66f)}",
            ["body.consciousness"] = str => $"{str:0}%",
            ["body.curAdrenaline"] = str => $"{str:0}%",
            ["body.dirtyness"] = str => $"{str:0}%",
            ["body.energy"] = str => $"{str:0}%",
            ["body.fibrillationProgress"] = str => $"{str:0}%",
            ["body.hearingLoss"] = str => $"{str:0}%",
            ["body.immunity"] = str => $"{str:0}%",
            ["body.limbs.max.infectionAmount"] = str => $"{str:0}%",
            ["body.radiationSickness"] = str => $"{Convert.ToSingle(str) * 0.3f:0.0}gy",
            ["body.respiratoryRate"] = str => $"{(int)Math.Round(Convert.ToSingle(str) * 0.25f)}/m",
            ["body.septicShock"] = str => $"{str:0}%",
            ["body.sicknessAmount"] = str => $"{str:0}%",
            ["body.stamina"] = str => $"{str:0}%",
            ["body.temperature"] = str => FormatTemperature(str),
            ["body.weightOffset"] = str => $"{Convert.ToSingle(str) * 0.34f + 50f:0.#}kg",
            ["body.wetness"] = str => $"{str:0}%"
        };

    //lua stuff
    public static readonly IReadOnlyDictionary<string, string> Misc =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["is_chipped"] = "Requires chip: ",
            ["is_chipped_desc"] = "Whether the moodle is only visible when the chip is functional.",
            ["caused_by"] = "Caused by ",
            ["critical"] = "Critical",
            ["intensity_label"] = "Intensity",
            ["recipe.any_item_with"] = "Any item with ",
            ["recipe.any_liquid_with"] = "Any liquid with",
            ["recipe.any_item"] = "Any item",
            ["recipe.ingredients"] = "Ingredients",
            ["recipe.count"] = "(x%d)",
            ["recipe.countLiquid"] = "(%dmL)",
            ["recipe.condition_at_least"] = "At least %d%% condition",
            ["recipe.liquid_condition_at_least"] = "At least %s mL",
            ["recipe.liquid_quality"] = "Total (%s) %s quality",
            ["recipe.quality"] = "(%s) %s quality",
            ["recipe.volume"] = "Volume: %dmL",
            ["recipe.info"] = "Info",
            ["recipe.condition"] = "Condition: %d%%",
            ["recipe.amount"] = "Amount: %d",
            ["recipe.intRequired"] = "INT needed: %d",
        };

    //MOVE IT FROM HERE!
    public static float BloodToLitersBody(float amount)
    {
        return 2.5f + amount * 0.025f;
    }

    public static string FormatTemperature(string value)
    {
        var normalized = value.Replace(',', '.');
        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var celsius))
        {
            return value;
        }

        var rounded = (int)Math.Round(celsius * 10f) * 0.1f;
        return $"{rounded.ToString("0.#", CultureInfo.InvariantCulture)}\u00B0C";
    }
}
