using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ApplyFadeSkybox
{
 public const string TexturePath="Assets/M/Arts/Textures/Skybox_Storybook_City_1024.png";
 public const string MaterialPath="Assets/M/Arts/Materials/Elevator/StorybookCitySkybox.mat";
 public static object Main()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new Exception("Stop Play Mode and preserve unsaved scene changes first.");
  string original=SceneManager.GetActiveScene().path;
  if(!original.StartsWith("Assets/M/Scene/"))throw new Exception("Only M scenes may be changed.");
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
  if(!texture||texture.width!=texture.height*2)throw new Exception("Expected existing 2:1 panorama.");
  var shader=Shader.Find("Skybox/Panoramic");if(!shader)throw new Exception("Built-in panoramic shader missing.");
  var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,MaterialPath);}
  material.shader=shader;material.SetTexture("_MainTex",texture);
  material.SetFloat("_Mapping",1);material.SetFloat("_ImageType",0);material.SetFloat("_Layout",0);
  material.SetFloat("_Exposure",1);material.SetFloat("_Rotation",0);material.SetColor("_Tint",new Color(.5f,.5f,.5f,1));
  material.DisableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");material.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
  EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
  int scenes=0,oldHeaders=0,oldHints=0;
  foreach(var name in new[]{"Elevator_Safety_Prototype","Elevator_Safety_ObjectInteraction_Test"})
  {
   string path="Assets/M/Scene/"+name+".unity";
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
   var l=Object.FindFirstObjectByType<ElevatorLesson>();if(!l)throw new Exception("Lesson missing: "+path);
   RenderSettings.skybox=material;
   // Preserve authored tri-light/cel lighting. Only the background changes.
   l.playerCamera.clearFlags=CameraClearFlags.Skybox;EditorUtility.SetDirty(l.playerCamera);
   var overrideSky=l.playerCamera.GetComponent<Skybox>();if(overrideSky){overrideSky.material=material;EditorUtility.SetDirty(overrideSky);}
   var thought=l.view.thoughtPanel;
   foreach(Transform child in thought)
    if(child.name=="ThoughtHeading"||child.name=="ThoughtRibbon")
    {child.gameObject.SetActive(false);var text=child.GetComponent<TMPro.TMP_Text>();if(text)text.text="";oldHeaders++;}
   if(l.scenario&&l.scenario.hints!=null)foreach(var hint in l.scenario.hints)
    if(hint){hint.gameObject.SetActive(false);if(hint.group){hint.group.alpha=0;hint.group.blocksRaycasts=hint.group.interactable=false;}oldHints++;}
   EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
   if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),path))throw new Exception("Save failed: "+path);
   scenes++;
  }
  EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
  return new{success=true,scenes,oldHeaders,oldHints,material=MaterialPath,texture=TexturePath,textureGuid=AssetDatabase.AssetPathToGUID(TexturePath),restoredScene=original};
 }
}