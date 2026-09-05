using AssetsTools.NET;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper.Mappers;

public static partial class BehaviourMapper
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
            id = baseField["id"].AsString,
            fullName = baseField["fullName"].AsString, // Requires locale text
            description = baseField["fullName"].AsString, // Requires locale text
            itemsDropOnDestroy = baseField["itemsDropOnDestroy.Array"].Select(MapItemDrop).ToArray(),
            health = baseField["health"].AsFloat,
            requireGround = baseField["requireGround"].AsBool,
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
    
    public static Container MapContainer(AssetTypeValueField baseField)
    {
        return new Container()
        {
            encumberanceMult = baseField["encumberanceMult"].AsFloat,
            itemsVisible = baseField["itemsVisible"].AsBool,
            maxWeight = baseField["maxWeight"].AsFloat,
            maxWeightPerItem = baseField["maxWeightPerItem"].AsFloat,
            tagRestriction = baseField["tagRestriction.Array"].Select(x => x.AsString).ToArray()
        };
    }
}