using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CasualtiesMiner.Dumper;

public partial class Dumper
{
    private readonly ModuleDefinition _module;
    private Language _language = new Language();

    public Dumper(string filePath)
    {
        _module = ModuleDefinition.ReadModule(filePath);
    }

    public Dumper(ModuleDefinition module)
    {
        _module = module;
    }

    [JsonSerializable(typeof(Language))]
    private partial class LocaleContext : JsonSerializerContext
    {
        public static LocaleContext Repo { get; } = new LocaleContext(new JsonSerializerOptions()
        {
            IncludeFields = true
        });
    }
    
    public async Task FetchEnglishLocaleAsync()
    {
        const string tag = "v6.1";
        const string hash = "3FEC04F51139DF1B9312D8B10AE508E40DF68B2D00CB2BBC96A38DC7A0D62980";
        const string url = $"https://raw.githubusercontent.com/orsoniks/scavgame-locale/refs/tags/{tag}/EN.json";
        
        // ReSharper disable once ShortLivedHttpClient - not used frequently
        using var httpClient = new HttpClient();

        try
        {
            var response = await httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            
            var responseHash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
            var expectedHash = Convert.FromHexString(hash);

            if (!responseHash.SequenceEqual(expectedHash))
                throw new InvalidOperationException($"Hash integrity check failed for English locale.");

            _language =
                JsonSerializer.Deserialize(body, LocaleContext.Repo.Language)
                ?? throw new InvalidOperationException($"Deserializer returned null for English locale.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to retrieve translation files from the main repository.");
            Console.WriteLine($"Exception: {e}");
            _language = new Language();
        }
    }

    public ItemInfo[] DumpItems(CSharpDecompiler decompiler)
    {
        var itemList = new List<ItemInfo>();

        var itemType = _module.Types.FirstOrDefault(t => t.FullName == "Item");

        var itemInfoType = _module.Types.FirstOrDefault(t => t.FullName == "ItemInfo");
        var liquidItemInfo = _module.Types.FirstOrDefault(t => t.FullName == "LiquidItemInfo");
        var batteryInfo = _module.Types.FirstOrDefault(t => t.FullName == "BatteryInfo");

        if (itemType is null) return [];

        //

        if (itemInfoType is null || liquidItemInfo is null || batteryInfo is null)
            return [];

        var setupMethod = itemType.Methods.FirstOrDefault(m => m.Name == "SetupItems");
        var globalField = itemType.Fields.FirstOrDefault(m => m.Name == "GlobalItems");
        if (setupMethod is null || globalField is null) return [];

        var itemInfoCtor = itemInfoType.Methods.First(m => m.IsConstructor);
        var liquidItemInfoCtor = liquidItemInfo.Methods.FirstOrDefault(m => m.IsConstructor);
        var batteryInfoCtor = batteryInfo.Methods.FirstOrDefault(m => m.IsConstructor);

        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 2; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Code != Code.Ldstr) continue;

            var isLiquid = liquidItemInfoCtor != null && instructions[i + 2].Operand == liquidItemInfoCtor;
            var isBattery = batteryInfoCtor != null && instructions[i + 2].Operand == batteryInfoCtor;

            if (instructions[i + 2].OpCode.Code != Code.Newobj ||
                (instructions[i + 2].Operand != itemInfoCtor && !isLiquid && !isBattery)) continue;

            var itemName = (string)instructions[i + 1].Operand;
            var itemDict = new Dictionary<string, object?>();

            string[] validTypes = ["ItemInfo"];
            if (isLiquid) validTypes = ["ItemInfo", "LiquidItemInfo"];
            if (isBattery) validTypes = ["ItemInfo", "BatteryInfo"];

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

            // 2. Map all the common base properties exactly once
            item.fullName = _language.main?.GetValueOrDefault(itemName, itemName) ?? itemName;
            item.description = _language.main?.GetValueOrDefault(itemName + "dsc", itemName + "dsc") ?? itemName + "dsc";
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
            item.tags = GetStringArray(itemDict, "actualTags")?.ToString()
                ?? GetStringArray(itemDict, "tags")?.ToString(); //TEMP
            item.decayInfo = GetValue<byte>(itemDict, "decayInfo");
            item.decayMinutes = GetValue<float>(itemDict, "decayMinutes");
            item.rec = new Recognition
            {
                min = GetValue(itemDict, "rec", 2)
            };
            item.qualities = ConvertList<CraftingQuality>(GetValue<List<object?>>(itemDict, "qualities"));

            itemList.Add(item);
            continue;

            static List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }
        }

        return [.. itemList];
    }

    public RecipeInfo[] DumpRecipes(CSharpDecompiler decompiler)
    {
        var recipeList = new List<RecipeInfo>();

        var recipesType = _module.Types.FirstOrDefault(t => t.FullName == "Recipes");
        var recipeType = _module.Types.FirstOrDefault(t => t.FullName == "Recipe");

        if (recipesType is null || recipeType is null) return [];

        var setupMethod = recipesType.Methods.FirstOrDefault(m => m.Name == "SetUpRecipes");
        var globalField = recipesType.Fields.FirstOrDefault(m => m.Name == "recipes");

        if (setupMethod is null || globalField is null) return [];

        var recipeCtor = recipeType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField) continue;
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != recipeCtor) continue;

            var recipeDict = new Dictionary<string, object?>();

            i = ParseObjectFields(decompiler, instructions, i + 1, ["Recipe"], recipeDict,
                op => op == "System.Void System.Collections.Generic.List`1<Recipe>::Add(!0)");

            var recipe = new RecipeInfo
            {
                specialKnown = GetValue<bool>(recipeDict, "specialKnown"),
                INT = GetValue<int>(recipeDict, "INT"),
                items = recipeDict.TryGetValue("items", out var itemsObj) && itemsObj is List<object?> list
                    ? [.. list.Cast<RecipeItem>()]
                    : [],
                result = GetValue<RecipeResult>(recipeDict, "result"),
                // Not needed, always false
                // hasMadeBefore = GetValue<bool>(recipeDict, "hasMadeBefore"),
                category = GetValue<int>(recipeDict, "category"),
                isRepair = GetValue<bool>(recipeDict, "isRepair"),
                index = GetValue<int>(recipeDict, "index")
            };

            recipeList.Add(recipe);
        }

        return [.. recipeList];
    }

    public LiquidType[] DumpLiquids(CSharpDecompiler decompiler)
    {
        var liquidList = new List<LiquidType>();

        var liquidsType = _module.Types.FirstOrDefault(t => t.FullName == "Liquids");
        var liquidType = _module.Types.FirstOrDefault(t => t.FullName == "LiquidType");
        if (liquidsType is null || liquidType is null) return [];

        var cctor = liquidsType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
        if (cctor is null) return [];

        var liquidCtor = liquidType.Methods.First(m => m.IsConstructor && !m.HasParameters);
        var instructions = cctor.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldstr) continue;
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != liquidCtor) continue;

            var key = (string)instructions[i].Operand;
            var entry = new Dictionary<string, object?> { ["name"] = key };

            i = ParseObjectFields(decompiler, instructions, i + 1, ["LiquidType"], entry,
                op => op.Contains("::set_Item("));

            List<T> ConvertList<T>(List<object?>? objects)
            {
                return objects?.Cast<T>().ToList() ?? [];
            }

            var liquid = new LiquidType
            {
                localeName = key,
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

    public BlockInfo[] DumpTiles(CSharpDecompiler decompiler)
    {
        var tileList = new List<BlockInfo>();

        var worldGenType = _module.Types.FirstOrDefault(t => t.FullName == "WorldGeneration");
        var blockInfoType = _module.Types.FirstOrDefault(t => t.FullName == "BlockInfo");
        if (worldGenType is null || blockInfoType is null) return [];

        var setupMethod = worldGenType.Methods.FirstOrDefault(m => m.Name == "GetBlockInfo");
        if (setupMethod is null) return [];

        var instructions = setupMethod.Body.Instructions;

        var switchInst = instructions.FirstOrDefault(i => i.OpCode.Code == Code.Switch);
        if (switchInst == null) return [];

        var switchTargets = (Instruction[])switchInst.Operand;

        foreach (var targetInst in switchTargets)
        {
            if (targetInst.OpCode.Code != Code.Newobj) continue;

            var entry = new Dictionary<string, object?>();

            var index = instructions.IndexOf(targetInst);
            for (var j = index; j < instructions.Count; j++)
            {
                var instruction = instructions[j];
                if (instruction.OpCode.Code == Code.Ret) break;
                if (instruction.OpCode.Code != Code.Dup) continue;

                var valueOpcodes = new List<Instruction>();
                var k = j + 1;
                while (k < instructions.Count && instructions[k].OpCode.Code != Code.Stfld)
                {
                    valueOpcodes.Add(instructions[k]);
                    k++;
                }

                if (k >= instructions.Count || instructions[k].Operand is not FieldDefinition fd) continue;

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
                return i;

            if (inst.OpCode.Code != Code.Dup) continue;

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
        if (instructions.Count == 0) return null;

        if (type.IsPrimitive)
            return type.Name switch
            {
                "Boolean" => instructions[0].OpCode.Code == Code.Ldc_I4_1,
                "Single" => Convert.ToSingle(instructions[0].Operand),
                "Byte" => Convert.ToByte(ParseInt(instructions[0])),
                "Int32" => ParseInt(instructions[0]),
                _ => WarnUnhandled(decompiler, type, instructions[0], fieldName)
            };

        if (type.IsArray && type is ArrayType { ElementType.FullName: "System.String" })
            return ParseStringArray(instructions);

        return type.Name switch
        {
            "String" => instructions[0].Operand ?? instructions[0].OpCode.Name,
            "Recognition" or "SleepQuality" => ParseInt(instructions[0]),
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
                    foreach (var (f, vals) in ExtractFields(decompiler, instructions))
                        dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);

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
                    var minimumCondition = ParseInt(instructions[0]);

                    var dict = new Dictionary<string, object?>();
                    foreach (var (f, vals) in ExtractFields(decompiler, instructions))
                        dict[f.Name] = ParseFieldValue(decompiler, f.FieldType, vals, f.Name);

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
                return ParseInt(instructions[0]);

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
                            result.r = (byte)(ParseInt(instructions[0]) * 255);
                            result.g = (byte)(ParseInt(instructions[1]) * 255);
                            result.b = (byte)(ParseInt(instructions[2]) * 255);
                            result.a = 255;
                            break;
                        case 6:
                            result.r = (byte)ParseInt(instructions[0]);
                            result.g = (byte)ParseInt(instructions[1]);
                            result.b = (byte)ParseInt(instructions[2]);
                            result.a = (byte)ParseInt(instructions[3]);
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
                    if (pointerToDelegate is null) return null;

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
            Console.WriteLine($"  {inst}");

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

    private static string[]? GetStringArray(Dictionary<string, object?> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            string[] array => array,
            string text => text.Split(','),
            _ => null
        };
    }

    private static object WarnUnhandled(CSharpDecompiler _, TypeReference type, Instruction inst,
        string fieldName)
    {
        Console.WriteLine($"[WARNING] Unhandled primitive type: {type.Name} for field {fieldName}");
        return inst.Operand ?? inst.OpCode.Name;
    }

    public static Dictionary<FieldDefinition, List<Instruction>> ExtractFields(CSharpDecompiler _,
        List<Instruction> instructions)
    {
        var fields = new Dictionary<FieldDefinition, List<Instruction>>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode.Code != Code.Dup) continue;
            i++;

            var valueOpcodes = new List<Instruction>();
            while (i < instructions.Count && instructions[i].OpCode.Code != Code.Stfld)
                valueOpcodes.Add(instructions[i++]);

            if (i < instructions.Count && instructions[i].Operand is FieldDefinition field)
                fields[field] = valueOpcodes;
        }

        return fields;
    }

    private static int ParseInt(Instruction inst)
    {
        return inst.OpCode.Code switch
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
            _ => inst.Operand is int v ? v : Convert.ToInt32(inst.Operand)
        };
    }

    private static T GetValue<T>(Dictionary<string, object?> dictionary, string key, T defaultValue = default!)
    {
        if (dictionary.TryGetValue(key, out var value) && value is not null) return (T)value;

        return defaultValue;
    }
}