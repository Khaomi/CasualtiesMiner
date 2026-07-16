using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Dumper.Parsing.Delegates;
using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public ItemInfo[] DumpItems(CSharpDecompiler decompiler, AssetsParser assetsParser)
    {
        var itemList = new List<ItemInfo>();

        var itemType = _module.Types.FirstOrDefault(t => t.FullName == "Item");

        var itemInfoType = _module.Types.FirstOrDefault(t => t.FullName == "ItemInfo");
        var liquidItemInfo = _module.Types.FirstOrDefault(t => t.FullName == "LiquidItemInfo");
        var batteryInfo = _module.Types.FirstOrDefault(t => t.FullName == "BatteryInfo");

        if (itemType is null)
            return [];

        if (itemInfoType is null || liquidItemInfo is null || batteryInfo is null)
            return [];

        var setupMethod = itemType.Methods.FirstOrDefault(m => m.Name == "SetupItems");
        var globalField = itemType.Fields.FirstOrDefault(m => m.Name == "GlobalItems");
        if (setupMethod is null || globalField is null)
            return [];

        //ILInstructionFormat.WriteMethodIl(Console.Out, setupMethod);
        //Console.WriteLine();

        var itemInfoCtor = itemInfoType.Methods.First(m => m.IsConstructor);
        var liquidItemInfoCtor = liquidItemInfo.Methods.FirstOrDefault(m => m.IsConstructor);
        var batteryInfoCtor = batteryInfo.Methods.FirstOrDefault(m => m.IsConstructor);

        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 2; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField)
            {
                continue;
            }
            if (instructions[i + 1].OpCode.Code != Code.Ldstr)
            {
                continue;
            }

            var isLiquid = liquidItemInfoCtor != null && instructions[i + 2].Operand == liquidItemInfoCtor;
            var isBattery = batteryInfoCtor != null && instructions[i + 2].Operand == batteryInfoCtor;

            if (instructions[i + 2].OpCode.Code != Code.Newobj ||
                (instructions[i + 2].Operand != itemInfoCtor && !isLiquid && !isBattery))
            {
                continue;
            }

            var itemName = (string)instructions[i + 1].Operand;
            var spriteName = assetsParser.ExtractSprite(itemName);
            var itemDict = new Dictionary<string, object?>();

            string[] validTypes = ["ItemInfo"];
            if (isLiquid)
            {
                validTypes = ["ItemInfo", "LiquidItemInfo"];
            }
            if (isBattery)
            {
                validTypes = ["ItemInfo", "BatteryInfo"];
            }

            i = ParseObjectFields(decompiler, instructions, i + 2, validTypes, itemDict, op =>
                op is "System.Void System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::Add(!0,!1)"
                    or "System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> System.Collections.Generic.Dictionary`2<System.String,ItemInfo>::GetEnumerator()");

            ItemInfo item;

            if (isLiquid)
                item = new LiquidItemInfo
                {
                    capacity = GetValue<float>(itemDict, "capacity"),
                    autoFill = GetValue<bool>(itemDict, "autoFill"),
                    defaultContents = ConvertList<LiquidStack>(GetValue<List<object?>>(itemDict, "defaultContents"))
                };
            else if (isBattery)
                item = new BatteryInfo
                {
                    maxCharge = GetValue<float>(itemDict, "maxCharge")
                };
            else
                item = new ItemInfo();

            item.fullName = itemName;
            item.spriteName = spriteName;
            item.category = GetValue<string>(itemDict, "category");
            item.slotRotation = GetValue<float>(itemDict, "slotRotation");
            item.usable = GetValue<bool>(itemDict, "usable");
            item.usableOnLimb = GetValue<bool>(itemDict, "usableOnLimb");
            item.rotSpeed = GetValue<float>(itemDict, "rotSpeed");
            item.useAction = GetValue<string[]>(itemDict, "useAction");
            item.useLimbAction = GetValue<string[]>(itemDict, "useLimbAction");
            item.destroyAtZeroCondition = GetValue<bool>(itemDict, "destroyAtZeroCondition");
            item.weight = GetValue<float>(itemDict, "weight");
            item.scaleWeightWithCondition = GetValue<bool>(itemDict, "scaleWeightWithCondition");
            item.onlyHoldInHands = GetValue<bool>(itemDict, "onlyHoldInHands");
            item.autoAttack = GetValue<bool>(itemDict, "autoAttack");
            item.usableWithLMB = GetValue<bool>(itemDict, "usableWithLMB");
            item.wearable = GetValue<bool>(itemDict, "wearable");
            item.wearableCanBeHeld = GetValue<bool>(itemDict, "wearableCanBeHeld");
            item.desiredWearLimb = GetValue<string>(itemDict, "desiredWearLimb");
            item.wearSlotId = GetValue<string>(itemDict, "wearSlotId");
            item.wearableArmor = GetValue<float>(itemDict, "wearableArmor");
            item.wearableIsolation = GetValue<float>(itemDict, "wearableIsolation");
            item.wearableHitDurabilityLossMultiplier = GetValue<float>(itemDict, "wearableHitDurabilityLossMultiplier");
            item.jumpHeightMultChange = GetValue<float>(itemDict, "jumpHeightMultChange");
            item.combineable = GetValue<bool>(itemDict, "combineable");
            item.ignoreDepression = GetValue<bool>(itemDict, "ignoreDepression");
            item.value = GetValue<int>(itemDict, "value");
            item.wearableVisualOffset = GetValue(itemDict, "wearableVisualOffset", 5);
            item.tags = GetValue<string>(itemDict, "tags");
            item.decayInfo = GetValue<byte>(itemDict, "decayInfo");
            item.decayMinutes = GetValue<float>(itemDict, "decayMinutes");
            item.rec = new Recognition
            {
                min = GetValue(itemDict, "rec", 2)
            };
            item.qualities = ConvertList<CraftingQuality>(GetValue<List<object?>>(itemDict, "qualities"));

            if (item.useAction != null)
            {
                DelegateParser.Parse(item.useAction);
            }
            if (item.useLimbAction != null)
            {
                DelegateParser.Parse(item.useLimbAction);
            }

            itemList.Add(item);
            continue;

            static List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }
        }

        return [.. itemList];
    }
}
