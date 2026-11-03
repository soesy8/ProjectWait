using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class InspectThoughtHeaders
{
    static string Path(Transform t) => t.parent ? Path(t.parent)+"/"+t.name : t.name;
    static bool Header(TMP_Text t) => t.name.Contains("Heading") || t.text.Contains("나의 생각") || t.text.Contains("살펴보니");
    public static object Main()
    {
        var scene=SceneManager.GetActiveScene();
        var lessons=Object.FindObjectsByType<ElevatorLesson>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.gameObject.scene==scene).ToArray();
        var report=new {
            readOnly=true,scene=scene.path,scene.isDirty,playing=Application.isPlaying,
            lessons=lessons.Select(l=>new {
                path=Path(l.transform),step=l.Flow?.Step.ToString(),thought=l.view.thought.text,
                panel=Path(l.view.thoughtPanel),
                title=l.view.spatial.thoughtHeading?new {path=Path(l.view.spatial.thoughtHeading.transform),l.view.spatial.thoughtHeading.text,active=l.view.spatial.thoughtHeading.gameObject.activeSelf}:null,
                prefab=AssetDatabase.GetAssetPath(l.view.spatial.storybook.thoughtBubblePrefab),
                prefabTitles=l.view.spatial.storybook.thoughtBubblePrefab.GetComponentsInChildren<TMP_Text>(true).Where(Header).Select(t=>new {path=Path(t.transform),t.text,active=t.gameObject.activeSelf}).ToArray()
            }).ToArray(),
            headers=Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&Header(t)).Select(t=>new {
                path=Path(t.transform),t.text,active=t.gameObject.activeSelf,inHierarchy=t.gameObject.activeInHierarchy,
                groups=t.GetComponentsInParent<CanvasGroup>(true).Select(g=>new {path=Path(g.transform),g.alpha,g.interactable,g.blocksRaycasts}).ToArray()
            }).ToArray(),
            ribbons=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("ThoughtRibbon")||t.name.Contains("Heading")||t.name=="TransitionText").Select(t=>new {path=Path(t),active=t.gameObject.activeSelf,inHierarchy=t.gameObject.activeInHierarchy}).ToArray()
        };
        File.WriteAllText("Assets/M/CodexTemp/ThoughtHeaderFix_20261103~/ReadOnlyInspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}