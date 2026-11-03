using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ValidateSavedPolish
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode scenes required.");
  string original=SceneManager.GetActiveScene().path;
  var result=new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"}.Select(path=>
  {
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();
   var p=l.CollectObservations().First(o=>o.clue==ElevatorClue.Phone);var f=l.friend.GetComponent<ElevatorObservation>();
   bool attached=p.transform.IsChildOf(l.friend)&&p.transform.parent.name=="pelvis";
   bool independent=!f.outlineTarget.geometry.Any(g=>g.sourceRenderer&&g.sourceRenderer.transform.IsChildOf(p.transform));
   bool fade=l.presentation.ballFadeMaterial&&l.presentation.ballFadeMaterial.shader.name=="ProjectWait/M/Ball Handoff Fade"&&!ShaderUtil.ShaderHasError(l.presentation.ballFadeMaterial.shader);
   bool values=l.scenario.guidanceDelay==8&&l.scenario.guidanceCycleSeconds==1&&l.presentation.ballHandoffSeconds==1.1f;
   bool ui=l.view.spatial.storybook.thoughtBubblePrefab&&l.view.spatial.intercomAnchor==l.presentation.space.sportsCentre.sideIntercom;
   if(!attached||!independent||!fade||!values||!ui)throw new Exception("Saved references invalid: "+path);
   return new{path,attached,independent,fade,values,ui};
  }).ToArray();
  EditorSceneManager.OpenScene(original,OpenSceneMode.Single);return new{success=true,result,compilationFailed=EditorUtility.scriptCompilationFailed};
 }
}