using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing.Moodles;

/// <summary>
/// Decides whether a branch guard seen earlier in the method body applies to a
/// specific <c>AddMoodle</c> call site. Needed because frames are sliced at each
/// call and nested if/else conditions (e.g. horrifiedLevel) live in earlier frames.
/// </summary>
internal static class MoodleGuardScope
{
    public static bool AppliesTruthyBrfalse(
        IList<Instruction> instructions,
        int branchIndex,
        int callIndex)
    {
        if (callIndex <= branchIndex)
        {
            return false;
        }

        var branch = instructions[branchIndex];
        if (branch.Operand is not Instruction target)
        {
            return false;
        }

        var targetIndex = ILParserHelper.IndexOf(instructions, target);
        if (targetIndex < 0)
        {
            return false;
        }

        return callIndex < targetIndex;
    }

    public static bool AppliesCompareBranch(
        IList<Instruction> instructions,
        int branchIndex,
        int callIndex)
    {
        if (callIndex <= branchIndex)
        {
            return false;
        }

        var branch = instructions[branchIndex];
        if (branch.Operand is not Instruction target)
        {
            return false;
        }

        var targetIndex = ILParserHelper.IndexOf(instructions, target);
        if (targetIndex < 0)
        {
            return false;
        }

        // fall-through continues below, taken jumps forward.
        if (targetIndex > branchIndex)
        {
            // then-block / single if-body (also covers ble -> merge at target).
            if (callIndex > branchIndex && callIndex < targetIndex)
            {
                return true;
            }

            // else-block (bgt → else start at target, merge after then-block br).
            if (callIndex >= targetIndex)
            {
                var mergeIndex = FindThenBlockMerge(instructions, branchIndex, targetIndex);
                if (mergeIndex < 0)
                {
                    return false;
                }

                if (callIndex >= mergeIndex)
                {
                    return false;
                }

                // skip outer else's inverted guard when a nested if's
                // then-block already encloses the call (e.g. irradiated tiers).
                if (HasNestedThenEnclosingCall(instructions, targetIndex, callIndex))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        // backward branch (unusual): taken path is between target and branch.
        return callIndex >= targetIndex && callIndex < branchIndex;
    }

    private static int FindThenBlockMerge(
        IList<Instruction> instructions,
        int branchIndex,
        int takenTargetIndex)
    {
        for (var j = branchIndex + 1; j < takenTargetIndex; j++)
        {
            var ins = instructions[j];
            if (ins.OpCode.Code is not (Code.Br or Code.Br_S) || ins.Operand is not Instruction target)
            {
                continue;
            }

            var mergeIndex = ILParserHelper.IndexOf(instructions, target);
            if (mergeIndex > takenTargetIndex)
            {
                return mergeIndex;
            }
        }

        return -1;
    }

    private static bool HasNestedThenEnclosingCall(
        IList<Instruction> instructions,
        int regionStart,
        int callIndex)
    {
        for (var i = regionStart; i < callIndex; i++)
        {
            if (!ILParserHelper.IsConditionalBranch(instructions[i]))
            {
                continue;
            }

            if (instructions[i].Operand is not Instruction target)
            {
                continue;
            }

            var targetIndex = ILParserHelper.IndexOf(instructions, target);
            if (targetIndex <= i)
            {
                continue;
            }

            if (callIndex > i && callIndex < targetIndex)
            {
                return true;
            }
        }

        return false;
    }
}
