using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data.Mappers;

/// <summary>
/// Converts dumped <see cref="RecipeItemRow"/> instances into wiki-ready <see cref="RecipeItemRow"/>s.
/// </summary>
internal static class RecipeItemRowMapper
{
    public static RecipeItemRow[] Map(Recipe recipe)
    {
        var recipeItem = recipe.isRepair
                ? string.Concat("repaired", recipe.result.id)
                : recipe.result.id;
        recipeItem = recipe.result.isLiquid
                ? string.Concat(recipeItem, "liquid")
                : recipeItem;

        var itemsList = new List<RecipeItemRow>();

        foreach (var item in recipe.items)
        {
            itemsList.Add(new()
            {
                RecipeId = recipeItem,
                SpecificId = string.IsNullOrEmpty(item.specificId) ? "" : item.specificId,
                IgnoredId = recipe.isRepair ? "" : recipe.result.id,
                Quality = item.quality is null ? [] : MapQualities([item.quality]),
                MinimumCondition = (double)(decimal)item.minimumCondition,
                Specific = !string.IsNullOrEmpty(item.specificId),
                DestroyItem = item.destroyItem,
                IsLiquid = item.isLiquid,
            });
        }

        return itemsList.ToArray();

        // add this
        // for (int i = 0; i < recipes.Count; i++)
        // {
        //     recipes[i].index = i;
        //     foreach (RecipeItem item in recipes[i].items)
        //     {
        //         if (!string.IsNullOrEmpty(item.specificId))
        //         {
        //             item.specific = true;
        //         }
        //         item.ignoredId = (recipes[i].isRepair ? "" : recipes[i].result.id);
        //     }
        // }
    }

    private static string[] MapQualities(List<CraftingQuality>? qualities)
    {
        if (qualities is null || qualities.Count == 0)
        {
            return [];
        }

        return qualities
            .Where(q => !string.IsNullOrWhiteSpace(q.id))
            .Select(q => Math.Abs(q.amount - 1f) < 0.00001f ? q.id : $"{q.id}:{Format(q.amount)}")
            .ToArray();
    }

    private static string Format(float value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
