using CasualtiesMiner.Uploader.Data.BucketRows;

namespace CasualtiesMiner.Uploader.Data;

internal class DataRows
{
    public required List<ItemRow> Items { get; set; }
    public required List<LiquidRow> Liquids { get; set; }
    public required List<BlockRow> Tiles { get; set; }
    public required List<RecipeItemRow> RecipeItems { get; set; }
    public required List<RecipeResultRow> RecipeResults { get; set; }
    public required List<RecipeRow> Recipes { get; set; }
    public required List<MoodleRow> Moodles { get; set; }
    public required List<GameFieldRow> GameFields { get; set; }
    public required List<BodyFieldRow> BodyFields { get; set; }
    public required List<BuildingEntityRow> BuildingEntities { get; set; }
}