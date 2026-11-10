using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing;

internal static partial class LimbAggregateLocalAnalyzer
{
    public static IReadOnlyDictionary<int, string> Analyze(IList<Instruction> instructions)
    {
        var map = new Dictionary<int, string>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (!ILParserHelper.TryGetLocalStoreIndex(instructions[i], out var localIndex))
            {
                continue;
            }

            if (TryParseMaxAccumulation(instructions, i, out var fieldName))
            {
                map[localIndex] = $"body.limbs.max.{fieldName}";
                continue;
            }

            if (TryParseLimbLoopFlag(instructions, i, out var flagPath))
            {
                map[localIndex] = flagPath;
            }
        }

        return map;
    }

    private static bool TryParseLimbLoopFlag(
        IList<Instruction> instructions,
        int stlocIndex,
        out string path)
    {
        path = "";

        if (stlocIndex < 3)
        {
            return false;
        }

        if (instructions[stlocIndex - 1].OpCode.Code is not (Code.Ldc_I4_1 or Code.Ldc_I4))
        {
            return false;
        }

        if (instructions[stlocIndex - 1].OpCode.Code == Code.Ldc_I4
            && ILInstructionParser.ParseInt(instructions[stlocIndex - 1]) != 1)
        {
            return false;
        }

        var branch = instructions[stlocIndex - 2];
        if (branch.OpCode.Code is not (Code.Brfalse or Code.Brfalse_S))
        {
            return false;
        }

        if (instructions[stlocIndex - 3].OpCode.Code == Code.Ldfld
            && instructions[stlocIndex - 3].Operand is Mono.Cecil.FieldReference dismembered
            && dismembered.Name == "dismembered")
        {
            path = "body.limbs.any.dismembered";
            return true;
        }

        // PlayerCamera.main.showInfection[i]
        if (stlocIndex >= 6
            && instructions[stlocIndex - 3].OpCode.Code == Code.Ldelem_U1
            && instructions[stlocIndex - 4].OpCode.Code is Code.Ldloc or Code.Ldloc_S
            && instructions[stlocIndex - 5].OpCode.Code == Code.Ldfld
            && instructions[stlocIndex - 5].Operand is Mono.Cecil.FieldReference showInfection
            && showInfection.Name == "showInfection")
        {
            path = "body.limbs.any.showInfection";
            return true;
        }

        return false;
    }

    private static bool TryParseMaxAccumulation(
        IList<Instruction> instructions,
        int stlocIndex,
        out string fieldName)
    {
        fieldName = "";

        // limb.boneHealTimer
        // ldloc.s limb
        // ldfld boneHealTimer
        // ldloc accumulator
        // ble.un skip
        // ldloc.s limb
        // ldfld boneHealTimer
        // stloc accumulator
        if (stlocIndex < 6)
        {
            return false;
        }

        if (instructions[stlocIndex].OpCode.Code is not (Code.Stloc or Code.Stloc_S or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3))
        {
            return false;
        }

        if (instructions[stlocIndex - 1].OpCode.Code != Code.Ldfld
            || instructions[stlocIndex - 1].Operand is not Mono.Cecil.FieldReference assignField)
        {
            return false;
        }

        if (!ILParserHelper.IsLocalLoad(instructions[stlocIndex - 2]))
        {
            return false;
        }

        if (instructions[stlocIndex - 3].OpCode.Code is not (Code.Ble or Code.Ble_Un or Code.Ble_Un_S or Code.Ble_S))
        {
            return false;
        }

        if (!ILParserHelper.IsLocalLoad(instructions[stlocIndex - 4]))
        {
            return false;
        }

        if (instructions[stlocIndex - 5].OpCode.Code != Code.Ldfld
            || instructions[stlocIndex - 5].Operand is not Mono.Cecil.FieldReference compareField)
        {
            return false;
        }

        if (compareField.FullName != assignField.FullName)
        {
            return false;
        }

        if (!ILParserHelper.IsLocalLoad(instructions[stlocIndex - 6]))
        {
            return false;
        }

        fieldName = assignField.Name;
        return true;
    }

    public static string SubstituteLocals(string expression, IReadOnlyDictionary<int, string> localPaths)
    {
        if (localPaths.Count == 0 || string.IsNullOrEmpty(expression))
        {
            return expression;
        }

        return VarRegex().Replace(expression, match =>
            {
                var index = int.Parse(match.Groups[1].Value);
                return localPaths.TryGetValue(index, out var path) ? path : match.Value;
            });
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\bvar(\d+)\b")]
    private static partial System.Text.RegularExpressions.Regex VarRegex();
}
