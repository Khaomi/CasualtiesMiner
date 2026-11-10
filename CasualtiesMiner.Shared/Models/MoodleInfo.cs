namespace CasualtiesMiner.Shared.Models;

public sealed class MoodleInfo
{
    public string icon = "";
    public string localeId = "";
    public string? descLocaleKey;
    public string? preconditionForMoodle;
    public int? intensity;
    public string? intensityExpr;
    public bool critical;
    public string? criticalExpr;
    public bool chippedOnly;
}
