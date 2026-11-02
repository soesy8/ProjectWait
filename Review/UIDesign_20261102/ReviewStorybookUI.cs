using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ReviewStorybookUI
{
    const string Root = "Assets/M/UI/Storybook/";
    static BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static object Resolution(int width, int height)
    {
        var editor = typeof(Editor).Assembly;
        var sizesType = editor.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var groupType = editor.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        var gt = group.GetType();
        int builtin = (int)gt.GetMethod("GetBuiltinCount").Invoke(group, null);
        int count = (int)gt.GetMethod("GetTotalCount").Invoke(group, null);
        int found = -1;
        for (int i = 0; i < count; i++)
        {
            var size = gt.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            var t = size.GetType();
            if ((int)t.GetProperty("width", Flags).GetValue(size) == width && (int)t.GetProperty("height", Flags).GetValue(size) == height) { found = i; break; }
        }
        if (found < 0)
        {
            var sizeType = editor.GetType("UnityEditor.GameViewSizeType");
            var type = editor.GetType("UnityEditor.GameViewSize");
            var constructor = type.GetConstructor(Flags, null, new[] { sizeType, typeof(int), typeof(int), typeof(string) }, null);
            var size = constructor.Invoke(new object[] { Enum.Parse(sizeType, "FixedResolution"), width, height, "Storybook UI " + width + "x" + height });
            gt.GetMethod("AddCustomSize").Invoke(group, new[] { size }); found = count;
        }
        var gvType = editor.GetType("UnityEditor.GameView");
        var gv = EditorWindow.GetWindow(gvType);
        var selected = gvType.GetProperty("selectedSizeIndex", Flags);
        if (!SessionState.GetBool("StorybookUI.HasOriginalResolution", false))
        { SessionState.SetInt("StorybookUI.OriginalResolution", (int)selected.GetValue(gv)); SessionState.SetBool("StorybookUI.HasOriginalResolution", true); }
        selected.SetValue(gv, found); gv.Repaint();
        return new { selectedIndex = found, width, height };
    }

    public static object RestoreResolution()
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(type);
        int index = SessionState.GetInt("StorybookUI.OriginalResolution", 0);
        type.GetProperty("selectedSizeIndex", Flags).SetValue(window, index);
        SessionState.EraseBool("StorybookUI.HasOriginalResolution"); window.Repaint(); return new { index };
    }

    public static object Validate()
    {
        Canvas.ForceUpdateCanvases();
        var checks = new List<object>();
        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            text.ForceMeshUpdate();
            if (string.IsNullOrWhiteSpace(text.text)) continue;
            checks.Add(new { path = PathOf(text.transform), text = text.text, width = text.rectTransform.rect.width,
                height = text.rectTransform.rect.height, neededWidth = text.preferredWidth, neededHeight = text.preferredHeight,
                heightFits = text.preferredHeight <= text.rectTransform.rect.height + 1,
                missingGlyphs = text.text.Where(c => !char.IsWhiteSpace(c) && c != '\n').Distinct().Where(c => !text.font.HasCharacter(c, true, false)).Select(c => c.ToString()).ToArray() });
        }
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).Select(path => {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return new { path, canvas = p.GetComponent<Canvas>() != null,
                missingScripts = p.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
                buttons = p.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length,
                cameras = p.GetComponentsInChildren<Canvas>(true).Count(c => c.worldCamera != null) };
        }).ToArray();
        var report = new { texts = checks, prefabs, eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length };
        File.WriteAllText("Review/UIDesign_20261102/validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }

    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

    public static object ExportPNGs()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        Directory.CreateDirectory(Root + "Previews");
        var original = SceneManager.GetActiveScene();
        var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        var files = new List<string>();
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "Prefabs" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), preview);
                go.transform.localScale = Vector3.one; go.transform.position = new Vector3(10000, 0, 0);
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                var rect = (RectTransform)go.transform;
                float w = rect.rect.width + 64, h = rect.rect.height + 100;
                var camera = new GameObject("ExportCamera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.position = new Vector3(10000, -12, -1000);
                camera.orthographic = true; camera.orthographicSize = h * .5f; camera.aspect = w / h;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 30; camera.nearClipPlane = 1; camera.farClipPlane = 2000;
                var canvas = go.GetComponent<Canvas>(); canvas.worldCamera = camera;
                Canvas.ForceUpdateCanvases();
                foreach (var text in go.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                var rt = RenderTexture.GetTemporary((int)w * 2, (int)h * 2, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt; camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = rt;
                var png = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); png.Apply();
                string output = Root + "Previews/" + Path.GetFileNameWithoutExtension(path) + ".png";
                File.WriteAllBytes(output, png.EncodeToPNG()); files.Add(output);
                RenderTexture.active = previous; camera.targetTexture = null;
                Object.DestroyImmediate(png); RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(go); Object.DestroyImmediate(camera.gameObject);
            }
        }
        finally { SceneManager.SetActiveScene(original); EditorSceneManager.CloseScene(preview, true); }
        AssetDatabase.Refresh();
        foreach (var path in files)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 200; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return files;
    }
}
