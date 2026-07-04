using AssetsTools.NET.Extra;
using AssetsTools.NET;
using CasualtiesMiner.Shared.Models;

namespace CasualtiesMiner.Dumper.Game;

public sealed class AssetsParser : IDisposable
{
    private readonly AssetsManager _manager;
    
    private readonly AssetsFileInstance _resourcesAssets;
    private readonly AssetsFileInstance _globalGameManagers;
    
    private readonly AssetFileInfo _resourceManager;

    public AssetsParser(string gameDataPath)
    {
        _manager = new AssetsManager
        {
            MonoTempGenerator = new MonoCecilTempGenerator(Path.Combine(gameDataPath, "Managed"))
        };

        using var tpk = File.OpenRead("Assets/lz4.tpk");

        _manager.LoadClassPackage(tpk);
        
        _globalGameManagers = _manager.LoadAssetsFile(Path.Combine(gameDataPath, "globalgamemanagers"));
        _manager.LoadClassDatabaseFromPackage(_globalGameManagers.file.Metadata.UnityVersion);
        
        _resourcesAssets = _manager.LoadAssetsFile(Path.Combine(gameDataPath, "resources.assets"));
        _resourceManager = _globalGameManagers.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];
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
        _manager?.UnloadAll(true);
        GC.SuppressFinalize(this);
    }

    ~AssetsParser()
    {
        Dispose();
    }
}
