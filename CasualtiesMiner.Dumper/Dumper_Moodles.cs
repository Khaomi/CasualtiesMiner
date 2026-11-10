using CasualtiesMiner.Dumper.Parsing.Moodles;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public MoodleInfo[] DumpMoodles()
    {
        var moodleManager = _module.Types.FirstOrDefault(t => t.FullName == "MoodleManager");
        if (moodleManager is null)
            return [];

        var addAllMoodles = moodleManager.Methods.FirstOrDefault(m => m.Name == "AddAllMoodles");
        if (addAllMoodles?.Body is null)
            return [];

        //ILInstructionFormat.WriteMethodIl(Console.Out, addAllMoodles, markAddMoodleCalls: true);
        //Console.WriteLine();

        var localeType = _module.Types.FirstOrDefault(t => t.FullName == "Locale");
        var getMoodle = localeType?.Methods.FirstOrDefault(m => m is { Name: "GetMoodle", Parameters.Count: 1 });
        if (getMoodle is null)
            return [];

        var addMoodle = moodleManager.Methods.FirstOrDefault(m => m.Name == "AddMoodle");
        if (addMoodle is null)
        {
            return [];
        }

        var moodles = MoodleStackWalker.Walk(
            addAllMoodles.Body.Instructions,
            getMoodle);

        return [.. moodles.OrderBy(m => m.localeId, StringComparer.Ordinal)];
    }
}
