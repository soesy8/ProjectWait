using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class ProjectReviewAudit
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before the scene audit.");
        var before = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (before.isDirty) throw new Exception("Current scene has unsaved edits.");
        var paths = new[] {
            "Assets/M/Scene/Elevator_Safety_Prototype.unity",
            "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity",
            "Assets/Test/Scenes/WetCorridor_Test.unity",
            "Assets/Test/Scenes/WetCorridor_Friend_Test.unity",
            "Assets/Test/Scenes/Friend_Animation_Test.unity",
            "Assets/Scenes/New Scene.unity"
        };
        var scenes = new List<object>();
        foreach (string path in paths)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var missingScripts = transforms.Where(t => t.GetComponents<Component>().Any(c => c == null)).Select(t => t.name).ToArray();
            var guidMatches = Regex.Matches(File.ReadAllText(path), @"guid: ([a-f0-9]{32})").Cast<Match>().Select(m => m.Groups[1].Value).Distinct();
            var missingGuids = guidMatches.Where(g => !g.StartsWith("0000000000000000") && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var emptyMaterials = transforms.SelectMany(t => t.GetComponents<Renderer>()).Where(r => r.sharedMaterials.Any(m => m == null)).Select(r => r.name).ToArray();
            scenes.Add(new { path, gameObjects = transforms.Length, missingScripts, missingGuids, emptyMaterials,
                cameras = transforms.SelectMany(t => t.GetComponents<Camera>()).Select(c => new { c.name, c.enabled }).ToArray() });
        }
        var buildScenes = EditorBuildSettings.scenes.Select(s => new { s.path, s.enabled, exists = File.Exists(s.path) }).ToArray();
        var shaders = new[] { "ProjectWait/Storybook Toon", "ProjectWait/Illustrated Water" }.Select(name => {
            var shader = Shader.Find(name); return new { name, exists = shader != null, hasError = shader && ShaderUtil.ShaderHasError(shader) };
        }).ToArray();
        EditorSceneManager.OpenScene("Assets/Test/Scenes/WetCorridor_Test.unity");
        var report = new { date = DateTime.Now, scenes, buildScenes, shaders, limits = "Unity Editor desktop play review. No physical XR hardware or player build verification." };
        File.WriteAllText("Review/20261102/audit.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
