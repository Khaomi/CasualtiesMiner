using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public LiquidType[] DumpLiquids(CSharpDecompiler decompiler)
    {
        var liquidList = new List<LiquidType>();

        var liquidsType = _module.Types.FirstOrDefault(t => t.FullName == "Liquids");
        var liquidType = _module.Types.FirstOrDefault(t => t.FullName == "LiquidType");

        if (liquidsType is null || liquidType is null)
        {
            return [];
        }

        var cctor = liquidsType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
        if (cctor is null)
        {
            return [];
        }

        //ILInstructionFormat.WriteMethodIl(Console.Out, cctor, markAddMoodleCalls: true);
        //Console.WriteLine();

        var liquidCtor = liquidType.Methods.First(m => m.IsConstructor && !m.HasParameters);
        var instructions = cctor.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldstr)
            {
                continue;
            }
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != liquidCtor)
            {
                continue;
            }

            var registryId = (string)instructions[i].Operand;
            var entry = new Dictionary<string, object?> { ["name"] = registryId };

            i = ParseObjectFields(decompiler, instructions, i + 1, ["LiquidType"], entry,
                op => op.Contains("::set_Item("));

            static List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }

            var liquid = new LiquidType
            {
                liquidId = registryId,
                localeName = GetValue<string>(entry, "localeName"),
                color = GetValue<Color>(entry, "color"),
                valuePerLiter = GetValue<float>(entry, "valuePerLiter"),
                onDrink = GetValue<string[]>(entry, "onDrink"),
                onHealthUse = GetValue<string[]>(entry, "onHealthUse"),
                healthUsable = GetValue<bool>(entry, "healthUsable"),
                injectable = GetValue<bool>(entry, "injectable"),
                localeFromItem = GetValue<bool>(entry, "localeFromItem"),
                injectionSickness = GetValue(entry, "injectionSickness", 1f),
                qualities = ConvertList<CraftingQuality>(GetValue<List<object?>>(entry, "qualities"))
            };

            liquidList.Add(liquid);
        }

        return [.. liquidList];
    }
}
