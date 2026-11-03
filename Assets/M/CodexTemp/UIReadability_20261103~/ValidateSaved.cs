using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class ValidateSavedElevatorReadability
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Use saved Edit Mode scenes.");
        string original=SceneManager.GetActiveScene().path;var reports=new List<object>();
        foreach(string path in new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
            var root=lesson.presentation.space.sportsCentre.geometry.Find("ReadableSignage");
            var texts=root.GetComponentsInChildren<TMP_Text>(true);
            bool fonts=texts.All(t=>t.font&&AssetDatabase.GetAssetPath(t.font).StartsWith("Assets/M/")&&t.fontSharedMaterial&&AssetDatabase.GetAssetPath(t.fontSharedMaterial).StartsWith("Assets/M/"));
            int missing=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            var p=PrefabUtility.GetCorrespondingObjectFromSource(root.gameObject);
            var shaders=texts.Select(t=>t.fontSharedMaterial.shader).Concat(root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader)).Distinct().ToArray();
            bool success=texts.Length==88&&root.GetComponentsInChildren<Collider>(true).Length==0&&fonts&&missing==0&&p&&shaders.All(s=>s&&!ShaderUtil.ShaderHasError(s));
            reports.Add(new{success,path,labels=texts.Length,fonts,missingScripts=missing,prefab=AssetDatabase.GetAssetPath(p),dirty=scene.isDirty,shaderErrors=shaders.Sum(s=>ShaderUtil.GetShaderMessages(s).Count(m=>m.severity.ToString()=="Error"))});
        }
        EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
        var result=new{success=reports.All(r=>(bool)r.GetType().GetProperty("success").GetValue(r)),scenes=reports};
        File.WriteAllText("Assets/M/CodexTemp/UIReadability_20261103~/SavedValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}