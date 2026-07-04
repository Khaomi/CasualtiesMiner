using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Generates <c>Module:Block/data</c> for bulk Bucket upload.
/// </summary>
internal static partial class WikiGenerator
{
    public static string BuildBlockDataModule(IReadOnlyList<BlockRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GeneratedHeader);
        sb.AppendLine("return {");

        foreach (var row in rows)
        {
            sb.AppendLine("  {");

            foreach (var (key, value) in EnumerateBlockFields(row))
            {
                sb.Append("    ").Append(key).Append(" = ").Append(value).AppendLine(",");
            }

            sb.AppendLine("  },");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static IEnumerable<(string Key, string Value)> EnumerateBlockFields(BlockRow row)
    {
        yield return ("name", LuaFormat.String(row.Name));
        yield return ("hitsound", LuaFormat.String(row.Hitsound));
        yield return ("stepsound", LuaFormat.String(row.Stepsound));
        yield return ("health", LuaFormat.Num(row.Health));
        yield return ("toxicity", LuaFormat.Num(row.Toxicity));
        yield return ("no_variation", LuaFormat.Bool(row.NoVariation));
        yield return ("metallic", LuaFormat.Bool(row.Metallic));
        yield return ("slippery", LuaFormat.Bool(row.Slippery));
        yield return ("sleep", LuaFormat.String(Enum.GetName(row.SleepQuality)));
    }
}
