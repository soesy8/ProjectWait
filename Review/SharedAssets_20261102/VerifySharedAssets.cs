using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ProjectWait.M.Editor;
using ProjectWait.M.Elevator;
using ProjectWait.Shared;
using Object = UnityEngine.Object;

public static class VerifySharedAssets
{
    const string Review = "Review/SharedAssets_20261102/";
    const string Example = "Assets/Shared Assets/Examples/SharedAssets_Showcase.unity";
    static readonly List<object> checks = new List<object>();
    static void Check(bool value, string name) { checks.Add(new { name, passed = value }); if (!value) throw new Exception(name); }
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var i = path.LastIndexOf('/'); Folder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }
    static string Hash(string path)
    {
        using (var hash = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)));
    }
    static string Report(string name)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { checks, count = checks.Count, passed = checks.All(x => (bool)x.GetType().GetProperty("passed").GetValue(x)) }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Review + name + ".json", json); return json;
    }

    public static string CreateFixture()
    {
        checks.Clear();
        var originalScene = SceneManager.GetActiveScene();
        Check(!originalScene.isDirty && !EditorApplication.isPlaying, "Original scene is clean and in Edit Mode");
        SessionState.SetString("SharedAssets.OriginalScene", originalScene.path);
        SessionState.SetBool("SharedAssets.RunInBackground", Application.runInBackground);
        var source = OutlineRendererInstaller.DetectPipeline(originalScene);
        var sourcePaths = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(source), true)
            .Where(p => p.EndsWith(".asset")).ToDictionary(p => p, Hash);
        Folder("Assets/Shared Assets/Examples");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -12);
        camera.orthographic = true; camera.orthographicSize = 5.6f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.90f, .94f, .93f);
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(35, -25, 0);
        RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene, Example);
        var installed = OutlineRendererInstaller.Install(source, scene);
        Check(installed.sceneLinked, "One-click setup supports a saved scene outside Assets/M");
        Check(installed.createdAssets.All(OutlineRendererInstaller.IsOwned), "Generated rendering assets stay inside Shared Assets");
        var shared = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SharedObjectHighlight>(true)).Single();
        var meshes = new List<GameObject>();
        var materialPath = "Assets/Shared Assets/Examples/HighlightDemo.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = new Color(.18f, .43f, .40f); AssetDatabase.CreateAsset(material, materialPath); }
        for (int i = 0; i < 3; i++)
        {
            var group = new GameObject("Highlight Example " + (i + 1));
            group.transform.position = new Vector3((i - 1) * 2.1f, -4.15f, 0);
            var shape = GameObject.CreatePrimitive(i == 0 ? PrimitiveType.Cube : i == 1 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            shape.name = "Mesh (automatically collected)"; shape.transform.SetParent(group.transform, false);
            shape.transform.localScale = Vector3.one * .75f;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            meshes.Add(group);
        }
        shared.objects = meshes.ToArray();
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Shared Assets/UI/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        Check(prefabPaths.Length == 9, "Nine UI prefabs exist in Shared Assets");
        var instances = new List<GameObject>();
        for (int i = 0; i < prefabPaths.Length; i++)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]), scene);
            var rect = (RectTransform)root.transform;
            Check(rect.rect.width > 0 && rect.rect.height > 0 && rect.localScale.x > 0, "Non-zero dimensions: " + root.name);
            var scale = Mathf.Min(3.7f / rect.rect.width, 2.1f / rect.rect.height);
            root.transform.localScale = Vector3.one * scale;
            root.transform.position = new Vector3((i % 3 - 1) * 4.25f, 3.5f - (i / 3) * 2.65f, 0);
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                Check(canvas.renderMode == RenderMode.WorldSpace, "World Space Canvas: " + root.name);
                canvas.worldCamera = camera;
                Check(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() && canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(), "PC/XR raycasters: " + root.name);
            }
            Check(root.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts: " + root.name);
            Check(root.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t => t.font && t.fontSharedMaterial), "Text fonts and materials: " + root.name);
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) Check(button.onClick.GetPersistentEventCount() == 0, "No scenario callbacks in shared prefab: " + button.name);
            instances.Add(root);
        }
        var choice = instances.Single(g => g.name == "ChoiceCard");
        var buttons = choice.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        UnityEventTools.AddPersistentListener(buttons[0].onClick, shared.Show);
        UnityEventTools.AddPersistentListener(buttons[1].onClick, shared.Hide);
        UnityEventTools.AddPersistentListener(buttons[2].onClick, shared.Flash);
        var labels = new[] { "강조 켜기", "강조 끄기", "반짝이기" };
        for (int i = 0; i < buttons.Length; i++) buttons[i].GetComponentInChildren<TMPro.TMP_Text>().text = labels[i];
        var next = instances.Single(g => g.name == "NextButton").GetComponentInChildren<UnityEngine.UI.Button>(true);
        UnityEventTools.AddBoolPersistentListener(next.onClick, shared.SetHovered, true);
        var nextLabel = next.GetComponentInChildren<TMPro.TMP_Text>();
        nextLabel.text = "파란 강조"; nextLabel.fontSize = 18;
        nextLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        foreach (var button in instances.Single(g => g.name == "PausePanel").GetComponentsInChildren<UnityEngine.UI.Button>(true))
            UnityEventTools.AddBoolPersistentListener(button.onClick, shared.SetHovered, false);
        UnityEventTools.AddBoolPersistentListener(instances.Single(g => g.name == "CompletionCard").GetComponentInChildren<UnityEngine.UI.Button>(true).onClick, shared.SetHovered, false);
        var again = OutlineRendererInstaller.Install(installed.pipeline, scene);
        Check(again.pipeline == installed.pipeline && again.createdAssets.Length == 0 && again.addedFeatures == 0, "Repeated setup reuses the pipeline, renderers and feature");
        Check(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SharedObjectHighlight>(true)).Count() == 1, "Repeated setup keeps one shared highlight list");
        Check(sourcePaths.All(p => Hash(p.Key) == p.Value), "Source pipelines, renderers and assets are unchanged");
        var window = EditorWindow.GetWindow<OutlineRendererSetupWindow>();
        window.CreateGUI();
        Check(window.rootVisualElement.Q<UnityEngine.UIElements.Button>("install") != null, "Moved setup window loads its UXML");
        window.Close();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
        EditorSceneManager.OpenScene(Example, OpenSceneMode.Single);
        Application.runInBackground = true;
        return Report("EditModeChecks");
    }

    static void Click(string name)
    {
        var button = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        var rect = (RectTransform)button.transform;
        var data = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        Check(hits.Count > 0 && hits.Any(hit => hit.gameObject.transform.IsChildOf(button.transform) || hit.gameObject == button.gameObject), "Pointer raycast hits " + name);
        var hitObject = hits.First().gameObject;
        Check(ExecuteEvents.ExecuteHierarchy(hitObject, data, ExecuteEvents.pointerClickHandler) != null, "Pointer click is handled: " + name);
    }

    public static string RuntimeChecks()
    {
        checks.Clear();
        Check(Application.isPlaying && Time.frameCount > 3, "Play Mode is advancing");
        var shared = Object.FindFirstObjectByType<SharedObjectHighlight>();
        Check(shared.ActiveTargetCount == 3 && shared.RendererCount == 3, "Connected roots automatically collect three child meshes without colliders");
        Check(ElevatorSilhouetteOutlineFeature.LastTargetCount == 3 && Time.frameCount - ElevatorSilhouetteOutlineFeature.LastFrame < 5, "Render Graph actually renders all three shared highlights");
        var original = shared.objects;
        shared.objects = new[] { original[0], original[0], null, original[0].transform.GetChild(0).gameObject, original[1], original[2] };
        shared.Rebuild();
        Check(shared.ActiveTargetCount == 3 && shared.RendererCount == 3, "Null, duplicate and overlapping roots do not duplicate renderers");
        shared.objects = original; shared.Rebuild();
        Click("Choice1"); Check(ElevatorOutlineTarget.Active.All(t => !t.Visible), "UI event hides all highlights");
        Click("Choice0"); Check(ElevatorOutlineTarget.Active.Count(t => t.Visible) == 3, "UI event shows all highlights");
        Click("DialogueNext"); Check(ElevatorOutlineTarget.Active.All(t => t.Hovered && t.InRange), "UI event switches to the blue highlight state");
        Click("Choice2"); Check(ElevatorOutlineTarget.Active.All(t => t.Width > shared.hoverWidth), "UI event flashes the shared targets");
        shared.SetHovered(false);
        shared.enabled = false; Check(!ElevatorOutlineTarget.Active.Any(t => t && t.transform.IsChildOf(shared.transform)), "Disabling the shared component removes its targets from the renderer registry");
        shared.enabled = true; Check(shared.ActiveTargetCount == 3, "Re-enabling reconstructs the connected targets");
        Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Exactly one EventSystem");
        return Report("PlayModeChecks");
    }

    public static string Restore()
    {
        Application.runInBackground = SessionState.GetBool("SharedAssets.RunInBackground", false);
        var path = SessionState.GetString("SharedAssets.OriginalScene", "");
        if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        return "Restored " + path;
    }
}
