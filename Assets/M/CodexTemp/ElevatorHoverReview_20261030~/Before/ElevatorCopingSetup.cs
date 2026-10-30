#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using TMPro;
using ProjectWait.M.Elevator;
using DwtD.TestScene;
using Object=UnityEngine.Object;
namespace ProjectWait.M.Editor
{
 public static class ElevatorCopingSetup
 {
  public const string Original="Assets/M/Scene/Elevator_Safety_Prototype.unity";
  public const string TestScene="Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity";
  const string Temp="Assets/M/CodexTemp/ElevatorCoping_20261030~";
  const string Mats="Assets/M/Arts/Materials/ElevatorInteraction/";
  static Material ink,teal,dark;
  static Transform Child(Transform root,string name){var t=root.Find(name);if(t)return t;t=new GameObject(name).transform;t.SetParent(root,false);return t;}
  static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
  static T Component<T>(GameObject g) where T:Component {var t=g.GetComponent<T>();return t?t:g.AddComponent<T>();}
  static Material ColorMaterial(string name,Color color)
  {
   string path=Mats+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;
  }
  static GameObject Shape(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
  {
   var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return go;
  }
  static ElevatorObservation Observation(Transform parent,ElevatorLesson l,string name,ElevatorClue clue,Vector3 position,Vector3 size)
  {
   var t=Child(parent,name);t.position=position;var box=Component<BoxCollider>(t.gameObject);box.size=size;var o=Component<ElevatorObservation>(t.gameObject);o.lesson=l;o.clue=clue;return o;
  }
  static void Wall(Transform parent,string name,Vector3 centre,Vector3 size)
  {var t=Child(parent,name);t.position=centre;var c=Component<BoxCollider>(t.gameObject);c.size=size;}
  static void Boundaries(ElevatorLesson l)
  {
   var b=Child(l.transform,"Scenario_TransparentBoundaries");
   Wall(b,"3F_StairEntrance",new Vector3(5.25f,9.8f,-2.05f),new Vector3(3.1f,4,.16f));
   Wall(b,"3F_StairSide",new Vector3(3.87f,10.7f,2.0f),new Vector3(.16f,5.6f,8.1f));
   Wall(b,"3F_TerraceDoor",new Vector3(0,9.9f,-5.58f),new Vector3(14,4,.18f));
   Wall(b,"3F_LeftGlassBoundary",new Vector3(-6.78f,9.9f,0),new Vector3(.18f,4,12));
   Wall(b,"3F_BackBoundary",new Vector3(0,9.9f,6.55f),new Vector3(14,4,.18f));
   Wall(b,"1F_StairSide",new Vector3(3.87f,1.9f,1.7f),new Vector3(.16f,4,8));
   Wall(b,"1F_CourtyardLeft",new Vector3(-6.8f,1.9f,-9),new Vector3(.18f,4,8));
   Wall(b,"1F_CourtyardRight",new Vector3(5.6f,1.9f,-9),new Vector3(.18f,4,8));
   Wall(b,"1F_CourtyardFar",new Vector3(-.6f,1.9f,-12.7f),new Vector3(12.5f,4,.18f));
  }
  static void Face(ElevatorLesson l,ElevatorCharacterPerformance performance)
  {
   var head=Find(l.friend,"head");var root=Child(head,"FaceExpression_Panic");foreach(Transform c in root.Cast<Transform>().ToArray())Object.DestroyImmediate(c.gameObject);
   var eyes=new List<Transform>();
   for(int side=0;side<2;side++)
   {
    float x=side==0?-.089f:.089f;var p=new Vector3(x,.2065f,.258f);

    var eye=Child(root,"FaceExpression_Spiral"+side);eye.localPosition=p;eyes.Add(eye);
    var line=Component<LineRenderer>(eye.gameObject);line.useWorldSpace=false;line.positionCount=100;line.widthMultiplier=.0041f;line.sharedMaterial=ink;line.shadowCastingMode=ShadowCastingMode.Off;line.numCapVertices=4;line.numCornerVertices=3;
    for(int j=0;j<100;j++){float t=j/99f,a=t*Mathf.PI*4.8f,r=Mathf.Lerp(.029f,.003f,t);line.SetPosition(j,new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*1.35f,0));}
   }
   var mouth=Child(root,"FaceExpression_WavyMouth");mouth.localPosition=new Vector3(0,.071f,.239f);var wave=Component<LineRenderer>(mouth.gameObject);wave.useWorldSpace=false;wave.positionCount=50;wave.widthMultiplier=.005f;wave.sharedMaterial=ink;wave.numCapVertices=4;wave.shadowCastingMode=ShadowCastingMode.Off;
   for(int j=0;j<50;j++){float t=j/49f;wave.SetPosition(j,new Vector3((t-.5f)*.1f,Mathf.Sin(t*Mathf.PI*4)*.009f,0));}
   var actor=l.friend.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>!r.name.StartsWith("SkyGlow"));
   var original=performance.normalFaceMesh?performance.normalFaceMesh:actor.sharedMesh;actor.sharedMesh=original;var baked=new Mesh();actor.BakeMesh(baked);var positions=baked.vertices;var colors=original.colors;var local=new Vector3[positions.Length];for(int i=0;i<local.Length;i++)local[i]=head.InverseTransformPoint(actor.transform.TransformPoint(positions[i]));
   // Weld only for connectivity analysis: skin and expression islands stay distinct.
   var parents=Enumerable.Range(0,positions.Length).ToArray();int Root(int i){while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;}void Union(int a,int b){a=Root(a);b=Root(b);if(a!=b)parents[b]=a;}
   var welded=new Dictionary<Vector3Int,int>();for(int i=0;i<positions.Length;i++){var v=positions[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));if(welded.TryGetValue(key,out int other))Union(i,other);else welded.Add(key,i);}
   var triangles=original.triangles;for(int i=0;i<triangles.Length;i+=3){Union(triangles[i],triangles[i+1]);Union(triangles[i],triangles[i+2]);}
   var bounds=new Dictionary<int,Bounds>();for(int i=0;i<local.Length;i++){int id=Root(i);if(!bounds.TryGetValue(id,out var box))box=new Bounds(local[i],Vector3.zero);box.Encapsulate(local[i]);bounds[id]=box;}
   var hidden=new HashSet<int>();foreach(var pair in bounds){var c=pair.Value.center;var size=pair.Value.size;bool eye=Mathf.Abs(c.x)>.035f&&Mathf.Abs(c.x)<.14f&&c.y>.14f&&c.y<.30f&&c.z>.16f&&size.x<.13f&&size.y<.16f&&size.z<.1f;bool mouthFeature=Mathf.Abs(c.x)<.04f&&c.y>-.005f&&c.y<.115f&&c.z>.16f&&size.x<.2f&&size.y<.13f&&size.z<.1f;if(eye||mouthFeature)hidden.Add(pair.Key);}
   var kept=new List<int>();for(int i=0;i<triangles.Length;i+=3)if(!hidden.Contains(Root(triangles[i]))){kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}
   if(kept.Count==triangles.Length)throw new Exception("No original facial features were isolated.");var panic=Object.Instantiate(original);panic.name="Friend original expression hidden";panic.subMeshCount=1;panic.SetTriangles(kept,0);string facePath="Assets/M/Arts/Models/ElevatorInteraction/FriendPanicBase.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(facePath);if(existing){EditorUtility.CopySerialized(panic,existing);Object.DestroyImmediate(panic);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(panic,facePath);existing=panic;}Object.DestroyImmediate(baked);
   existing.SetTriangles(kept,0,false);existing.UploadMeshData(false);EditorUtility.SetDirty(existing);
   performance.faceRenderer=actor;performance.normalFaceMesh=original;performance.panicFaceMesh=existing;performance.panicFace=root;performance.spiralEyes=eyes.ToArray();root.gameObject.SetActive(false);
  }
  static void Feedback(ElevatorObservation o,Transform moving,Vector3 direction)
  {
   var f=Component<ElevatorButtonFeedback>(o.gameObject);o.feedback=f;f.movingPart=moving;f.localPressDirection=direction;
   var source=o.shapeHighlight?o.shapeHighlight.shells.LastOrDefault(r=>r&&r.GetComponent<MeshFilter>()&&r.sharedMaterial.shader.name=="ProjectWait/M/Sky Highlight"):null;
   if(source)
   {
    var root=Child(o.transform,"ButtonPressFeedback");foreach(Transform c in root.Cast<Transform>().ToArray())Object.DestroyImmediate(c.gameObject);
    var copy=new GameObject("ButtonPressPulse");copy.transform.SetParent(root,false);copy.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);copy.transform.localScale=source.transform.lossyScale;
    copy.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;var r=copy.AddComponent<MeshRenderer>();r.sharedMaterial=source.sharedMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.enabled=false;f.pulseRenderers=new Renderer[]{r};
   }
   EditorUtility.SetDirty(o);EditorUtility.SetDirty(f);
  }
  static void Hints(ElevatorLesson l,ElevatorScenarioDirector director)
  {
   var root=Child(l.transform,"Fixed_Object_Hints");foreach(Transform c in root.Cast<Transform>().ToArray())Object.DestroyImmediate(c.gameObject);
   var result=new List<ElevatorObjectHint>();
   foreach(var o in l.CollectObservations().Where(o=>(o.gameObject.activeInHierarchy||o.clue==ElevatorClue.Rescuer)&&o.clue!=ElevatorClue.LobbyCallUp))
   {
    var r=(RectTransform)new GameObject("Hint_"+o.name,typeof(RectTransform)).transform;r.SetParent(root,false);r.sizeDelta=new Vector2(340,136);r.localScale=Vector3.one*.0017f;var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=l.playerCamera;canvas.overrideSorting=true;canvas.sortingOrder=160;var group=r.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
    var graphic=r.gameObject.AddComponent<CorridorBubbleGraphic>();graphic.color=new Color(1,.965f,.86f);graphic.borderColor=new Color(.28f,.19f,.14f);graphic.borderWidth=2;graphic.cornerRadius=18;graphic.extrusion=4;graphic.bevelWidth=2;graphic.raycastTarget=false;graphic.material=director.worldOverlay;
    var label=(RectTransform)new GameObject("HintText",typeof(RectTransform)).transform;label.SetParent(r,false);label.sizeDelta=new Vector2(300,112);var t=label.gameObject.AddComponent<TextMeshProUGUI>();t.font=l.view.font;t.fontSize=23;t.enableAutoSizing=true;t.fontSizeMin=18;t.fontSizeMax=23;t.color=new Color(.28f,.19f,.14f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
    var hint=r.gameObject.AddComponent<ElevatorObjectHint>();hint.observation=o;hint.label=t;hint.group=group;hint.worldOffset=new Vector3(-.45f,.28f,-.22f);
    if(o.clue==ElevatorClue.Friend||o.clue==ElevatorClue.Ball){hint.worldOffset=new Vector3(.90f,.60f,0);}
    if(o.clue==ElevatorClue.Ball)hint.worldOffset=new Vector3(.75f,-.2f,0);
    if(o.clue==ElevatorClue.Identity||o.clue==ElevatorClue.FrontIntercom)hint.worldOffset=new Vector3(.15f,.30f,.38f);
    if(o.clue==ElevatorClue.Notice||o.clue==ElevatorClue.FacilityGuide)hint.worldOffset=new Vector3(.70f,.1f,0);
    if(o.clue==ElevatorClue.Rescuer)hint.worldOffset=new Vector3(.90f,.60f,-.1f);
    r.position=o.transform.position+hint.worldOffset;result.Add(hint);
   }
   director.hints=result.ToArray();
  }
  static void WorldUI(ElevatorLesson l,ElevatorScenarioDirector director)
  {
   var cache=new Dictionary<Material,Material>();
   foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==l.gameObject.scene))
   {
    canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=l.playerCamera;
    var tracked=canvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();if(tracked){tracked.checkFor3DOcclusion=false;tracked.checkFor2DOcclusion=false;}var raycaster=canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();if(raycaster)raycaster.blockingObjects=UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
    foreach(var g in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
    {
     if(g is TMP_Text t)
     {
      var source=t.fontSharedMaterial;if(!source||source.shader.name=="TextMeshPro/Distance Field Overlay")continue;
      string path=Mats+"WorldText_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(!m){m=Object.Instantiate(source);m.shader=Shader.Find("TextMeshPro/Distance Field Overlay");m.renderQueue=4201;AssetDatabase.CreateAsset(m,path);}t.fontSharedMaterial=m;
     }
     else if(!(g is TMP_SubMeshUI))g.material=director.worldOverlay;
    }
   }
   l.view.spatial.hud.gameObject.SetActive(false);l.pauseMenu.canvas.gameObject.SetActive(false);
  }
  [MenuItem("Tools/Wait/Elevator/Apply coping scenario improvements (original)")]
  public static void ApplyOriginal()
  {
   if(Application.isPlaying)throw new Exception("Stop Play Mode first.");var scene=SceneManager.GetActiveScene();if(scene.path!=Original)throw new Exception("Open the original M scene.");Directory.CreateDirectory(Temp);
   if(!File.Exists(Temp+"/BeforeLiveScene.unity"))EditorSceneManager.SaveScene(scene,Temp+"/BeforeLiveScene.unity",true);
   ElevatorInteractionSetup.Apply();var l=Object.FindFirstObjectByType<ElevatorLesson>();var map=l.presentation.space.sportsCentre;
   var director=Component<ElevatorScenarioDirector>(l.gameObject);l.scenario=director;director.lesson=l;director.objectGuidedScene=false;director.worldOverlay=AssetDatabase.LoadAssetAtPath<Material>(Mats+"UIOverlay.mat");
   ink=ColorMaterial("ExpressionInk",Color.black);teal=ColorMaterial("PhoneTeal",new Color(.16f,.48f,.44f));dark=ColorMaterial("PhoneDark",new Color(.08f,.12f,.13f));
   l.view.spatial.worldPixelSize=.002f;
   var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();var pivot=Find(controls.xrOrigin,"Elevator_ShakePivot");if(!pivot){pivot=Child(l.playerCamera.transform.parent,"Elevator_ShakePivot");l.playerCamera.transform.SetParent(pivot,false);}
   var fx=Component<ElevatorCameraEffects>(l.gameObject);fx.lesson=l;fx.shakePivot=pivot;
   var performance=Component<ElevatorCharacterPerformance>(l.gameObject);performance.lesson=l;Face(l,performance);Boundaries(l);
   var down=l.transform.Find("SportsCentre_Interactions/3F_Lobby_Call");down.position=new Vector3(-.12f,9.10f,2.975f);down.GetComponent<BoxCollider>().size=new Vector3(.17f,.16f,.065f);
   var up=Observation(down.parent,l,"3F_Lobby_Up_Unavailable",ElevatorClue.LobbyCallUp,new Vector3(-.12f,9.32f,2.975f),new Vector3(.17f,.16f,.065f));up.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().enabled=false;
   var staff=Component<ElevatorObservation>(l.presentation.rescuer);staff.lesson=l;staff.clue=ElevatorClue.Rescuer;var staffBox=Component<BoxCollider>(staff.gameObject);staffBox.center=new Vector3(0,.75f,0);staffBox.size=new Vector3(.65f,1.55f,.5f);ElevatorInteractionSetup.AddActorHighlight(staff,90);
   var phone=Observation(down.parent,l,"Cabin_Optional119Phone",ElevatorClue.Phone,new Vector3(-.48f,8.98f,5.60f),new Vector3(.08f,.26f,.16f));
   if(!phone.transform.Find("PhoneBody")){Shape("PhoneBody",phone.transform,PrimitiveType.Cube,Vector3.zero,new Vector3(.03f,.24f,.135f),dark);Shape("PhoneScreen",phone.transform,PrimitiveType.Cube,new Vector3(-.018f,0,0),new Vector3(.007f,.19f,.108f),teal);}
   ElevatorInteractionSetup.AddActorHighlight(phone,91);
   foreach(var o in l.GetComponentsInChildren<ElevatorObservation>(true))
   {
    Transform moving=null;Vector3 direction=Vector3.zero;
    if(o.clue==ElevatorClue.FloorButtons){moving=Find(map.geometry,"3F · Cabin · 측면 01층 링");direction=Vector3.right*.004f;}
    else if(o.clue==ElevatorClue.FrontFloorButtons){moving=Find(map.geometry,"3F · Cabin · 전면 01층 링");direction=Vector3.back*.004f;}
    else if(o.clue==ElevatorClue.Intercom){moving=l.presentation.intercomButton;direction=Vector3.right*.003f;}
    if(o.clue==ElevatorClue.LobbyCall||o.clue==ElevatorClue.FloorButtons||o.clue==ElevatorClue.FrontFloorButtons||o.clue==ElevatorClue.Intercom)Feedback(o,moving,direction);
   }
   Hints(l,director);WorldUI(l,director);l.view.title.text="멈춰도 함께, 안전하게";l.settings.stoppedDialogue="덜컹! 어? 엘리베이터가 멈췄어…";
   foreach(var o in new Object[]{l,l.view,l.view.spatial,l.settings,director,performance,fx})EditorUtility.SetDirty(o);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Temp+"/OriginalApplied.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,guided=false,hints=director.hints.Length,boundaries=l.transform.Find("Scenario_TransparentBoundaries").childCount,appliedUtc=DateTime.UtcNow.ToString("o")},Newtonsoft.Json.Formatting.Indented));
  }
  [MenuItem("Tools/Wait/Elevator/Copy improved original to object interaction test")]
  public static void CopyTestScene()
  {
   if(Application.isPlaying)throw new Exception("Stop Play Mode first.");if(SceneManager.GetActiveScene().path!=Original)throw new Exception("Open the improved original first.");EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
   if(AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene))throw new Exception("Test scene already exists; preserve it rather than overwrite.");if(!AssetDatabase.CopyAsset(Original,TestScene))throw new Exception("Scene copy failed.");
   var scene=EditorSceneManager.OpenScene(TestScene,OpenSceneMode.Single);var l=Object.FindFirstObjectByType<ElevatorLesson>();l.scenario.objectGuidedScene=true;EditorUtility.SetDirty(l.scenario);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  }
 }
}
#endif
