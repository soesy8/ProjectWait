#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
namespace ProjectWait.M.Editor
{
 public static class ElevatorMaskOutlineSetup
 {
  public const string RendererPath="Assets/M/Arts/Rendering/ElevatorOutlineRenderer.asset";
  public static void EnsureRenderer()
  {
   var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/M/Arts/Rendering/ElevatorScenePipeline.asset");if(!pipeline)throw new Exception("M scene pipeline is missing.");
   var so=new SerializedObject(pipeline);var renderers=so.FindProperty("m_RendererDataList");var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
   if(!renderer){var path=AssetDatabase.GetAssetPath(renderers.GetArrayElementAtIndex(0).objectReferenceValue);if(!AssetDatabase.CopyAsset(path,RendererPath))throw new Exception("Cannot copy scene renderer.");renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);}
   renderer.name="ElevatorOutlineRenderer";var feature=renderer.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().FirstOrDefault();
   if(!feature){feature=ScriptableObject.CreateInstance<ElevatorSilhouetteOutlineFeature>();feature.name="Elevator mask silhouette";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
   feature.maskShader=Shader.Find("ProjectWait/M/Outline Coverage");feature.distanceShader=Shader.Find("ProjectWait/M/Outline Distance");feature.SetActive(true);feature.Create();renderer.SetDirty();EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(feature);
   renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
  }
  [MenuItem("Tools/Wait/Elevator/Apply screen mask outline configuration")]
  public static void ApplyBoth()
  {
   if(Application.isPlaying)throw new Exception("Stop Play Mode first.");EnsureRenderer();
   foreach(var path in ElevatorHoverRefinementSetup.Scenes)
   {
    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
    foreach(var observation in lesson.CollectObservations())if(observation.outlineTarget){var target=observation.outlineTarget;target.idleColor=new Color(1,.70f,.28f,1);target.hoverColor=new Color(.32f,.84f,1,1);target.idleWidth=6.5f;target.hoverWidth=10.4f;target.idleHalo=20;target.hoverHalo=32;EditorUtility.SetDirty(target);}
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   AssetDatabase.SaveAssets();
  }
 }
}
#endif
