namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// A flattened, wiki-ready representation of a building entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
public class BuildingEntityRow
{
    public required string Id { get; init; }
    public required string LocaleId { get; init; }
    public required string SpriteName { get; init; }
    public required double Health { get; init; }
    public required double DropChanceMultiplier { get; init; }
    public required IReadOnlyList<string> ItemsDropOnDestroy { get; init; } = [];
    public required IReadOnlyList<string> AlwaysDrop { get; init; } = [];
    public required IReadOnlyList<string> ItemCategoriesToAdd { get; init; } = [];
    public required int GuaranteedDropAmount { get; init; }
    public required int BlockFootstepSoundId { get; init; }
    public required bool RequireGround { get; init; }
    public required bool SkipDescriptionSet { get; init; }
    public required bool CantHit { get; init; }
    public required bool Animal { get; init; }
    public required bool IgnoreBodyOptimize { get; init; }
    public required bool Metallic { get; init; }
}