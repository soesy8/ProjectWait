var paths = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" })
    .Select(AssetDatabase.GUIDToAssetPath)
    .Concat(new[] {
        "Assets/M/Arts/Fonts/ElevatorKorean.asset",
        "Assets/M/Arts/Fonts/ElevatorReadable.asset"
    })
    .Distinct()
    .OrderBy(path => path)
    .ToArray();
var large = new List<object>();
var records = new List<object>();
var textureCount = 0;
foreach (var path in paths)
{
    foreach (var texture in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture>())
    {
        textureCount++;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var sourceWidth = 0;
        var sourceHeight = 0;
        if (importer != null)
            importer.GetSourceTextureWidthAndHeight(out sourceWidth, out sourceHeight);
        string assetGuid;
        long localFileId;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out assetGuid, out localFileId);
        var entry = new {
            path,
            name = texture.name,
            type = texture.GetType().Name,
            width = texture.width,
            height = texture.height,
            sourceWidth,
            sourceHeight,
            maxTextureSize = importer == null ? 0 : importer.maxTextureSize,
            runtimeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture),
            guid = assetGuid,
            fileId = localFileId
        };
        records.Add(entry);
        if (texture.width >= 2048 || texture.height >= 2048)
            large.Add(entry);
    }
}
var report = new {
    project = Application.dataPath,
    buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
    importedTexturesScanned = textureCount,
    largeTextures = large,
    textures = records
};
var output = "Assets/M/CodexTemp/TextureAudit_Unity_20261029.json";
System.IO.File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return new { output, importedTexturesScanned = textureCount, largeTexturesFound = large.Count };
