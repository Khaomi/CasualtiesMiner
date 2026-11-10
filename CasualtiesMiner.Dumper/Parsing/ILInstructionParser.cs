using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace CasualtiesMiner.Dumper.Parsing;

internal static class ILInstructionParser
{
    public static bool IsLdcI4(Instruction instruction) =>
        instruction.OpCode.Code switch
        {
            Code.Ldc_I4_0 or Code.Ldc_I4_1
                          or Code.Ldc_I4_2
                          or Code.Ldc_I4_3
                          or Code.Ldc_I4_4
                          or Code.Ldc_I4_5
                          or Code.Ldc_I4_6
                          or Code.Ldc_I4_7
                          or Code.Ldc_I4_8
                          or Code.Ldc_I4_M1
                          or Code.Ldc_I4
                          or Code.Ldc_I4_S => true,
            _ => false
        };

    public static int ParseInt(Instruction instruction) =>
        instruction.OpCode.Code switch
        {
            Code.Ldc_I4_0 => 0,
            Code.Ldc_I4_1 => 1,
            Code.Ldc_I4_2 => 2,
            Code.Ldc_I4_3 => 3,
            Code.Ldc_I4_4 => 4,
            Code.Ldc_I4_5 => 5,
            Code.Ldc_I4_6 => 6,
            Code.Ldc_I4_7 => 7,
            Code.Ldc_I4_8 => 8,
            Code.Ldc_I4_M1 => -1,
            _ => instruction.Operand is int value ? value : Convert.ToInt32(instruction.Operand)
        };

    private static void ConsumeCallArguments(Collection<Instruction> instructions, ref int index, MethodReference method)
    {
        var argCount = method.Parameters.Count;

        if (method.HasThis)
        {
            argCount++;
        }

        for (var i = 0; i < argCount; i++)
        {
            ConsumeOne(instructions, ref index);
        }
    }

    public static void ConsumeOne(Collection<Instruction> instructions, ref int index)
    {
        if (index < 0)
        {
            throw new InvalidOperationException("Instruction index underflow while parsing IL stack.");
        }

        var instruction = instructions[index];
        index--;

        switch (instruction.OpCode.Code)
        {
            case Code.Nop:
            case Code.Ldstr:
            case Code.Ldnull:
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
            case Code.Ldc_R4:
            case Code.Ldc_R8:
            case Code.Ldarg_0:
            case Code.Ldarg_1:
            case Code.Ldarg_2:
            case Code.Ldarg_3:
            case Code.Ldarg:
            case Code.Ldarg_S:
            case Code.Ldloc_0:
            case Code.Ldloc_1:
            case Code.Ldloc_2:
            case Code.Ldloc_3:
            case Code.Ldloc:
            case Code.Ldloc_S:
            case Code.Ldloca:
            case Code.Ldloca_S:
            case Code.Ldsfld:
            case Code.Ldfld:
                return;

            case Code.Stloc_0:
            case Code.Stloc_1:
            case Code.Stloc_2:
            case Code.Stloc_3:
            case Code.Stloc:
            case Code.Stloc_S:
                ConsumeOne(instructions, ref index);
                return;

            case Code.Box:
                ConsumeOne(instructions, ref index);
                return;

            case Code.Ldelem_Ref:
            case Code.Ldelem_Any:
            case Code.Ldelem_I:
            case Code.Ldelem_I4:
            case Code.Ldelem_R4:
                ConsumeOne(instructions, ref index);
                ConsumeOne(instructions, ref index);
                return;

            case Code.Call:
            case Code.Callvirt:
                ConsumeCallArguments(instructions, ref index, (MethodReference)instruction.Operand!);
                return;

            case Code.Newobj:
                ConsumeCallArguments(instructions, ref index, (MethodReference)instruction.Operand!);
                return;

            case Code.Add:
            case Code.Sub:
            case Code.Mul:
            case Code.Div:
            case Code.Rem:
            case Code.And:
            case Code.Or:
            case Code.Xor:
            case Code.Shl:
            case Code.Shr:
            case Code.Shr_Un:
            case Code.Ceq:
            case Code.Cgt:
            case Code.Clt:
            case Code.Cgt_Un:
            case Code.Clt_Un:
                ConsumeOne(instructions, ref index);
                ConsumeOne(instructions, ref index);
                return;

            case Code.Neg:
            case Code.Not:
            case Code.Conv_I4:
            case Code.Conv_I8:
            case Code.Conv_R4:
            case Code.Conv_R8:
            case Code.Conv_U4:
                ConsumeOne(instructions, ref index);
                return;

            default:
                Console.WriteLine($"[WARNING] Unhandled IL while rewinding stack: {instruction}");
                return;
        }
    }
}
