using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ValidateSavedReadability
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Saved Edit Mode scenes required.");
  string original=SceneManager.GetActiveScene().path;int validated=0;
  foreach(var path in new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"})
  {
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();
   if(!l||!l.view||!l.view.spatial||!l.view.spatial.storybook||!l.view.spatial.storybook.thoughtBubblePrefab)throw new Exception("UI reference missing: "+path);
   if(l.settings.lobbyDialogue!="오늘 패스 좋았어!\n1층으로 내려가서 집에 가자.")throw new Exception("Short saved opening line missing.");
   if(!AssetDatabase.GetAssetPath(l.settings).StartsWith("Assets/M/"))throw new Exception("Settings must belong to M.");
   if(ElevatorDialogueText.BodySize!=35||ElevatorDialogueText.PanelWidth!=580||ElevatorDialogueText.LinesPerPage!=3)throw new Exception("Dialogue layout constants invalid.");
   validated++;
  }
  EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
  return new{success=true,validated,compilationFailed=EditorUtility.scriptCompilationFailed,restoredScene=original};
 }
}