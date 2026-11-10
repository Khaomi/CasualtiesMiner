using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data.Mappers;

internal sealed class RecipeResultRowMapper
{
    public static RecipeResultRow Map(Recipe recipe)
    {
        var recipeItem = recipe.isRepair
                ? string.Concat("repaired", recipe.result.id)
                : recipe.result.id;
        recipeItem = recipe.result.isLiquid
                ? string.Concat(recipeItem, "liquid")
                : recipeItem;

        return new()
        {
            RecipeId = recipeItem,
            Id = recipe.result.id,
            Amount = recipe.result.amount,
            ResultCondition = (double)(decimal)recipe.result.resultCondition,
            IsLiquid = recipe.result.isLiquid,
            DontDrainResultLiquid = recipe.result.dontDrainResultLiquid,
        };
    }
}
