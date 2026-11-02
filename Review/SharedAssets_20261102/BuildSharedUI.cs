using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildSharedUI
{
    const string Root = "Assets/Shared Assets/UI";
    static readonly Dictionary<string, string> paths = new Dictionary<string, string>();
    static readonly Dictionary<Object, Object> references = new Dictionary<Object, Object>();

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        Folder(path.Substring(0, split));
        AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
    }

    static void Copy(string source, string destination)
    {
        if (!AssetDatabase.LoadMainAssetAtPath(destination) && !AssetDatabase.CopyAsset(source, destination))
            throw new Exception("Cannot copy " + source);
        paths[source] = destination;
    }

    static long LocalId(Object obj)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long id);
        return id;
    }

    static void Remap(Object obj)
    {
        var serialized = new SerializedObject(obj);
        var property = serialized.GetIterator();
        bool changed = false;
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
            var original = property.objectReferenceValue;
            if (original && references.TryGetValue(original, out var replacement) && replacement != original)
            {
                property.objectReferenceValue = replacement;
                changed = true;
            }
        }
        if (changed) { serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(obj); }
    }

    public static string Main()
    {
        paths.Clear(); references.Clear();
        foreach (var child in new[] { "Art", "Fonts", "Shaders", "Prefabs" }) Folder(Root + "/" + child);
        var sources = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/M/UI/Storybook/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        if (sources.Length != 9) throw new Exception("Expected the nine current Storybook UI templates.");
        foreach (var source in AssetDatabase.GetDependencies(sources, true))
        {
            if (source.StartsWith("Assets/M/UI/Storybook/Art/Cartoon/")) Copy(source, Root + "/Art/" + Path.GetFileName(source));
            else if (source.StartsWith("Assets/M/Arts/Fonts/")) Copy(source, Root + "/Fonts/" + Path.GetFileName(source));
        }
        var shaderPath = Root + "/Shaders/SharedUIText.shader";
        Copy("Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc", Root + "/Shaders/TMPro_Properties.cginc");
        Copy("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader", shaderPath);
        var shaderSource = File.ReadAllText(shaderPath).Replace("Shader \"TextMeshPro/Mobile/Distance Field\"", "Shader \"ProjectWait/Shared/UI Text\"");
        File.WriteAllText(shaderPath, shaderSource);
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
        foreach (var pair in paths)
        {
            var copies = AssetDatabase.LoadAllAssetsAtPath(pair.Value);
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(pair.Key))
            {
                var copy = copies.FirstOrDefault(o => o && o.GetType() == original.GetType() && LocalId(o) == LocalId(original));
                if (!copy) copy = copies.FirstOrDefault(o => o && o.GetType() == original.GetType() && o.name == original.name);
                if (copy) references[original] = copy;
            }
        }
        foreach (var destination in paths.Values.Distinct())
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(destination))
                if (asset && !(asset is MonoScript) && !(asset is Shader) && !(asset is Texture2D) && !(asset is Sprite)) Remap(asset);

        var summary = new List<object>();
        foreach (var source in sources)
        {
            var destination = Root + "/Prefabs/" + Path.GetFileName(source);
            Copy(source, destination);
            var root = PrefabUtility.LoadPrefabContents(destination);
            try
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true)) if (component) Remap(component);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = null;
                PrefabUtility.SaveAsPrefabAsset(root, destination);
                summary.Add(new { path = destination, scale = root.transform.localScale.ToString(), texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.name).ToArray(), buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b => b.name).ToArray() });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        var external = AssetDatabase.GetDependencies(sources.Select(s => Root + "/Prefabs/" + Path.GetFileName(s)).ToArray(), true)
            .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/Shared Assets/")).ToArray();
        if (external.Length != 0) throw new Exception("Shared UI still has project-local dependencies: " + string.Join(", ", external));
        var report = Newtonsoft.Json.JsonConvert.SerializeObject(new { prefabs = summary, copiedSupportAssets = paths.Count - sources.Length, externalProjectDependencies = external }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Review/SharedAssets_20261102/UIAssets.json", report);
        return report;
    }
}
