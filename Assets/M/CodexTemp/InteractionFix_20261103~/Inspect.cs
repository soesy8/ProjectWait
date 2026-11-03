using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class InspectInteractionFix
{
    public static object Main()
    {
        var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
        var storybook = lesson.view.spatial.storybook;
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shared Assets/UI/Prefabs/ThoughtCloud.prefab");
        return new
        {
            scene = SceneManager.GetActiveScene().path,
            dirty = SceneManager.GetActiveScene().isDirty,
            cameraFar = lesson.playerCamera.farClipPlane,
            actors = lesson.CollectObservations().Where(o => o.clue == ElevatorClue.Friend || o.clue == ElevatorClue.Ball).Select(o => new
            {
                o.name,
                position = o.transform.position.ToString("F3"),
                colliders = o.GetComponentsInChildren<Collider>(true).Select(c => new { c.name, bounds = c.bounds.ToString(), owner = c.GetComponentInParent<ElevatorObservation>()?.name }).ToArray(),
                geometry = o.outlineTarget.geometry.Select(g => g.sourceRenderer ? g.sourceRenderer.name + " / " + g.sourceRenderer.GetComponentInParent<ElevatorObservation>()?.name : "mesh").ToArray()
            }).ToArray(),
            prefabChildren = prefab.GetComponentsInChildren<RectTransform>(true).Select(t => new { t.name, size = t.sizeDelta.ToString(), position = t.anchoredPosition.ToString() }).ToArray(),
            currentThought = lesson.view.thoughtPanel.GetComponentsInChildren<RectTransform>(true).Select(t => t.name).ToArray()
        };
    }
}