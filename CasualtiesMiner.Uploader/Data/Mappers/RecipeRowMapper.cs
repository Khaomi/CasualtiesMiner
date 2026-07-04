using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Text.Json;

namespace CasualtiesMiner.Uploader.Data.Mappers;

internal static class RecipeRowMapper
{
    private static readonly string[] Categories =
        ["materials", "tools", "medicine", "utilities", "food"];

    public static RecipeRow Map(Recipe recipe)
    {
        var recipeItem = recipe.isRepair
                ? string.Concat("repaired", recipe.result.id)
                : recipe.result.id;
        recipeItem = recipe.result.isLiquid
                ? string.Concat(recipeItem, "liquid")
                : recipeItem;

        return new RecipeRow
        {
            RecipeId = recipeItem,
            Items = recipe.items,
            Result = recipe.result,
            Category = NormalizeCategory(recipe.category),
            Intelligence = recipe.INT,
            Index = recipe.index,
            HasMadeBefore = recipe.hasMadeBefore,
            IsRepair = recipe.isRepair
        };
    }

    private static string NormalizeCategory(RecipeCategory category)
    {
        var id = JsonNamingPolicy.CamelCase.ConvertName(category.ToString());
        return Categories.Contains(id) ? id : "materials";
    }
}
