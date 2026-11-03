using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class ApplyInteractionPolish
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Preserve unsaved changes and stop Play Mode first.");
  string original=SceneManager.GetActiveScene().path;
  const string materialPath="Assets/M/Arts/Materials/Elevator/BallHandoffFade.mat";
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/ElevatorBallFade.shader");
  if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Ball fade shader is missing or has compilation errors.");
  var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(!material){material=new Material(shader){name="BallHandoffFade",enableInstancing=true};AssetDatabase.CreateAsset(material,materialPath);}
  material.SetFloat("_Visibility",1);AssetDatabase.SaveAssetIfDirty(material);
  var scenes=new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
  foreach(string path in scenes)
  {
   string backup="Assets/M/CodexTemp/InteractionPolish_20261103~/Before/"+Path.GetFileName(path);
   if(!File.Exists(backup))File.Copy(path,backup);
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
   var phone=lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Phone);
   var pelvis=lesson.friend.GetComponentsInChildren<Transform>(true).First(t=>t.name=="pelvis");
   phone.transform.SetParent(pelvis,true);
   phone.transform.position=lesson.friend.TransformPoint(new Vector3(.19f,.58f,-.14f));
   phone.transform.rotation=lesson.friend.rotation*Quaternion.Euler(0,-90,0)*Quaternion.Euler(0,0,5);
   phone.transform.localScale=Vector3.one*(.85f/pelvis.lossyScale.x);
   // Only the exposed top is drawn; the rest is inside the pocket, not outlined through clothing.
   var body=phone.transform.Find("PhoneBody");body.localPosition=new Vector3(0,.04f,0);body.localScale=new Vector3(.025f,.09f,.085f);
   var screen=phone.transform.Find("PhoneScreen");screen.localPosition=new Vector3(-.016f,.05f,0);screen.localScale=new Vector3(.006f,.045f,.065f);
   var collider=phone.GetComponent<BoxCollider>();collider.center=new Vector3(-.10f,.045f,-.03f);collider.size=new Vector3(.10f,.09f,.10f);
   lesson.presentation.ballFadeMaterial=material;lesson.presentation.ballHandoffSeconds=1.1f;
   lesson.presentation.playerBall.localScale=lesson.presentation.friendBall.lossyScale;
   foreach(var r in lesson.presentation.playerBall.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;EditorUtility.SetDirty(r);}
   lesson.scenario.guidanceDelay=8;lesson.scenario.guidanceCycleSeconds=1;
   var spatial=lesson.view.spatial;spatial.intercomAnchor=lesson.presentation.space.sportsCentre.sideIntercom;
   foreach(var item in new Object[]{phone,phone.transform,collider,lesson.presentation,lesson.scenario,spatial})EditorUtility.SetDirty(item);
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed: "+path);
  }
  EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
  return new{success=true,scenes,materialPath};
 }
}