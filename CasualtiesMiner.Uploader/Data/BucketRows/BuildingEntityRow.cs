namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// A flattened, wiki-ready representation of a building entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
public class BuildingEntityRow
{
    public IReadOnlyList<string> ItemsDropOnDestroy { get; init; } = [];
    public double Health { get; init; }
    public bool RequireGround { get; init; }
    public string Id { get; init; }
    public bool SkipDescriptionSet { get; init; }
    public double DropChanceMultiplier { get; init; }
    public int GuaranteedDropAmount { get; init; }
    public IReadOnlyList<string> AlwaysDrop { get; init; } = [];
    public IReadOnlyList<string> ItemCategoriesToAdd { get; init; } = [];
    public int BlockFootstepSoundId { get; init; }
    public bool CantHit { get; init; }
    public bool Animal { get; init; }
    public bool IgnoreBodyOptimize { get; init; }
    public bool Metallic { get; init; }
}