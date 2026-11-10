using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing.Moodles;

internal static class MoodleGuardParser
{
    public static IReadOnlyList<string> ParseGuards(
        IList<Instruction> instructions,
        int callIndex,
        int argsStartIndex,
        IReadOnlyDictionary<int, string> localPaths)
    {
        var guards = new List<string>();

        for (var i = 0; i < argsStartIndex;)
        {
            if (ILParserHelper.TryGetFieldAccessChainStart(instructions, i, out _)
                && TryParseShortCircuitAndGuard(instructions, i, callIndex, localPaths, out var andGuard, out var andNext))
            {
                guards.Add(andGuard);
                i = andNext;
                continue;
            }

            if (ILParserHelper.IsLocalLoad(instructions[i])
                && TryParseStackOrGuard(instructions, i, callIndex, localPaths, out var stackOrGuard, out var stackOrNext))
            {
                guards.Add(stackOrGuard);
                i = stackOrNext;
                continue;
            }

            var instruction = instructions[i];

            if (ILParserHelper.TryGetFieldAccessChainStart(instructions, i, out var chainStart)
                && ILParserHelper.TryParseFieldBooleanGuard(instructions, i, out var boolGuard, out var boolNext)
                && MoodleGuardScope.AppliesTruthyBrfalse(instructions, boolNext - 1, callIndex))
            {
                guards.Add(boolGuard);
                i = boolNext;
                continue;
            }

            if (ILParserHelper.TryGetFieldAccessChainStart(instructions, i, out chainStart)
                && TryParseScopedFieldCompare(instructions, i, callIndex, localPaths, out var guardExpr, out var compareNext))
            {
                guards.Add(guardExpr);
                i = compareNext;
                continue;
            }

            if (ILParserHelper.IsLocalLoad(instruction)
                && TryParseScopedLocalCompare(instructions, i, callIndex, instruction, localPaths, out var localGuard))
            {
                guards.Add(localGuard);
            }

            i++;
        }

        return guards;
    }

    private static bool TryParseScopedFieldCompare(
        IList<Instruction> instructions,
        int compareStartIndex,
        int callIndex,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = compareStartIndex + 1;

        if (!ILParserHelper.TryReadFieldChainAtIndex(instructions, compareStartIndex, out var path, out var rhsStartIndex))
        {
            return false;
        }

        if (!ILParserHelper.TryParseCompareRhsExpression(instructions, rhsStartIndex, localPaths, out var rhs, out var branchIndex))
        {
            return false;
        }

        if (!MoodleGuardScope.AppliesCompareBranch(instructions, branchIndex, callIndex))
        {
            return false;
        }

        var branch = instructions[branchIndex];
        var op = ILParserHelper.BranchOperatorForGuard(instructions, branch, callIndex);
        if (op.Length == 0)
        {
            return false;
        }

        expression = $"{path} {op} {rhs}";
        endExclusive = branchIndex + 1;

        return true;
    }

    private static bool TryParseScopedLocalCompare(
        IList<Instruction> instructions,
        int compareStartIndex,
        int callIndex,
        Instruction start,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression)
    {
        expression = "";

        var literalInsn = start.Next;
        if (literalInsn is null || !ILParserHelper.TryFormatLiteral(literalInsn, null, out var literal))
        {
            return false;
        }

        var branch = literalInsn.Next;
        if (branch is null)
        {
            return false;
        }

        var branchIndex = ILParserHelper.IndexOf(instructions, branch);
        if (branchIndex < 0 || !MoodleGuardScope.AppliesCompareBranch(instructions, branchIndex, callIndex))
        {
            return false;
        }

        var op = ILParserHelper.BranchOperatorForGuard(instructions, branch, callIndex);
        if (op.Length == 0)
        {
            return false;
        }

        expression = $"{ILParserHelper.FormatLocalName(start, localPaths)} {op} {literal}";

        return true;
    }

    private static bool TryParseShortCircuitAndGuard(
        IList<Instruction> instructions,
        int index,
        int callIndex,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = index + 1;

        if (!ILParserHelper.TryParseFieldBooleanGuard(instructions, index, out var left, out var firstBranchNext))
        {
            return false;
        }

        if (!ILParserHelper.TryParseFieldBooleanGuard(instructions, firstBranchNext, out var right, out endExclusive))
        {
            return false;
        }

        if (!MoodleGuardScope.AppliesTruthyBrfalse(instructions, firstBranchNext - 1, callIndex)
            || !MoodleGuardScope.AppliesTruthyBrfalse(instructions, endExclusive - 1, callIndex))
        {
            return false;
        }

        expression = $"{left} && {right}";

        return true;
    }

    // num > 25f || flag  →  ldloc; ldc; cgt; ldloc; or; brfalse
    private static bool TryParseStackOrGuard(
        IList<Instruction> instructions,
        int index,
        int callIndex,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = index + 1;

        if (!ILParserHelper.IsLocalLoad(instructions[index])
            || !ILParserHelper.TryFormatLiteral(instructions[index + 1], null, out var literal)
            || !ILParserHelper.IsFloatCompare(instructions[index + 2].OpCode.Code)
            || !ILParserHelper.IsLocalLoad(instructions[index + 3])
            || instructions[index + 4].OpCode.Code != Code.Or
            || instructions[index + 5].OpCode.Code is not (Code.Brfalse or Code.Brfalse_S)
            || index + 5 >= instructions.Count)
        {
            return false;
        }

        if (!MoodleGuardScope.AppliesTruthyBrfalse(instructions, index + 5, callIndex))
        {
            return false;
        }

        var leftOp = ILParserHelper.FloatCompareOperator(instructions[index + 2].OpCode.Code);
        if (leftOp.Length == 0)
        {
            return false;
        }

        var left = $"{ILParserHelper.FormatLocalName(instructions[index], localPaths)} {leftOp} {literal}";
        var right = ILParserHelper.FormatLocalName(instructions[index + 3], localPaths);
        expression = $"{left} || {right}";
        endExclusive = index + 6;

        return true;
    }
}
