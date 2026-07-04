using AssetsTools.NET.Extra;
using AssetsTools.NET;

namespace CasualtiesMiner.Dumper.Game;

public sealed class AssetsParser : IDisposable
{
    private readonly AssetsManager _manager;
    
    private readonly AssetsFileInstance? _resourcesAssets;
    private readonly AssetsFileInstance? _globalGameManagers;
    
    private readonly AssetFileInfo? _resourceManager;

    public string GamePath { get; private set; }

    public AssetsManager Manager { get; private set; }

    public AssetsParser(string gameDataPath)
    {
        GamePath = gameDataPath;

        Manager = new AssetsManager
        {
            MonoTempGenerator = new MonoCecilTempGenerator(Path.Combine(gameDataPath, "Managed"))
        };
    }

    public AssetsFileInstance LoadResources()
    {
        using var classPackage = OpenEmbeddedClassPackage();
        Manager.LoadClassPackage(classPackage);

        var assetsPath = Path.Combine(GamePath, "resources.assets");
        _resourcesAssets = Manager.LoadAssetsFile(assetsPath, loadDeps: true);

        Manager.LoadClassDatabaseFromPackage(_resourcesAssets.file.Metadata.UnityVersion);
        
        
        _globalGameManagers = _manager.LoadAssetsFile(Path.Combine(gameDataPath, "globalgamemanagers"));
        _manager.LoadClassDatabaseFromPackage(_globalGameManagers.file.Metadata.UnityVersion);
        
        _resourcesAssets = _manager.LoadAssetsFile(Path.Combine(gameDataPath, "resources.assets"));
        _resourceManager = _globalGameManagers.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];

        return instance;
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
    
    public IEnumerable<AssetTypeValueField> ExtractMonoBehaviours(string behaviourName, bool onlyFromNamedPrefabs = true)
    {
        if (!onlyFromNamedPrefabs)
        {
            return _resourcesAssets.file.GetAssetsOfType(AssetClassID.MonoBehaviour)
                .Where(x =>
                {
                    var script = _manager.GetExtAsset(_resourcesAssets, _manager.GetBaseField(_resourcesAssets, x)["m_Script"]);
                    return script.baseField != null && script.baseField["m_Name"].AsString == behaviourName;
                })
                .Select(x => _manager.GetBaseField(_resourcesAssets, x))
                .ToList();
        }

        var resourceManagerRoot = _manager.GetBaseField(_globalGameManagers, _resourceManager);

        var references = resourceManagerRoot["m_Container.Array"].ToList();
        var monoBehavioursFound = new List<AssetTypeValueField>();

        foreach (var reference in references)
        {
            var assetExt = _manager.GetExtAsset(_globalGameManagers, reference[1]);
            
            if (assetExt.info == null)
                continue;
            
            if (assetExt.info.TypeId == (int)AssetClassID.GameObject)
            {
                // Extract first one we find in the root object's components
                AssetExternal monoBehaviour = default;

                foreach (var componentKeyPptr in _manager.GetBaseField(assetExt.file, assetExt.info)["m_Component.Array"])
                {
                    var componentInstance = _manager.GetExtAsset(assetExt.file, componentKeyPptr[0]);

                    if (componentInstance.info == null || componentInstance.info.TypeId != (int)AssetClassID.MonoBehaviour)
                        continue;

                    var script = _manager.GetExtAsset(_resourcesAssets, componentInstance.baseField["m_Script"]);

                    if (script.baseField == null || script.baseField["m_Name"].AsString != behaviourName)
                        continue;
                    
                    monoBehaviour = componentInstance;
                    break;
                }

                if (monoBehaviour.baseField != null)
                    monoBehavioursFound.Add(monoBehaviour.baseField);
            }
        }

        return monoBehavioursFound;
    }

    public void Dispose()
    {
        Manager?.UnloadAll(true);
        Manager = default;
        GC.SuppressFinalize(this);
    }
}
