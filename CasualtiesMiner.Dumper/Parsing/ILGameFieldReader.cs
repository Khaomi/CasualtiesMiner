using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILGameFieldReader
{
    public static float? ReadStaticFloatInitializer(TypeDefinition type, string fieldName)
    {
        var field = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == fieldName);
        if (field is null)
        {
            return null;
        }

        var cctor = type.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic && m.HasBody);
        if (cctor?.Body is null)
        {
            return null;
        }

        return ReadFloatStoredToField(cctor.Body.Instructions, field);
    }

    public static float? ReadInstanceFieldStore(TypeDefinition type, string methodName, string fieldName)
    {
        var field = type.Fields.FirstOrDefault(f => !f.IsStatic && f.Name == fieldName);
        if (field is null)
        {
            return null;
        }

        var method = type.Methods.FirstOrDefault(m => m.Name == methodName && m.HasBody);
        if (method?.Body is null)
        {
            return null;
        }

        return ReadFloatStoredToField(method.Body.Instructions, field);
    }

    public static float? ReadSplintBoneHealExtraMultiplier(TypeDefinition splintType)
    {
        var method = splintType.Methods.FirstOrDefault(m => m.Name == "Update" && m.HasBody);
        if (method?.Body is null)
        {
            return null;
        }

        var instructions = method.Body.Instructions;
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld)
            {
                continue;
            }

            if (instructions[i].Operand is not FieldReference speedField
                || speedField.Name != "boneHealSpeed")
            {
                continue;
            }

            for (var j = i + 1; j < instructions.Count && j <= i + 6; j++)
            {
                if (!TryReadFloatLiteral(instructions[j], out var literal))
                {
                    continue;
                }

                if (literal > 0f)
                {
                    return literal;
                }
            }
        }

        return null;
    }

    public static float? ReadSplintHealDivisor(TypeDefinition limbType, string getterName, float expectedApprox)
    {
        var method = limbType.Methods.FirstOrDefault(m => m.Name == getterName && m.HasBody);
        if (method?.Body is null)
        {
            return null;
        }

        foreach (var instruction in method.Body.Instructions)
        {
            if (!TryReadFloatLiteral(instruction, out var literal))
            {
                continue;
            }

            if (Math.Abs(literal - expectedApprox) < 0.01f)
            {
                return literal;
            }
        }

        return null;
    }

    public static float? ReadMoodleIntensityScale(TypeDefinition moodleManagerType)
    {
        var method = moodleManagerType.Methods.FirstOrDefault(m => m.Name == "AddAllMoodles" && m.HasBody);
        if (method?.Body is null)
        {
            return null;
        }

        var instructions = method.Body.Instructions;
        for (var i = 0; i < instructions.Count; i++)
        {
            if (!TryReadFloatLiteral(instructions[i], out var literal))
            {
                continue;
            }

            if (literal is < 0.01f or > 0.10f)
            {
                continue;
            }

            if (!IsUsedAsMultiplyOperand(instructions, i))
            {
                continue;
            }

            if (!LeadsToRoundToInt(instructions, i))
            {
                continue;
            }

            return literal;
        }

        return null;
    }

    private static float? ReadFloatStoredToField(Collection<Instruction> instructions, FieldDefinition field)
    {
        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (!TryReadFloatLiteral(instructions[i], out var literal))
            {
                continue;
            }

            if (instructions[i + 1].OpCode.Code is not (Code.Stsfld or Code.Stfld))
            {
                continue;
            }

            if (instructions[i + 1].Operand is FieldReference stored && stored.Name == field.Name)
            {
                return literal;
            }
        }

        return null;
    }

    private static bool IsUsedAsMultiplyOperand(Collection<Instruction> instructions, int literalIndex)
    {
        for (var i = literalIndex + 1; i < instructions.Count && i <= literalIndex + 4; i++)
        {
            if (instructions[i].OpCode.Code == Code.Mul)
            {
                return true;
            }
        }

        return false;
    }

    private static bool LeadsToRoundToInt(Collection<Instruction> instructions, int literalIndex)
    {
        for (var i = literalIndex + 1; i < instructions.Count && i <= literalIndex + 12; i++)
        {
            if (instructions[i].OpCode.Code is not (Code.Call or Code.Callvirt))
            {
                continue;
            }

            if (instructions[i].Operand is MethodReference called
                && called.Name == "RoundToInt")
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadFloatLiteral(Instruction instruction, out float value)
    {
        value = 0f;

        return instruction.OpCode.Code switch
        {
            Code.Ldc_R4 => Assign((float)instruction.Operand!, out value),
            Code.Ldc_R8 => Assign((float)(double)instruction.Operand!, out value),
            _ => false
        };
    }

    private static bool Assign(float value, out float result)
    {
        result = value;

        return true;
    }
}
