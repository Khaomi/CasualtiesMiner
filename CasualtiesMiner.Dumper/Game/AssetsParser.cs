using AssetsTools.NET.Extra;
using AssetsTools.NET;

namespace CasualtiesMiner.Dumper.Game;

public sealed class AssetsParser : IDisposable
{
    public AssetsFileInstance? ResourcesAssets { get; private set; }
    public AssetsFileInstance? GlobalGameManagers { get; private set; }
    public AssetFileInfo? ResourcesManager { get; private set; }

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

        ResourcesAssets = Manager.LoadAssetsFile(Path.Combine(GamePath, "resources.assets"), loadDeps: true);
        Manager.LoadClassDatabaseFromPackage(ResourcesAssets.file.Metadata.UnityVersion);
        
        GlobalGameManagers = Manager.LoadAssetsFile(Path.Combine(GamePath, "globalgamemanagers"), loadDeps: true);
        ResourcesManager = GlobalGameManagers.file.GetAssetsOfType(AssetClassID.ResourceManager)[0];

        return ResourcesAssets;
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
        if (ResourcesAssets == null || GlobalGameManagers == null || ResourcesManager == null)
        {
            Console.WriteLine("Cannot extract monobehaviours, call LoadResources() first.");
            return [];
        }
        
        if (!onlyFromNamedPrefabs)
        {
            return ResourcesAssets.file.GetAssetsOfType(AssetClassID.MonoBehaviour)
                .Where(x =>
                {
                    var script = Manager.GetExtAsset(ResourcesAssets, Manager.GetBaseField(ResourcesAssets, x)["m_Script"]);
                    return script.baseField != null && script.baseField["m_Name"].AsString == behaviourName;
                })
                .Select(x => Manager.GetBaseField(ResourcesAssets, x))
                .ToList();
        }

        var resourceManagerRoot = Manager.GetBaseField(GlobalGameManagers, ResourcesManager);

        var references = resourceManagerRoot["m_Container.Array"].ToList();
        var monoBehavioursFound = new List<AssetTypeValueField>();

        foreach (var reference in references)
        {
            var assetExt = Manager.GetExtAsset(GlobalGameManagers, reference[1]);
            
            if (assetExt.info == null)
                continue;
            
            if (assetExt.info.TypeId == (int)AssetClassID.GameObject)
            {
                // Extract first one we find in the root object's components
                AssetExternal monoBehaviour = default;

                foreach (var componentKeyPptr in Manager.GetBaseField(assetExt.file, assetExt.info)["m_Component.Array"])
                {
                    var componentInstance = Manager.GetExtAsset(assetExt.file, componentKeyPptr[0]);

                    if (componentInstance.info == null || componentInstance.info.TypeId != (int)AssetClassID.MonoBehaviour)
                        continue;

                    var script = Manager.GetExtAsset(ResourcesAssets, componentInstance.baseField["m_Script"]);

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
