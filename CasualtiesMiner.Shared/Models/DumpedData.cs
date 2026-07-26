using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CasualtiesMiner.Shared.Models;

public sealed partial class DumpedData
{
    public ItemInfo[] Items { get; set; } = [];
    public Recipe[] Recipes { get; set; } = [];
    public LiquidType[] Liquids { get; set; } = [];
    public BlockInfo[] Tiles { get; set; } = [];
    public MoodleInfo[] Moodles { get; set; } = [];
    public GameFields Fields { get; set; } = new();
    public BuildingEntity[] Buildings { get; set; } = [];
    
    public static readonly JsonSerializerOptions SerializationOptions = new()
    {
        AllowTrailingCommas = true,
        IncludeFields = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
