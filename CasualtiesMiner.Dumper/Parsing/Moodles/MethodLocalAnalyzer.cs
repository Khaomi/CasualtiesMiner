using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing.Moodles;

internal static class MethodLocalAnalyzer
{
    private const int MaxAssignmentLength = 32;

    public static IReadOnlyDictionary<int, string> Analyze(IList<Instruction> instructions)
    {
        var map = new Dictionary<int, string>(LimbAggregateLocalAnalyzer.Analyze(instructions));

        for (var i = 0; i < instructions.Count; i++)
        {
            if (!ILParserHelper.TryGetLocalStoreIndex(instructions[i], out var localIndex)
                || map.ContainsKey(localIndex))
            {
                continue;
            }

            if (TryParseAssignmentExpression(instructions, i, map, out var expression))
            {
                map[localIndex] = expression;
            }
        }

        return map;
    }

    private static bool TryParseAssignmentExpression(
        IList<Instruction> instructions,
        int stlocIndex,
        IReadOnlyDictionary<int, string> existingLocals,
        out string expression)
    {
        expression = "";

        var maxLength = Math.Min(MaxAssignmentLength, stlocIndex);
        for (var length = maxLength; length >= 1; length--)
        {
            var start = stlocIndex - length;
            var slice = new Instruction[length];
            for (var k = 0; k < length; k++)
            {
                slice[k] = instructions[start + k];
            }

            if (ILComplexExpressionParser.TryFormat(slice, existingLocals, out expression))
            {
                return true;
            }
        }

        return false;
    }
}
