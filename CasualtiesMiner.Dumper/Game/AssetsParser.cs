using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace CasualtiesMiner.Dumper.Game;

public readonly record struct PrefabBuildingEntitySnapshot(
    string PrefabName,
    AssetExternal Behaviour,
    string SpriteName
);

public readonly record struct PrefabItemSnapshot(
    string PrefabName,
    AssetExternal Container,
    AssetExternal GunScript,
    string SpriteName
);

public sealed class AssetsParser : IDisposable
{
    public string GamePath { get; }
    public AssetsManager Manager { get; private set; }
    
    public AssetsParser(string gameDataPath)
    {
        GamePath = gameDataPath;
        Manager = new AssetsManager
        {
            MonoTempGenerator = new MonoCecilTempGenerator(Path.Combine(gameDataPath, "Managed"))
        };
        
        using var classPackage = OpenEmbeddedClassPackage();
        Manager.LoadClassPackage(classPackage);
        Manager.LoadClassDatabaseFromPackage(ResourcesAssets.file.Metadata.UnityVersion);
    }
    
    public IEnumerable<PrefabBuildingEntitySnapshot> ExtractBuildingEntities()
    {
        var snapshots = new Dictionary<string, PrefabBuildingEntitySnapshot>();

        foreach (var item in ResourceManagerItems)
        {
            var prefab = Manager.GetExtAsset(GlobalGameManagers, item[1]);

            if (prefab.info == null || prefab.info.TypeId != (int)AssetClassID.GameObject)
            {
                continue;
            }
                
            if (!TryFindBehaviour(prefab, "BuildingEntity", out var behaviour))
            {
                continue;
            }

            if (snapshots.ContainsKey(item[0].AsString))
            {
                Console.WriteLine($"Warn: found a duplicate BuildingEntity with prefab ID {item[0].AsString}.");
                continue;
            }

            var spriteName = TryGetSpriteName(prefab, out var name) ? name : string.Empty;
            snapshots.Add(item[0].AsString, new PrefabBuildingEntitySnapshot(item[0].AsString, behaviour, spriteName));
        }

        return snapshots.Values;
    }
    
    public IEnumerable<PrefabItemSnapshot> ExtractItems()
    {
        var snapshots = new Dictionary<string, PrefabItemSnapshot>();

        foreach (var item in ResourceManagerItems)
        {
            var prefab = Manager.GetExtAsset(GlobalGameManagers, item[1]);

            if (prefab.info == null || prefab.info.TypeId != (int)AssetClassID.GameObject)
            {
                continue;
            }

            _ = TryFindBehaviour(prefab, "Container", out var container);
            _ = TryFindBehaviour(prefab, "GunScript", out var gunScript);

            if (snapshots.ContainsKey(item[0].AsString))
            {
                Console.WriteLine($"Warn: found a duplicate item with prefab ID {item[0].AsString}.");
                continue;
            }

            var spriteName = TryGetSpriteName(prefab, out var name) ? name : string.Empty;
            snapshots.Add(item[0].AsString, new PrefabItemSnapshot(item[0].AsString, container, gunScript, spriteName));
        }

        return snapshots.Values;
    }

    public string ExtractSprite(string objectName)
    {
        if (TryLoadResource(objectName, AssetClassID.GameObject, out var resource))
        {
            return TryGetSpriteName(resource, out var name) ? name : string.Empty;
        }

        return string.Empty;
    }
    
    private static Stream OpenEmbeddedClassPackage()
    {
        var assembly = typeof(AssetsParser).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(static n => n.EndsWith("lz4.tpk", StringComparison.OrdinalIgnoreCase));

        return resourceName is null
            ? throw new InvalidOperationException(
                "Embedded class package not found. Add Assets/lz4.tpk as EmbeddedResource in CasualtiesMiner.Dumper.csproj.")
            : assembly.GetManifestResourceStream(resourceName)
              ?? throw new InvalidOperationException($"Failed to open embedded resource '{resourceName}'.");
    }

    private AssetsFileInstance ResourcesAssets =>
        Manager.LoadAssetsFile(Path.Combine(GamePath, "resources.assets"), loadDeps: true);

    private AssetsFileInstance GlobalGameManagers =>
        Manager.LoadAssetsFile(Path.Combine(GamePath, "globalgamemanagers"), loadDeps: true);

    private AssetFileInfo ResourcesManager =>
        field ??= GlobalGameManagers.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];

    private List<AssetTypeValueField> ResourceManagerItems
    {
        get
        {
            if (field != null)
                return field;
            
            var resourceManagerRoot = Manager.GetBaseField(GlobalGameManagers, ResourcesManager);
            return field = resourceManagerRoot["m_Container.Array"].ToList();
        }
    }

    /// <summary>
    /// Loads a game resource by type similarly to <c>Resources.Load&lt;T&gt;(string path)</c>.
    /// </summary>
    /// <param name="path">Path to asset, similar to how the game uses it.</param>
    /// <param name="assetType">The type of the asset to load.</param>
    /// <param name="result">The resulting asset, if found.</param>
    /// <returns>True if such an asset was found, false otherwise.</returns>
    private bool TryLoadResource(string path, AssetClassID assetType, out AssetExternal result)
    {
        foreach (var item in ResourceManagerItems)
        {
            if (item[0].AsString != path)
                continue;
            
            var assetExt = Manager.GetExtAsset(GlobalGameManagers, item[1]);

            if (assetExt.info == null || assetExt.info.TypeId != (int)assetType)
            {
                continue;
            }

            result = assetExt;
            return true;
        }

        result = default;
        return false;
    }
    
    private IEnumerable<AssetExternal> GetComponentsOfType(AssetExternal asset, AssetClassID componentType)
    {
        foreach (var componentKeyPptr in asset.baseField["m_Component.Array"])
        {
            var component = Manager.GetExtAsset(asset.file, componentKeyPptr["component"]);

            if (component.info == null || component.info.TypeId != (int)componentType)
            {
                continue;
            }

            yield return component;
        }
    }

    private bool TryFindBehaviour(AssetExternal asset, string behaviourName, out AssetExternal behaviour)
    {
        foreach (var component in GetComponentsOfType(asset, AssetClassID.MonoBehaviour))
        {
            var script = Manager.GetExtAsset(ResourcesAssets, component.baseField["m_Script"]);

            if (script.baseField == null || script.baseField["m_Name"].AsString != behaviourName)
            {
                continue;
            }

            behaviour = component;
            return true;
        }
        
        behaviour = default;
        return false;
    }

    private bool TryGetSpriteName(AssetExternal asset, out string spriteName)
    {
        foreach (var component in GetComponentsOfType(asset, AssetClassID.SpriteRenderer))
        {
            var spriteExtInfo = Manager.GetExtAsset(asset.file, component.baseField["m_Sprite"]);

            if (spriteExtInfo.info == null)
            {
                continue;
            }

            spriteName = spriteExtInfo.baseField["m_Name"].AsString;
            return true;
        }

        spriteName = string.Empty;
        return false;
    }

    public void Dispose()
    {
        Manager?.UnloadAll(true);
        Manager = default!;

        GC.SuppressFinalize(this);
    }

    ~AssetsParser()
    {
        Manager?.UnloadAll(true);
        Manager = default!;
    }
}
