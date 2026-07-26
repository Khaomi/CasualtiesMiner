using System.Reflection;
using CasualtiesMiner.Uploader.Data.Mappers;

namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Bucket table definitions. Each entry is a page in the <c>Bucket:</c> namespace whose content is the
/// JSON schema described at https://meta.weirdgloop.org/w/Extension:Bucket.
/// </summary>
internal static class BucketSchemas
{
    /// <summary>
    /// All schema pages keyed by their bucket name (page title is <c>Bucket:{name}</c>).
    /// </summary>
    public static IReadOnlyList<(string Bucket, string Schema)> All()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("BucketSchemas/"))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name);
                using var reader = new StreamReader(stream!);
                return (name.Replace("BucketSchemas/", "").Replace(".json", ""), reader.ReadToEnd());
            })
            .ToList();
    }
}
