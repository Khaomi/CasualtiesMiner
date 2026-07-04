using CasualtiesMiner.Uploader.Data.BucketRows;
using CasualtiesMiner.Uploader.Data.Enums;

namespace CasualtiesMiner.Uploader.Data;

internal readonly record struct LocaleModuleEntry(string Key, string LocaleKey, GameObjectType Type)
{
    public static LocaleModuleEntry Create(string key, GameObjectType type) => new(key, key, type);

    public static LocaleModuleEntry CreateFromLiquid(LiquidRow row) => new(row.LiquidId, row.LocaleName, GameObjectType.Liquid);
}
