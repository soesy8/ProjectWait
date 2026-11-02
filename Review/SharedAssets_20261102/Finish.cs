using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWait.M.Editor;
using ProjectWait.Shared;

public static class FinishSharedAssets
{
    public static async Task<string> SaveExample()
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit Mode required.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var shared = UnityEngine.Object.FindFirstObjectByType<SharedObjectHighlight>();
        var originals = shared.objects.ToArray();
        var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == "DialogueNext");
        var label = button.GetComponentInChildren<TMPro.TMP_Text>();
        label.text = "파란 강조"; label.fontSize = 18; label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        var window = EditorWindow.GetWindow<OutlineRendererSetupWindow>();
        window.Show(); await Task.Delay(200);
        var install = window.rootVisualElement.Q<UnityEngine.UIElements.Button>("install");
        if (install == null || !install.enabledInHierarchy) throw new Exception("Setup button unavailable.");
        install.Focus();
        using (var ev = NavigationSubmitEvent.GetPooled()) { ev.target = install; install.SendEvent(ev); }
        await Task.Delay(100);
        if (window.LastResult == null || !window.LastResult.sceneLinked) throw new Exception("One-click button did not configure the scene.");
        if (!shared.objects.SequenceEqual(originals)) throw new Exception("Setup changed the connected objects.");
        if (Selection.activeGameObject != shared.gameObject) throw new Exception("Setup did not select the highlight list.");
        window.Close();
        var shaderPaths = new[] { "Assets/Shared Assets/UI/Shaders/SharedUIText.shader", "Assets/Shared Assets/Highlight/Shaders/ElevatorOutlineCoverage.shader", "Assets/Shared Assets/Highlight/Shaders/ElevatorOutlineDistance.shader" };
        foreach (var path in shaderPaths)
            if (ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(path))) throw new Exception("Shader error: " + path);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { oneClickButton = true, objectsPreserved = true, highlightListSelected = true, shaderErrors = 0, shaderPaths }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Review/SharedAssets_20261102/FinalChecks.json", json);
        return json;
    }

    public static string Restore()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        const string preview = "Assets/Shared Assets/Examples/SharedAssets_Preview.png";
        if (!AssetDatabase.LoadMainAssetAtPath(preview))
            AssetDatabase.CopyAsset("Assets/Review/SharedAssets_20261102/SharedAssets_Showcase.png", preview);
        Application.runInBackground = SessionState.GetBool("SharedAssets.RunInBackground", false);
        var path = SessionState.GetString("SharedAssets.OriginalScene", "");
        if (string.IsNullOrEmpty(path)) throw new Exception("Original scene not recorded.");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var missing = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if (missing != 0) throw new Exception("Original scene has missing scripts: " + missing);
        return "Restored original scene with no missing scripts: " + path;
    }
}
