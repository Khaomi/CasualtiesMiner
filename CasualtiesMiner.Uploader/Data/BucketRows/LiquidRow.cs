namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// A flattened, wiki-ready representation of a single liquid entity. All values are already converted to the
/// shape expected by the Bucket schema; the Lua/wikitext generators only serialize this object.
/// </summary>
internal sealed record LiquidRow
{
    public required string LiquidId { get; init; }

    public required string LocaleName { get; init; }

    public required string Color { get; init; }

    public required double ValuePerLiter { get; init; }
    public required double InjectionSickness { get; init; }

    public required bool HealthUsable { get; init; }
    public required bool Injectable { get; init; }
    public required bool LocaleFromItem { get; init; }

    public required IReadOnlyList<string> Qualities { get; init; }
}
