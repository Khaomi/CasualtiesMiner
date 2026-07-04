using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data.Mappers;

/// <summary>
/// Converts dumped <see cref="BuildingEntity"/> instances into wiki-ready <see cref="BuildingEntityRow"/>s.
/// </summary>
public class BuildingEntityRowMapper
{
    public static BuildingEntityRow Map(BuildingEntity building)
    {
        return new BuildingEntityRow
        {
            ItemsDropOnDestroy = building.itemsDropOnDestroy.Select(MapDrop).ToList(),
            Health = building.health,
            RequireGround = building.requireGround,
            Id = building.id,
            SkipDescriptionSet = building.skipDescriptionSet,
            DropChanceMultiplier = building.dropChanceMultiplier,
            GuaranteedDropAmount = building.guaranteedDropAmount,
            AlwaysDrop = building.alwaysDrop.Select(MapDrop).ToList(),
            ItemCategoriesToAdd = building.itemCategoriesToAdd,
            BlockFootstepSoundId = building.blockFootstepSoundId,
            CantHit = building.cantHit,
            Animal = building.animal,
            IgnoreBodyOptimize = building.ignoreBodyOptimize,
            Metallic = building.metallic
        };
        
        static string MapDrop(ItemDrop drop) => $"{drop.id}:{drop.chance}:{drop.conditionMin}:{drop.conditionMax}";
    }
}