using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class InspectElevatorUIReadability
{
    static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;
    public static object Main()
    {
        var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
        var result = new
        {
            playing = Application.isPlaying,
            scene = SceneManager.GetActiveScene().path,
            dirty = SceneManager.GetActiveScene().isDirty,
            step = lesson && lesson.Flow != null ? lesson.Flow.Step.ToString() : "EditMode",
            texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene == SceneManager.GetActiveScene())
                .Select(t => new { path = Path(t.transform), text = t.text, size = t.fontSize, style = t.fontStyle.ToString(),
                    weight = t.fontWeight.ToString(), rect = t.rectTransform.sizeDelta.ToString(), scale = t.transform.lossyScale.ToString(),
                    active = t.gameObject.activeInHierarchy, enabled = t.enabled, overflow = t.isTextOverflowing,
                    font = t.font ? AssetDatabase.GetAssetPath(t.font) : "", material = t.fontSharedMaterial ? t.fontSharedMaterial.name : "" }).ToArray()
        };
        System.IO.File.WriteAllText("Assets/M/CodexTemp/UIReadability_20261103~/Inspection.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.playing, result.scene, result.dirty, count = result.texts.Length };
    }
}