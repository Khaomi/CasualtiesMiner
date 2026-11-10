using CasualtiesMiner.Shared.Models;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil.Cil;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public Recipe[] DumpRecipes(CSharpDecompiler decompiler)
    {
        var recipeList = new List<Recipe>();

        var recipesType = _module.Types.FirstOrDefault(t => t.FullName == "Recipes");
        var recipeType = _module.Types.FirstOrDefault(t => t.FullName == "Recipe");

        if (recipesType is null || recipeType is null)
        {
            return [];
        }

        var setupMethod = recipesType.Methods.FirstOrDefault(m => m.Name == "SetUpRecipes");
        var globalField = recipesType.Fields.FirstOrDefault(m => m.Name == "recipes");

        if (setupMethod is null || globalField is null)
        {
            return [];
        }

        //ILInstructionFormat.WriteMethodIl(Console.Out, setupMethod, markAddMoodleCalls: true);
        //Console.WriteLine();

        var recipeCtor = recipeType.Methods.First(m => m.IsConstructor);
        var instructions = setupMethod.Body.Instructions;

        for (var i = 0; i < instructions.Count - 1; i++)
        {
            if (instructions[i].OpCode.Code != Code.Ldsfld || instructions[i].Operand != globalField)
            {
                continue;
            }
            if (instructions[i + 1].OpCode.Code != Code.Newobj || instructions[i + 1].Operand != recipeCtor)
            {
                continue;
            }

            var recipeDict = new Dictionary<string, object?>();

            i = ParseObjectFields(decompiler, instructions, i + 1, ["Recipe"], recipeDict,
                op => op == "System.Void System.Collections.Generic.List`1<Recipe>::Add(!0)");

            var recipe = new Recipe
            {
                specialKnown = GetValue<bool>(recipeDict, "specialKnown"),
                INT = GetValue<int>(recipeDict, "INT"),
                items = recipeDict.TryGetValue("items", out var itemsObj) && itemsObj is List<object?> list
                    ? [.. list.Cast<RecipeItem>()]
                    : [],
                result = GetValue<RecipeResult>(recipeDict, "result"),
                category = GetValue<RecipeCategory>(recipeDict, "category"),
                isRepair = GetValue<bool>(recipeDict, "isRepair"),
                index = GetValue<int>(recipeDict, "index")
            };

            recipeList.Add(recipe);
        }

        return [.. recipeList];
    }
}
