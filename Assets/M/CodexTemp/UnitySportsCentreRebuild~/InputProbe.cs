using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
public static class SportsInputProbe
{
 public static async Task<string> Main()
 {
  var original=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(original);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
  var m=InputSystem.AddDevice<Mouse>("QAProbe");m.MakeCurrent();
  try{var before=m.position.ReadValue().ToString();InputSystem.QueueStateEvent(m,new MouseState{position=new Vector2(960,540)});await Task.Delay(100);var auto=m.position.ReadValue().ToString();InputSystem.QueueStateEvent(m,new MouseState{position=new Vector2(960,540)});InputSystem.Update();var manual=m.position.ReadValue().ToString();await Task.Delay(200);return Newtonsoft.Json.JsonConvert.SerializeObject(new{before,auto,manual,later=m.position.ReadValue().ToString(),current=Mouse.current?.name,enabled=m.enabled,focused=Application.isFocused,window=EditorWindow.focusedWindow?EditorWindow.focusedWindow.GetType().FullName:null,mode=InputSystem.settings.updateMode.ToString(),behavior=InputSystem.settings.editorInputBehaviorInPlayMode.ToString()});}
  finally{InputSystem.RemoveDevice(m);InputSystem.settings=original;UnityEngine.Object.Destroy(settings);}
 }
}
