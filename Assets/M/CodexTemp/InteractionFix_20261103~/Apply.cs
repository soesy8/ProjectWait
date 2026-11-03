using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class ApplyInteractionFix
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play Mode and preserve unsaved scene changes before applying.");
        var original = SceneManager.GetActiveScene().path;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shared Assets/UI/Prefabs/ThoughtCloud.prefab");
        if (!prefab || !prefab.transform.Find("ThoughtCloud")) throw new Exception("Current thought bubble prefab is missing.");
        var scenes = new[] { "Assets/M/Scene/Elevator_Safety_Prototype.unity", "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity" };
        foreach (var path in scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
            foreach (var observation in lesson.CollectObservations())
            {
                observation.ExcludeNestedOutlineGeometry();
                if (observation.outlineTarget) EditorUtility.SetDirty(observation.outlineTarget);
            }
            var storybook = lesson.view.spatial.storybook;
            storybook.thoughtBubblePrefab = prefab;
            EditorUtility.SetDirty(storybook);
            // XR ray reach matches the visible scene, rather than the old 30 m cap.
            foreach (var ray in Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ray.gameObject.scene != scene) continue;
                ray.maxRaycastDistance = lesson.playerCamera.farClipPlane;
                EditorUtility.SetDirty(ray);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Failed to save " + path);
        }
        if (scenes.Contains(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        return new { success = true, scenes, thoughtPrefab = AssetDatabase.GetAssetPath(prefab) };
    }
}