using CasualtiesMiner.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CasualtiesMiner.Uploader.Data;

internal static class DataJson
{
    internal static readonly JsonSerializerOptions ReadOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static ItemInfo[] LoadItems(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("items", out var itemsElement))
        {
            throw new InvalidOperationException("data.json has no 'items' array.");
        }

        return itemsElement.Deserialize<ItemInfo[]>(ReadOptions)
               ?? throw new InvalidOperationException("Could not parse items from data.json.");
    }

    internal static LiquidType[] LoadLiquids(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("liquids", out var itemsElement))
        {
            throw new InvalidOperationException("data.json has no 'liquids' array.");
        }

        return itemsElement.Deserialize<LiquidType[]>(ReadOptions)
               ?? throw new InvalidOperationException("Could not parse liquids from data.json.");
    }

    internal static BlockInfo[] LoadBlocks(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("tiles", out var itemsElement))
        {
            throw new InvalidOperationException("data.json has no 'blocks' array.");
        }

        return itemsElement.Deserialize<BlockInfo[]>(ReadOptions)
               ?? throw new InvalidOperationException("Could not parse blocks from data.json.");
    }

    internal static Recipe[] LoadRecipes(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("recipes", out var moodlesElement))
            return [];

        return moodlesElement.Deserialize<Recipe[]>(ReadOptions) ?? [];
    }

    internal static MoodleInfo[] LoadMoodles(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("moodles", out var moodlesElement))
            return [];

        return moodlesElement.Deserialize<MoodleInfo[]>(ReadOptions) ?? [];
    }

    internal static GameFields? LoadFields(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("fields", out var moodlesElement))
            return null;

        return moodlesElement.Deserialize<GameFields>(ReadOptions) ?? null;
    }

    internal static BuildingEntity[] LoadBuildings(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("buildings", out var buildingsElement))
            return [];

        return buildingsElement.Deserialize<BuildingEntity[]>(ReadOptions) ?? [];
    }
}
