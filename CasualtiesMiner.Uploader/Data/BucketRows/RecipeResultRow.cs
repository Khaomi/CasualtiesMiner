namespace CasualtiesMiner.Uploader.Data.BucketRows;

internal sealed record RecipeResultRow
{
    public required string RecipeId { get; init; }
    public required string Id { get; init; }

    public required int Amount { get; init; }

    public required double ResultCondition { get; init; }

    public required bool IsLiquid { get; init; }
    public required bool DontDrainResultLiquid { get; init; }
}
