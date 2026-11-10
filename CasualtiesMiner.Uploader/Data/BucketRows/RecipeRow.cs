using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// A flattened, wiki-ready representation of a single recipe entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
internal sealed record RecipeRow
{
    public required string RecipeId { get; init; }

    [Obsolete("Dosen't make sense to use it because there's separate mapper")]
    public List<RecipeItem> Items { get; init; }
    [Obsolete("Dosen't make sense to use it because there's separate mapper")]
    public RecipeResult Result { get; init; }
    public required string Category { get; init; }

    public required int Intelligence { get; init; }
    public required int Index { get; init; }

    [Obsolete("Dosen't make sense to use it outside the game")]
    public bool HasMadeBefore { get; init; }
    public required bool IsRepair { get; init; }
}
