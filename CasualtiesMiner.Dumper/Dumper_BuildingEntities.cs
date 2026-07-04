using CasualtiesMiner.Dumper.Game;
using CasualtiesMiner.Dumper.Parsing;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper;

public sealed partial class Dumper
{
    public BuildingEntity[] DumpBuildingEntities(AssetsParser assetsParser)
    {
        var behaviours = assetsParser.ExtractMonoBehaviours("BuildingEntity");
        return behaviours.Select(BehaviourMapper.MapBuildingEntity).ToArray();
    }
}
