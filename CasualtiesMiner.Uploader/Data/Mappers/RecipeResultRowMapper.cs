using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data.Mappers;

internal sealed class RecipeResultRowMapper
{
    public static RecipeResultRow Map(Recipe recipe)
    {
        return new()
        {
            RecipeId = RecipeRowMapper.GetRecipeId(recipe),
            Id = recipe.result.id,
            Amount = recipe.result.amount,
            ResultCondition = (double)(decimal)recipe.result.resultCondition,
            IsLiquid = recipe.result.isLiquid,
            DontDrainResultLiquid = recipe.result.dontDrainResultLiquid,
        };
    }
}
