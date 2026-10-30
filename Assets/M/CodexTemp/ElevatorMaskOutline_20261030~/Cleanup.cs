using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using ProjectWait.M.Editor;
using Object=UnityEngine.Object;
public static class CleanupMaskMO
{
 const string D="Assets/M/CodexTemp/ElevatorMaskOutline_20261030~/";
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Edit Mode required");ElevatorMaskOutlineSetup.EnsureRenderer();var sceneReports=new List<object>();
  foreach(var path in ElevatorHoverRefinementSetup.Scenes)
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var roots=scene.GetRootGameObjects();var behaviours=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(b=>b).ToArray();
   if(behaviours.Any(b=>b.GetType().Name=="ElevatorShapeHighlight"))throw new Exception("Legacy component remains in "+path);
   var oldLines=roots.SelectMany(r=>r.GetComponentsInChildren<LineRenderer>(true)).Where(r=>r.name=="ObservationOutline"||r.name=="PointerOutline"||r.name=="ObserveOutline").ToArray();foreach(var line in oldLines)Object.DestroyImmediate(line.gameObject);
   var targets=behaviours.OfType<ElevatorOutlineTarget>().ToArray();if(targets.Length<15)throw new Exception("Missing targets");
   if(roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>t.name.StartsWith("SkyGlow")||t.name=="ButtonPressFeedback"))throw new Exception("Legacy glow meshes remain");
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");sceneReports.Add(new{path,targets=targets.Length,parts=targets.Sum(t=>t.geometry.Length),legacyLinesRemoved=oldLines.Length});
  }
  Directory.CreateDirectory("Assets/M/Arts/Models/ElevatorMaskOutline");AssetDatabase.Refresh();var moves=new List<object>();
  foreach(var path in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/M/Arts/Models/ElevatorInteraction"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("Target_")).ToArray())
  {
   string destination="Assets/M/Arts/Models/ElevatorMaskOutline/MaskGeometry_"+Path.GetFileName(path).Substring(7);var error=AssetDatabase.MoveAsset(path,destination);if(!string.IsNullOrEmpty(error))throw new Exception(error);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(destination);mesh.name=Path.GetFileNameWithoutExtension(destination);EditorUtility.SetDirty(mesh);moves.Add(new{path,destination});
  }
  var candidates=new List<string>{"Assets/M/Scripts/Elevator/ElevatorShapeHighlight.cs","Assets/M/Arts/Shaders/ElevatorSkyHighlight.shader","Assets/M/Arts/Shaders/ElevatorSkyHighlightMask.shader","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlowMask.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_0.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_1.mat","Assets/M/Arts/Materials/ElevatorInteraction/SkyGlow_2.mat"};
  candidates.AddRange(AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/M/Arts/Models/ElevatorInteraction"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("Actor_")));
  var rootsToCheck=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/M/")&&!p.Contains("~")&&!candidates.Contains(p)&&!p.EndsWith(".cs")&&!AssetDatabase.IsValidFolder(p)).ToArray();var dependencies=new HashSet<string>();foreach(var root in rootsToCheck)foreach(var dep in AssetDatabase.GetDependencies(root,true))dependencies.Add(dep);
  var blocked=candidates.Where(p=>dependencies.Contains(p)).ToArray();if(blocked.Length>0)throw new Exception("Legacy assets still referenced: "+string.Join(",",blocked));
  AssetDatabase.SaveAssets();var removed=new List<string>();foreach(var p in candidates.Where(p=>!p.EndsWith(".cs"))){if(AssetDatabase.LoadMainAssetAtPath(p)){if(!AssetDatabase.DeleteAsset(p))throw new Exception("Delete failed "+p);removed.Add(p);}}
  AssetDatabase.SaveAssets();var script=candidates.Single(p=>p.EndsWith(".cs"));if(AssetDatabase.DeleteAsset(script))removed.Add(script);
  var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{success=true,scenes=sceneReports,moved=moves,removed},Newtonsoft.Json.Formatting.Indented);File.WriteAllText(D+"Cleanup.json",report);return report;
 }
}
