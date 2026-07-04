using AssetsTools.NET;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public static class BehaviourMapper
{
    public static ItemDrop MapItemDrop(AssetTypeValueField baseField)
    {
        return new ItemDrop()
        {
            id = baseField["id"].AsString,
            chance = baseField["chance"].AsFloat,
            conditionMax = baseField["conditionMax"].AsFloat,
            conditionMin = baseField["conditionMin"].AsFloat
        };
    }
    
    public static BuildingEntity MapBuildingEntity(AssetTypeValueField baseField)
    {
        return new BuildingEntity
        {
            itemsDropOnDestroy = baseField["itemsDropOnDestroy.Array"].Select(MapItemDrop).ToArray(),
            health = baseField["health"].AsFloat,
            requireGround = baseField["requireGround"].AsBool,
            id = baseField["id"].AsString,
            fullName = baseField["fullName"].AsString, // Requires locale text
            description = baseField["fullName"].AsString, // Requires locale text
            skipDescriptionSet = baseField["skipDescriptionSet"].AsBool,
            dropChanceMultiplier = baseField["dropChanceMultiplier"].AsFloat,
            guaranteedDropAmount = baseField["guaranteedDropAmount"].AsInt,
            alwaysDrop = baseField["alwaysDrop.Array"].Select(MapItemDrop).ToArray(),
            itemCategoriesToAdd = baseField["itemCategoriesToAdd.Array"].Select(x => x.AsString).ToArray(),
            blockFootstepSoundId = baseField["blockFootstepSoundId"].AsUShort,
            cantHit = baseField["cantHit"].AsBool,
            animal = baseField["animal"].AsBool,
            ignoreBodyOptimize = baseField["ignoreBodyOptimize"].AsBool,
            metallic = baseField["metallic"].AsBool,
        };
    }
}