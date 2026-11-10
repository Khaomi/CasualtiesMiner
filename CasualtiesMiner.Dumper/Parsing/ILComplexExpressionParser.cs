using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILComplexExpressionParser
{
    public static bool TryFormat(
        IReadOnlyList<Instruction> instructions,
        IReadOnlyDictionary<int, string> localPaths,
        out string expression)
    {
        expression = "";

        if (instructions.Count == 0)
        {
            return false;
        }

        var index = instructions.Count - 1;
        var result = ParseExpression(instructions, ref index, localPaths);
        if (result is null || index >= 0)
        {
            return false;
        }

        expression = result;

        return true;
    }

    private static string? ParseExpression(
        IReadOnlyList<Instruction> instructions,
        ref int index,
        IReadOnlyDictionary<int, string> localPaths)
    {
        if (index < 0)
        {
            return null;
        }

        var insn = instructions[index];
        index--;

        switch (insn.OpCode.Code)
        {
            case Code.Call:
            case Code.Callvirt:
                return ParseCall(instructions, ref index, (MethodReference)insn.Operand!, localPaths);

            case Code.Add:
                return ParseBinary(instructions, ref index, "+", localPaths);
            case Code.Sub:
                return ParseBinary(instructions, ref index, "-", localPaths);
            case Code.Mul:
                return ParseBinary(instructions, ref index, "*", localPaths);
            case Code.Div:
                return ParseBinary(instructions, ref index, "/", localPaths);

            case Code.Ldc_R4:
                return ILParserHelper.FormatFloatLiteral((float)insn.Operand!);
            case Code.Ldc_R8:
                return ILParserHelper.FormatFloatLiteral((float)(double)insn.Operand!);

            case Code.Ldc_I4_0:
            case Code.Ldc_I4_1:
            case Code.Ldc_I4_2:
            case Code.Ldc_I4_3:
            case Code.Ldc_I4_4:
            case Code.Ldc_I4_5:
            case Code.Ldc_I4_6:
            case Code.Ldc_I4_7:
            case Code.Ldc_I4_8:
            case Code.Ldc_I4_M1:
            case Code.Ldc_I4:
            case Code.Ldc_I4_S:
                return ILInstructionParser.ParseInt(insn).ToString();

            case Code.Ldfld:
                var receiver = ParseExpression(instructions, ref index, localPaths);
                if (receiver is null)
                {
                    return null;
                }

                var fieldName = ((FieldReference)insn.Operand!).Name;
                return string.IsNullOrEmpty(receiver) ? fieldName : $"{receiver}.{fieldName}";

            case Code.Ldelem_Ref:
            case Code.Ldelem_Any:
            case Code.Ldelem_I:
            case Code.Ldelem_I4:
            case Code.Ldelem_R4:
                var elemIndex = ParseExpression(instructions, ref index, localPaths);
                var array = ParseExpression(instructions, ref index, localPaths);
                if (elemIndex is null || array is null)
                {
                    return null;
                }

                return $"{array}[{elemIndex}]";

            case Code.Ldarg_0:
                return "";

            case Code.Ldloc_0:
            case Code.Ldloc_1:
            case Code.Ldloc_2:
            case Code.Ldloc_3:
            case Code.Ldloc:
            case Code.Ldloc_S:
                return ILParserHelper.FormatLocalName(insn, localPaths);

            case Code.Conv_R4:
            case Code.Conv_R8:
            case Code.Conv_I4:
                return ParseExpression(instructions, ref index, localPaths);

            default:
                return null;
        }
    }

    private static string? ParseCall(
        IReadOnlyList<Instruction> instructions,
        ref int index,
        MethodReference method,
        IReadOnlyDictionary<int, string> localPaths)
    {
        var args = new string[method.Parameters.Count];
        for (var i = args.Length - 1; i >= 0; i--)
        {
            var arg = ParseExpression(instructions, ref index, localPaths);
            if (arg is null)
            {
                return null;
            }

            args[i] = arg;
        }

        return $"{FormatMethodName(method)}({string.Join(", ", args)})";
    }

    private static string? ParseBinary(
        IReadOnlyList<Instruction> instructions,
        ref int index,
        string op,
        IReadOnlyDictionary<int, string> localPaths)
    {
        var right = ParseExpression(instructions, ref index, localPaths);
        var left = ParseExpression(instructions, ref index, localPaths);
        if (right is null || left is null)
        {
            return null;
        }

        return $"({left} {op} {right})";
    }

    private static string FormatMethodName(MethodReference method)
    {
        var typeName = method.DeclaringType.Name;
        return typeName is "Mathf" or "Math"
            ? $"{typeName}.{method.Name}"
            : method.Name;
    }
}
