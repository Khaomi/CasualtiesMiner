using CasualtiesMiner.Dumper.Parsing;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    private static int ParseObjectFields(
        CSharpDecompiler decompiler,
        Collection<Instruction> instructions,
        int startIndex,
        string[] declaringTypeNames,
        Dictionary<string, object?> target,
        Func<string, bool> isStopCall)
    {
        var i = startIndex;

        while (++i < instructions.Count)
        {
            var inst = instructions[i];

            if (inst.OpCode.Code == Code.Callvirt && isStopCall(inst.Operand?.ToString() ?? ""))
            {
                return i;
            }

            if (inst.OpCode.Code != Code.Dup)
            {
                continue;
            }

            var valueOpcodes = new List<Instruction>();

            while (++i < instructions.Count)
            {
                var next = instructions[i];
                if (next.OpCode.Code == Code.Stfld
                    && next.Operand is FieldDefinition fd
                    && declaringTypeNames.Contains(fd.DeclaringType.Name))
                {
                    target[fd.Name] = ParseFieldValue(decompiler, fd.FieldType, valueOpcodes, fd.Name);
                    break;
                }

                valueOpcodes.Add(next);
            }
        }

        return i;
    }

    private static object? ParseFieldValue(
        CSharpDecompiler decompiler,
        TypeReference type,
        List<Instruction> instructions,
        string fieldName = "")
    {
        if (instructions.Count == 0)
        {
            return null;
        }

        if (type.IsPrimitive)
            return type.Name switch
            {
                "Boolean" => instructions[0].OpCode.Code == Code.Ldc_I4_1,
                "Single" => Convert.ToSingle(instructions[0].Operand),
                "Byte" => Convert.ToByte(ILInstructionParser.ParseInt(instructions[0])),
                "Int32" => ILInstructionParser.ParseInt(instructions[0]),
                _ => WarnUnhandled(type, instructions[0], fieldName)
            };

        if (type.IsArray && type is ArrayType { ElementType.FullName: "System.String" })
            return ParseStringArray(instructions);

        return type.Name switch
        {
            "String" => instructions[0].Operand ?? instructions[0].OpCode.Name,
            "Recognition" or "SleepQuality" => ILInstructionParser.ParseInt(instructions[0]),
            _ => ParseComplexValue(decompiler, type, instructions, fieldName)
        };
    }

    private static object? ParseComplexValue(
        CSharpDecompiler decompiler,
        TypeReference type,
        List<Instruction> instructions,
        string fieldName)
    {
        switch (type.FullName)
        {
            case "LiquidStack":
                {
                    return new LiquidStack
                    {
                        liquidId = (string)instructions[0].Operand,
                        amount = Convert.ToSingle(instructions[1].Operand)
                    };
                }
            case "RecipeResult":
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var (f, vals) in ExtractFields(instructions))
                    {
                        dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);
                    }

                    return new RecipeResult
                    {
                        id = GetValue<string>(dict, "id"),
                        isLiquid = GetValue<bool>(dict, "isLiquid"),
                        amount = GetValue(dict, "amount", 1),
                        resultCondition = GetValue(dict, "resultCondition", 1f),
                        dontDrainResultLiquid = GetValue<bool>(dict, "dontDrainResultLiquid")
                    };
                }
            case "RecipeItem":
                {
                    var minimumCondition = Convert.ToSingle(instructions[0].Operand);

                    var dict = new Dictionary<string, object?>();
                    foreach (var (f, vals) in ExtractFields(instructions))
                    {
                        dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);
                    }

                    return new RecipeItem
                    {
                        specific = GetValue<bool>(dict, "specific"),
                        specificId = GetValue<string>(dict, "specificId"),
                        isLiquid = GetValue<bool>(dict, "isLiquid"),
                        quality = GetValue<CraftingQuality>(dict, "quality"),
                        minimumCondition = dict.ContainsKey("minimumCondition")
                            ? GetValue(dict, "minimumCondition", 0.9f)
                            : minimumCondition,
                        destroyItem = GetValue(dict, "destroyItem", true),
                        ignoredId = GetValue<string>(dict, "ignoredId")
                    };
                }
            case "Recipes/RecipeCategory":
                return ILInstructionParser.ParseInt(instructions[0]);
            case "CraftingQuality":
                {
                    var craftingQuality = new CraftingQuality
                    {
                        id = "",
                        amount = 0
                    };

                    switch (instructions.Count)
                    {
                        case 3:
                            craftingQuality.id = (string)instructions[0].Operand;
                            craftingQuality.amount = (float)instructions[1].Operand;
                            break;
                        case 2:
                            craftingQuality.id = (string)instructions[0].Operand;
                            craftingQuality.amount = 1f;
                            break;
                    }

                    return craftingQuality;
                }
            case "UnityEngine.Color":
                {
                    var result = new Color();

                    switch (instructions.Count)
                    {
                        case 4:
                            result.r = (byte)(Convert.ToSingle(instructions[0].Operand) * 255);
                            result.g = (byte)(Convert.ToSingle(instructions[1].Operand) * 255);
                            result.b = (byte)(Convert.ToSingle(instructions[2].Operand) * 255);
                            result.a = 255;
                            break;
                        case 6:
                            result.r = (byte)ILInstructionParser.ParseInt(instructions[0]);
                            result.g = (byte)ILInstructionParser.ParseInt(instructions[1]);
                            result.b = (byte)ILInstructionParser.ParseInt(instructions[2]);
                            result.a = (byte)ILInstructionParser.ParseInt(instructions[3]);
                            break;
                    }

                    return result;
                }
            case "ItemInfo/Use":
            case "ItemInfo/UseLimb":
            case "LiquidType/OnDrink":
            case "LiquidType/OnHealthUse":
                {
                    var pointerToDelegate = instructions.FirstOrDefault(p => p.OpCode.Code == Code.Ldftn);
                    if (pointerToDelegate is null)
                    {
                        return null;
                    }

                    var methodRef = (MethodReference)pointerToDelegate.Operand;
                    var methodDef = methodRef.Resolve();

                    return decompiler.DecompileAsString(MetadataTokens.EntityHandle(methodDef.MetadataToken.ToInt32()))
                        .Replace("\r\n", "\n").Replace("\t", "    ").Split("\n");
                }
        }

        if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
        {
            var elementType = type is GenericInstanceType git ? git.GenericArguments[0] : type;
            return ParseList(decompiler, elementType, instructions, fieldName);
        }

        Console.WriteLine($"[WARNING] No parser for '{fieldName}' ({type.FullName})");

        foreach (var inst in instructions)
        {
            Console.WriteLine($"  {inst}");
        }

        return null;
    }

    private static List<object?> ParseList(
        CSharpDecompiler decompiler,
        TypeReference elementType,
        List<Instruction> instructions,
        string fieldName)
    {
        var items = new List<object?>();
        var buffer = new List<Instruction>();
        List<Instruction>? current = null;

        foreach (var inst in instructions)
            switch (inst.OpCode.Code)
            {
                case Code.Newobj:
                case Code.Call:
                    if (inst.Operand is MethodReference ctor
                        && !ctor.DeclaringType.FullName.StartsWith("System.Collections.Generic.List`1"))
                    {
                        if (current is null)
                        {
                            current = [.. buffer, inst];
                            buffer.Clear();
                        }
                        else
                        {
                            current.Add(inst);
                        }
                    }

                    break;

                case Code.Callvirt:
                    if (current != null && inst.Operand?.ToString()?.Contains("::Add(") == true)
                    {
                        items.Add(ParseFieldValue(decompiler, elementType, current, fieldName));
                        current = null;
                    }

                    break;

                case Code.Dup:
                    buffer.Clear();
                    current?.Add(inst);
                    break;

                case Code.Stfld:
                    current?.Add(inst);
                    break;

                default:
                    (current ?? buffer).Add(inst);
                    break;
            }

        return items;
    }

    private static string[] ParseStringArray(List<Instruction> instructions)
    {
        if (instructions.Any(i => i.OpCode.Code == Code.Ldnull))
            return [];

        var items = new List<string>();
        string? pending = null;

        foreach (var inst in instructions)
        {
            switch (inst.OpCode.Code)
            {
                case Code.Ldstr:
                    pending = (string)inst.Operand;
                    break;
                case Code.Stelem_Ref:
                    if (pending != null)
                    {
                        items.Add(pending);
                        pending = null;
                    }

                    break;
            }
        }

        if (items.Count > 0)
            return [.. items];

        var single = instructions.FirstOrDefault(i => i.OpCode.Code == Code.Ldstr);
        if (single?.Operand is string text)
            return text.Split(',');

        return [];
    }

    private static object WarnUnhandled(TypeReference type, Instruction inst,
        string fieldName)
    {
        Console.WriteLine($"[WARNING] Unhandled primitive type: {type.Name} for field {fieldName}");

        return inst.Operand ?? inst.OpCode.Name;
    }

    private static Dictionary<FieldDefinition, List<Instruction>> ExtractFields(List<Instruction> instructions)
    {
        var fields = new Dictionary<FieldDefinition, List<Instruction>>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode.Code != Code.Dup)
            {
                continue;
            }
            i++;

            var valueOpcodes = new List<Instruction>();
            while (i < instructions.Count && instructions[i].OpCode.Code != Code.Stfld)
            {
                valueOpcodes.Add(instructions[i++]);
            }

            if (i < instructions.Count && instructions[i].Operand is FieldDefinition field)
            {
                fields[field] = valueOpcodes;
            }
        }

        return fields;
    }

    private static T GetValue<T>(Dictionary<string, object?> dictionary, string key, T defaultValue = default!)
    {
        if (dictionary.TryGetValue(key, out var value) && value is not null)
        {
            return (T)value;
        }

        return defaultValue;
    }
}
