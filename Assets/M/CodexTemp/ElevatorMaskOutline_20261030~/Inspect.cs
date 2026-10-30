using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectMO{public static string Main(){var l=Object.FindFirstObjectByType<ElevatorLesson>();var pipeline=l.GetComponent<ElevatorSceneRendering>().scenePipeline;var settings=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();var so=new SerializedObject(pipeline);var array=so.FindProperty("m_RendererDataList");var paths=Enumerable.Range(0,array.arraySize).Select(i=>AssetDatabase.GetAssetPath(array.GetArrayElementAtIndex(i).objectReferenceValue)).ToArray();return Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=l.gameObject.scene.path,Application.isPlaying,pipeline=AssetDatabase.GetAssetPath(pipeline),rendererPaths=paths,compatibility=settings.enableRenderCompatibilityMode,targets=l.CollectObservations().Where(o=>o.gameObject.activeInHierarchy).Select(o=>new{name=o.name,clue=o.clue.ToString(),shells=o.shapeHighlight?o.shapeHighlight.shells.Length:0}).ToArray()});}}