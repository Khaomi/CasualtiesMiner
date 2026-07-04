using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Uploader.Data.BucketRows;

internal sealed record BlockRow
{
    public required string Name { get; set; }
    public required string? Hitsound { get; set; }
    public required string Stepsound { get; set; }
    public required double Health { get; set; }
    public required double Toxicity { get; set; }
    public required bool NoVariation { get; set; }
    public required bool Metallic { get; set; }
    public required bool Slippery { get; set; }

    public required SleepQuality SleepQuality { get; set; }
}
