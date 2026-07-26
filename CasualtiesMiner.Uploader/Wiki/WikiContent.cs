namespace CasualtiesMiner.Uploader.Wiki;

using System.Text;

/// <summary>
/// Hand-authored wiki pages (Lua router, locale resolver, trigger page).
/// Generated pages: <c>Module:Item/data</c>, <c>Module:Locale/&lt;lang&gt;/*</c>.
/// </summary>
internal static class WikiContent
{
    public static string MakeDataModuleTitle(string name) => $"Module:{name}/data";
    public static string MakeTriggerPageTitle(string name) => $"Project:{name} data";

    public static string MakeTriggerPage(string moduleName, string targetBucket) =>
        """
        This page stores data from "{moduleName}" into "{targetBucket}" [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.
        
        {{#invoke:BucketInsert|main|{moduleName}|{targetBucket}}}
        """
            .Replace("{moduleName}", moduleName)
            .Replace("{targetBucket}", targetBucket);
}