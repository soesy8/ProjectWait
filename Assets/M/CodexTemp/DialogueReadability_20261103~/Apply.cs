using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ApplyDialogueReadability
{
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Save unsaved user changes and stop Play Mode first.");
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var path=AssetDatabase.GetAssetPath(l.settings);
  if(!path.StartsWith("Assets/M/"))throw new Exception("Only M settings may be saved.");
  const string backup="Assets/M/CodexTemp/DialogueReadability_20261103~/Before/ElevatorSettings.asset";
  if(!File.Exists(backup))File.Copy(path,backup);
  l.settings.lobbyDialogue="오늘 패스 좋았어!\n1층으로 내려가서 집에 가자.";
  l.settings.handoffDialogue="공 좀 잠깐 맡아 줘.\n튀기지 말고 안고 있어 줘.";
  EditorUtility.SetDirty(l.settings);AssetDatabase.SaveAssetIfDirty(l.settings);
  return new{success=true,path};
 }
}