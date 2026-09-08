using CasualtiesMiner.Shared.Models;
using CasualtiesMiner.Uploader.Data.BucketRows;
using System.Globalization;

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
            Id = building.id,
            LocaleId = building.localeId ?? "MISSING",
            SpriteName = building.spriteName ?? "MISSING",
            Health = building.health,
            RequireGround = building.requireGround,
            SkipDescriptionSet = building.skipDescriptionSet,
            DropChanceMultiplier = building.dropChanceMultiplier,
            GuaranteedDropAmount = building.guaranteedDropAmount,
            ItemsDropOnDestroy = building.itemsDropOnDestroy.Select(MapDrop).ToList(),
            AlwaysDrop = building.alwaysDrop.Select(MapDrop).ToList(),
            ItemCategoriesToAdd = building.itemCategoriesToAdd,
            BlockFootstepSoundId = building.blockFootstepSoundId,
            CantHit = building.cantHit,
            Animal = building.animal,
            IgnoreBodyOptimize = building.ignoreBodyOptimize,
            Metallic = building.metallic
        };

        static string MapDrop(ItemDrop drop) => $"{drop.id}:{drop.chance.ToString(CultureInfo.InvariantCulture)}:{drop.conditionMin.ToString(CultureInfo.InvariantCulture)}:{drop.conditionMax.ToString(CultureInfo.InvariantCulture)}";
    }
}