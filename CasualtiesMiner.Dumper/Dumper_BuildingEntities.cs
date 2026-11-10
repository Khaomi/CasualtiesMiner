using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Dumper.Mappers;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public static BuildingEntity[] DumpBuildingEntities(AssetsParser assetsParser)
    {
        var entityList = new List<BuildingEntity>();

        foreach (var snapshot in assetsParser.ExtractBuildingEntities())
        {
            var entity = BehaviourMapper.MapBuildingEntity(snapshot.Behaviour.baseField);
            entity.id = snapshot.PrefabName;

            if (string.IsNullOrWhiteSpace(entity.id))
                continue;

            entity.spriteName = snapshot.SpriteName;

            if (string.IsNullOrEmpty(entity.spriteName))
            {
                Console.WriteLine($"Warning: item '{entity.id}' has an empty sprite.");
            }
            
            entityList.Add(entity);
        }

        return entityList.ToArray();
    }
}
