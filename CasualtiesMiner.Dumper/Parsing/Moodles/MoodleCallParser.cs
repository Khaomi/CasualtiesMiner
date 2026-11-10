using CasualtiesMiner.Shared.Models;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing.Moodles;

internal static class MoodleCallParser
{
    // code looks so sophisticated beacuse moodles can't be parsed like
    // fields in big lists where you have every field stacked on top of eachother.
    // Moreover, some fields being calculated as arguments in function
    // so it just adds more complexity to the whole thing
    public static MoodleInfo? Parse(
        Collection<Instruction> frame,
        int frameStartIndex,
        IList<Instruction> fullInstructions,
        MethodReference getMoodleMethod,
        IReadOnlyDictionary<int, string> localPaths)
    {
        var callIndexInFrame = frame.Count - 1;
        var globalCallIndex = frameStartIndex + callIndexInFrame;

        var argsStartIndex = FindAddMoodleArgsStartIndex(frame, callIndexInFrame);
        if (argsStartIndex < 0)
        {
            return null;
        }

        var globalArgsStartIndex = frameStartIndex + argsStartIndex;
        var guards = MoodleGuardParser.ParseGuards(fullInstructions, globalCallIndex, globalArgsStartIndex, localPaths).ToList();

        var cursor = argsStartIndex + 1;
        if (cursor >= callIndexInFrame)
        {
            return null;
        }

        // get ternary `intensity`, literal, or computed expression (RoundToInt, Clamp)
        if (!TryParseIntensity(frame, cursor, callIndexInFrame, localPaths, out var intensityText, out cursor))
        {
            return null;
        }

        int? intensity = null;
        string? intensityExpr = null;
        if (int.TryParse(intensityText, out var intensityValue))
        {
            intensity = intensityValue;
        }
        else
        {
            intensityExpr = LimbAggregateLocalAnalyzer.SubstituteLocals(intensityText, localPaths);
        }

        if (cursor >= callIndexInFrame || frame[cursor].OpCode.Code != Code.Ldstr)
        {
            return null;
        }

        // get `icon`
        var icon = (string)frame[cursor].Operand!;
        cursor++;

        // get `locale`
        if (!TryParseLocaleKey(frame, ref cursor, getMoodleMethod, callIndexInFrame, skipSuffix: false, out var localeId))
        {
            return null;
        }

        // get `locale desc` (may continue with .Replace(...), or pick between two keys with a ternary)
        if (!TryParseLocaleKey(frame, ref cursor, getMoodleMethod, callIndexInFrame, skipSuffix: true, out var descLocaleKey)
            && !TryParseLocaleKeyTernary(frame, ref cursor, getMoodleMethod, callIndexInFrame, localeId + "dsc", out descLocaleKey))
        {
            return null;
        }

        // get `critical` argument/expression
        var critical = false;
        string? criticalExpr = null;
        if (cursor < callIndexInFrame)
        {
            if (!TryParseBoolArg(frame, cursor, out critical, out criticalExpr, out cursor))
            {
                return null;
            }
        }

        var chippedOnly = false;
        if (cursor < callIndexInFrame && ILInstructionParser.IsLdcI4(frame[cursor]))
        {
            chippedOnly = ILInstructionParser.ParseInt(frame[cursor]) != 0;
        }

        //Console.WriteLine($"icon: {icon}");
        //Console.WriteLine($"localeId: {localeId}");
        //Console.WriteLine($"descLocaleKey: {descLocaleKey}");
        //Console.WriteLine($"preconditionForMoodle: {(string.IsNullOrEmpty(string.Join(", ", guards)) ? "none" : string.Join(", ", guards))}");
        //Console.WriteLine($"intensity: {intensityExpr ?? intensity.ToString()}");
        //Console.WriteLine($"critical: {criticalExpr ?? critical.ToString()}");
        //Console.WriteLine($"chippedOnly: {chippedOnly}\n");

        return new MoodleInfo
        {
            icon = icon,
            localeId = localeId,
            descLocaleKey = descLocaleKey,
            preconditionForMoodle = SubstituteGuards(string.Join(", ", guards), localPaths),
            intensity = intensity,
            intensityExpr = intensityExpr,
            critical = critical,
            criticalExpr = criticalExpr,
            chippedOnly = chippedOnly
        };
    }

    private static bool TryParseLocaleKey(
        Collection<Instruction> instructions,
        ref int cursor,
        MethodReference getMoodleMethod,
        int callIndex,
        bool skipSuffix,
        out string localeKey)
    {
        localeKey = "";

        if (cursor >= instructions.Count || instructions[cursor].OpCode.Code != Code.Ldstr)
        {
            return false;
        }

        localeKey = (string)instructions[cursor].Operand!;
        cursor++;

        if (cursor < instructions.Count
            && IsGetMoodleCall(instructions[cursor], getMoodleMethod))
        {
            cursor++;

            if (skipSuffix)
            {
                SkipDescExpressionRemainder(instructions, ref cursor, callIndex);
            }
        }

        return true;
    }

    // Handles `cond ? Locale.GetMoodle("a") : Locale.GetMoodle("b")`, as used by "FOCUSED" moodle
    private static bool TryParseLocaleKeyTernary(
        Collection<Instruction> instructions,
        ref int cursor,
        MethodReference getMoodleMethod,
        int callIndex,
        string preferredKey,
        out string localeKey)
    {
        localeKey = "";

        var branchIndex = -1;
        for (var i = cursor; i < callIndex; i++)
        {
            if (instructions[i].OpCode.Code == Code.Ldstr)
                return false;

            if (instructions[i].OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                branchIndex = i;
                break;
            }
        }

        if (branchIndex < 0 || instructions[branchIndex].Operand is not Instruction trueLabel)
            return false;

        var falseCursor = branchIndex + 1;
        if (!TryReadGetMoodleKey(instructions, ref falseCursor, getMoodleMethod, out var whenFalse)
            || falseCursor >= callIndex
            || instructions[falseCursor].OpCode.Code is not (Code.Br or Code.Br_S)
            || instructions[falseCursor].Operand is not Instruction mergeLabel)
            return false;

        var trueCursor = ILParserHelper.IndexOf(instructions, trueLabel);
        if (trueCursor < 0
            || !TryReadGetMoodleKey(instructions, ref trueCursor, getMoodleMethod, out var whenTrue)
            || trueCursor != ILParserHelper.IndexOf(instructions, mergeLabel))
            return false;

        localeKey = whenTrue == preferredKey ? whenTrue : whenFalse;
        cursor = trueCursor;
        return true;
    }

    private static bool TryReadGetMoodleKey(
        Collection<Instruction> instructions,
        ref int cursor,
        MethodReference getMoodleMethod,
        out string key)
    {
        key = "";

        if (cursor + 1 >= instructions.Count
            || instructions[cursor].OpCode.Code != Code.Ldstr
            || !IsGetMoodleCall(instructions[cursor + 1], getMoodleMethod))
            return false;

        key = (string)instructions[cursor].Operand!;
        cursor += 2;
        return true;
    }

    // skips IL after GetMoodle that builds the final desc string (like .Replace(...)).
    private static void SkipDescExpressionRemainder(
        Collection<Instruction> instructions,
        ref int cursor,
        int callIndex)
    {
        var end = FindIndexBeforeTrailingBoolArgs(instructions, callIndex);
        if (end > cursor)
        {
            cursor = end;
        }
    }

    private static int FindIndexBeforeTrailingBoolArgs(Collection<Instruction> instructions, int callIndex)
    {
        var idx = callIndex - 1;
        if (idx < 0)
        {
            return 0;
        }

        if (ILInstructionParser.IsLdcI4(instructions[idx]))
        {
            idx--;
        }

        if (idx < 0)
        {
            return 0;
        }

        if (ILInstructionParser.IsLdcI4(instructions[idx]))
        {
            return idx;
        }

        if (ILParserHelper.IsStackCompare(instructions[idx].OpCode.Code))
        {
            var compareIndex = idx;
            for (var i = compareIndex; i >= 0 && i >= compareIndex - 24; i--)
            {
                if (instructions[i].OpCode.Code != Code.Ldarg_0)
                {
                    continue;
                }

                if (TryParseFieldCompareBool(instructions, i, out _, out var endExclusive)
                    && endExclusive == compareIndex + 1)
                {
                    return i;
                }
            }
        }

        if (IsCriticalExprStart(instructions, idx))
        {
            ILInstructionParser.ConsumeOne(instructions, ref idx);
        }

        return idx + 1;
    }

    private static bool TryParseBoolArg(
        Collection<Instruction> instructions,
        int startIndex,
        out bool value,
        out string? expression,
        out int endExclusive)
    {
        value = false;
        expression = null;

        if (TryParseFieldCompareBool(instructions, startIndex, out var compareExpr, out endExclusive))
        {
            expression = compareExpr;

            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            value = ILInstructionParser.ParseInt(instructions[startIndex]) != 0;
            endExclusive = startIndex + 1;

            return true;
        }

        return false;
    }

    private static bool TryParseFieldCompareBool(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        if (startIndex >= instructions.Count)
        {
            return false;
        }

        if (!ILParserHelper.TryReadFieldChain(instructions[startIndex], out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (afterChain is null || !ILParserHelper.TryFormatLiteral(afterChain, leaf, out var literal))
        {
            return false;
        }

        var compare = afterChain.Next;
        if (compare is null || !ILParserHelper.IsStackCompare(compare.OpCode.Code))
        {
            return false;
        }

        var op = ILParserHelper.StackCompareOperator(compare.OpCode.Code);
        if (op.Length == 0)
        {
            return false;
        }

        var compareIndex = ILParserHelper.IndexOf(instructions, compare);
        if (compareIndex < 0)
        {
            return false;
        }

        expression = $"{path} {op} {literal}";
        endExclusive = compareIndex + 1;

        return true;
    }

    private static bool IsGetMoodleCall(Instruction instruction, MethodReference getMoodleMethod)
    {
        if (instruction.OpCode.Code is not (Code.Call or Code.Callvirt))
        {
            return false;
        }

        if (instruction.Operand is not MethodReference called)
        {
            return false;
        }

        return called.Name == getMoodleMethod.Name
               && called.DeclaringType.FullName == getMoodleMethod.DeclaringType.FullName;
    }

    private static bool TryParseIntensity(
        Collection<Instruction> instructions,
        int startIndex,
        int callIndex,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        if (TryParseTernary(instructions, startIndex, out expression, out endExclusive))
        {
            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            expression = ILInstructionParser.ParseInt(instructions[startIndex]).ToString();
            endExclusive = startIndex + 1;
            return true;
        }

        return TryParseComputedIntensity(instructions, startIndex, callIndex, localPaths, out expression, out endExclusive);
    }

    private static bool TryParseComputedIntensity(
        Collection<Instruction> instructions,
        int startIndex,
        int callIndex,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        var iconIndex = FindIconIndex(instructions, startIndex, callIndex);
        if (iconIndex <= startIndex)
        {
            return false;
        }

        var slice = new Instruction[iconIndex - startIndex];
        for (var i = 0; i < slice.Length; i++)
        {
            slice[i] = instructions[startIndex + i];
        }

        if (!ILComplexExpressionParser.TryFormat(slice, localPaths, out expression))
        {
            return false;
        }

        endExclusive = iconIndex;
        return true;
    }

    private static int FindIconIndex(Collection<Instruction> instructions, int startIndex, int callIndex)
    {
        for (var i = startIndex; i < callIndex; i++)
        {
            if (instructions[i].OpCode.Code == Code.Ldstr)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseTernaryOrLiteral(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        if (TryParseTernary(instructions, startIndex, out expression, out endExclusive))
        {
            return true;
        }

        if (startIndex < instructions.Count && ILInstructionParser.IsLdcI4(instructions[startIndex]))
        {
            expression = ILInstructionParser.ParseInt(instructions[startIndex]).ToString();
            endExclusive = startIndex + 1;
            return true;
        }

        expression = "";
        endExclusive = startIndex;

        return false;
    }

    private static bool TryParseTernary(
        Collection<Instruction> instructions,
        int startIndex,
        out string expression,
        out int endExclusive)
    {
        expression = "";
        endExclusive = startIndex;

        if (startIndex >= instructions.Count)
        {
            return false;
        }

        if (!TryReadTernaryCondition(instructions[startIndex], out var condition, out var branchInsn))
        {
            return false;
        }

        var branchIndex = ILParserHelper.IndexOf(instructions, branchInsn);
        if (branchIndex < 0 || branchIndex + 1 >= instructions.Count)
        {
            return false;
        }

        var falseStart = branchIndex + 1;
        if (!TryParseTernaryOrLiteral(instructions, falseStart, out var whenFalse, out var afterFalse))
        {
            return false;
        }

        if (afterFalse >= instructions.Count)
        {
            return false;
        }

        var skipInsn = instructions[afterFalse];
        if (skipInsn.OpCode.Code is not (Code.Br or Code.Br_S)
            || skipInsn.Operand is not Instruction mergeLabel)
        {
            return false;
        }

        if (branchInsn.Operand is not Instruction trueLabel)
        {
            return false;
        }

        var trueStart = ILParserHelper.IndexOf(instructions, trueLabel);
        if (trueStart < 0)
        {
            return false;
        }

        if (!TryParseTernaryOrLiteral(instructions, trueStart, out var whenTrue, out var afterTrue))
        {
            return false;
        }

        var mergeIndex = ILParserHelper.IndexOf(instructions, mergeLabel);
        if (mergeIndex < 0 || afterTrue != mergeIndex)
        {
            return false;
        }

        expression = $"({condition}) ? {WrapNested(whenTrue)} : {WrapNested(whenFalse)}";
        endExclusive = mergeIndex;

        return true;
    }

    private static bool TryReadTernaryCondition(
        Instruction start,
        out string condition,
        out Instruction branchInsn)
    {
        condition = "";
        branchInsn = null!;

        if (!ILParserHelper.TryReadFieldChain(start, out var path, out var leaf, out var afterChain))
        {
            return false;
        }

        if (afterChain is null || !ILParserHelper.TryFormatLiteral(afterChain, leaf, out var literal))
        {
            return false;
        }

        var branch = afterChain.Next;
        if (branch is null || branch.Operand is not Instruction)
        {
            return false;
        }

        var op = ILParserHelper.BranchOperatorWhenTaken(branch.OpCode.Code);
        if (op.Length == 0)
        {
            return false;
        }

        branchInsn = branch;
        condition = $"{path} {op} {literal}";
        return true;
    }

    private static int FindAddMoodleArgsStartIndex(Collection<Instruction> instructions, int callIndex)
    {
        // receiver is the last ldarg.0 before call whose next when starts loading args
        // (not ldarg.0 → ldfld, which is a guard or nested field read).
        for (var i = callIndex - 1; i >= 0; i--)
        {
            if (instructions[i].OpCode.Code != Code.Ldarg_0)
            {
                continue;
            }

            if (i + 1 < callIndex && instructions[i + 1].OpCode.Code == Code.Ldfld)
            {
                continue;
            }

            if (i > 0 && instructions[i - 1].OpCode.Code == Code.Ldarg_0)
            {
                return i - 1;
            }

            return i;
        }

        return -1;
    }

    private static string SubstituteGuards(string guards, IReadOnlyDictionary<int, string> localPaths) =>
        string.IsNullOrEmpty(guards)
            ? "none"
            : LimbAggregateLocalAnalyzer.SubstituteLocals(guards, localPaths);

    private static bool IsCriticalExprStart(Collection<Instruction> instructions, int cursor) =>
        cursor + 1 < instructions.Count
            && instructions[cursor].OpCode.Code == Code.Ldarg_0
            && instructions[cursor + 1].OpCode.Code == Code.Ldfld;

    private static string WrapNested(string value) => value.Contains('?') ? $"({value})" : value;
}
