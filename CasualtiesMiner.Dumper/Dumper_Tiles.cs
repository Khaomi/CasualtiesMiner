using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public BlockInfo[] DumpTiles(CSharpDecompiler decompiler)
    {
        var tileList = new List<BlockInfo>();

        var worldGenType = _module.Types.FirstOrDefault(t => t.FullName == "WorldGeneration");
        var blockInfoType = _module.Types.FirstOrDefault(t => t.FullName == "BlockInfo");
        if (worldGenType is null || blockInfoType is null)
        {
            return [];
        }

        var setupMethod = worldGenType.Methods.FirstOrDefault(m => m.Name == "GetBlockInfo");
        if (setupMethod is null)
        {
            return [];
        }

        var instructions = setupMethod.Body.Instructions;

        var switchInst = instructions.FirstOrDefault(i => i.OpCode.Code == Code.Switch);
        if (switchInst == null)
        {
            return [];
        }

        var switchTargets = (Instruction[])switchInst.Operand;

        foreach (var targetInst in switchTargets)
        {
            if (targetInst.OpCode.Code != Code.Newobj)
            {
                continue;
            }

            var entry = new Dictionary<string, object?>();

            var index = instructions.IndexOf(targetInst);
            for (var j = index; j < instructions.Count; j++)
            {
                var instruction = instructions[j];

                if (instruction.OpCode.Code == Code.Ret)
                {
                    break;
                }
                if (instruction.OpCode.Code != Code.Dup)
                {
                    continue;
                }

                var valueOpcodes = new List<Instruction>();
                var k = j + 1;
                while (k < instructions.Count && instructions[k].OpCode.Code != Code.Stfld)
                {
                    valueOpcodes.Add(instructions[k]);
                    k++;
                }

                if (k >= instructions.Count || instructions[k].Operand is not FieldDefinition fd)
                {
                    continue;
                }

                entry[fd.Name] = ParseFieldValue(decompiler, fd.FieldType, valueOpcodes, fd.Name);
                j = k;
            }

            tileList.Add(new BlockInfo
            {
                health = GetValue<float>(entry, "health"),
                name = GetValue<string>(entry, "name"),
                hitsound = GetValue<string>(entry, "hitsound"),
                stepsound = GetValue<string>(entry, "stepsound"),
                noVariation = GetValue<bool>(entry, "noVariation"),
                metallic = GetValue<bool>(entry, "metallic"),
                toxicity = GetValue<float>(entry, "toxicity"),
                slippery = GetValue<bool>(entry, "slippery"),
                sleep = GetValue<SleepQuality>(entry, "sleep")
            });
        }

        return [.. tileList];
    }
}
