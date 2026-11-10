namespace CasualtiesMiner.Uploader.Data.BucketRows;

/// <summary>
/// Wiki-ready moodle row for <c>Module:Moodle/data</c>.
/// </summary>
internal sealed record MoodleRow
{
    public required string Icon { get; init; }
    public required string LocaleId { get; init; }
    public required string DescLocaleKey { get; init; }
    public required string? PreconditionForMoodle { get; init; }
    public required string? PreconditionDisplay { get; init; }
    public required int? Intensity { get; init; }
    public required string? IntensityBodyFieldId { get; init; }
    public required bool Critical { get; init; }
    public required string? CriticalExpr { get; init; }
    public required bool ChippedOnly { get; init; }
    public required int IconSrcSize { get; init; }
}
