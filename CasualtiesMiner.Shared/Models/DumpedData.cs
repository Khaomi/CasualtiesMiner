namespace CasualtiesMiner.Shared.Models;

public sealed class DumpedData
{
    public ItemInfo[] Items { get; set; } = [];
    public Recipe[] Recipes { get; set; } = [];
    public LiquidType[] Liquids { get; set; } = [];
    public BlockInfo[] Tiles { get; set; } = [];
    public MoodleInfo[] Moodles { get; set; } = [];
    public GameFields Fields { get; set; } = new();
    public BuildingEntity[] Buildings { get; set; } = [];
}
