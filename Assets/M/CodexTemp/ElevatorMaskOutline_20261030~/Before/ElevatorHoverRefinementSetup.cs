#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
namespace ProjectWait.M.Editor
{
 public static class ElevatorHoverRefinementSetup
 {
  const string Model="Assets/M/Arts/Models/SportsCentre3F/";
  public static readonly string[] Scenes={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
  static Transform Child(Transform parent,string name)
  {var t=parent.Find(name);if(t)return t;var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
  static void Wall(Transform root,string name,Vector3 centre,Vector3 size)
  {var t=Child(root,name);t.SetPositionAndRotation(centre,Quaternion.identity);t.localScale=Vector3.one;var box=t.GetComponent<BoxCollider>();if(!box)box=t.gameObject.AddComponent<BoxCollider>();box.center=Vector3.zero;box.size=size;box.isTrigger=false;box.enabled=true;}
  static Vector3 V(float[] p)=>new Vector3(p[0],p[1],p[2]);
  static Bounds PartBounds(ElevatorSportsCentreSetup.Part part,BinaryReader reader)
  {
   var q=part.rotation;var matrix=Matrix4x4.TRS(V(part.position),new Quaternion(q[0],q[1],q[2],q[3]),V(part.scale));
   reader.BaseStream.Position=part.offset;int count=reader.ReadInt32();var bounds=new Bounds();
   for(int i=0;i<count;i++){var p=matrix.MultiplyPoint3x4(new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));reader.BaseStream.Position+=24;if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);}return bounds;
  }
  public static void ApplyToLesson(ElevatorLesson l)
  {
   if(Application.isPlaying||!l||!l.gameObject.scene.path.StartsWith("Assets/M/"))throw new Exception("Use an M scene in Edit Mode.");
   var dialogueGroup=l.view.dialoguePanel.GetComponent<CanvasGroup>();if(dialogueGroup)dialogueGroup.alpha=1;
   var pause=(RectTransform)l.pauseMenu.canvas.transform;pause.anchorMin=pause.anchorMax=pause.pivot=new Vector2(.5f,.5f);pause.sizeDelta=new Vector2(1280,720);pause.localScale=Vector3.one*.0016f;l.pauseMenu.canvas.gameObject.SetActive(false);
   foreach(var h in l.scenario.hints)
   {
    if(!h||!h.observation)continue;h.worldPixelSize=.002f;h.appearSeconds=.09f;h.disappearSeconds=.07f;h.objectPointOffset=Vector3.zero;h.observation.hasDescription=true;EditorUtility.SetDirty(h.observation);
    if(h.observation.shapeHighlight){var effect=h.observation.shapeHighlight;effect.idleColor=new Color(1,.70f,.28f,1);effect.idleWidthScale=1.25f;effect.hoverWidthScale=2;EditorUtility.SetDirty(effect);}
    switch(h.observation.clue)
    {
     case ElevatorClue.Friend:h.worldOffset=new Vector3(.85f,1.25f,.05f);break;
     case ElevatorClue.Rescuer:h.worldOffset=new Vector3(.95f,1.25f,-.1f);break;
     case ElevatorClue.Ball:h.worldOffset=new Vector3(-.65f,.40f,.05f);break;
     case ElevatorClue.Door:h.worldOffset=new Vector3(-.70f,1.20f,.40f);break;
     case ElevatorClue.Intercom:h.worldOffset=new Vector3(-.45f,.38f,-.20f);break;
     case ElevatorClue.FloorButtons:h.worldOffset=new Vector3(-.45f,.38f,.38f);break;
     case ElevatorClue.FrontFloorButtons:h.worldOffset=new Vector3(.55f,-.08f,.38f);break;
     case ElevatorClue.FrontIntercom:h.worldOffset=new Vector3(.62f,.20f,.38f);break;
     case ElevatorClue.Identity:h.worldOffset=new Vector3(.66f,.12f,.43f);break;
     case ElevatorClue.Display:h.worldOffset=new Vector3(.65f,.22f,.38f);break;
     case ElevatorClue.Notice:h.worldOffset=new Vector3(.40f,.20f,-.82f);break;
     case ElevatorClue.FacilityGuide:h.worldOffset=new Vector3(.40f,.18f,.78f);break;
     case ElevatorClue.Phone:h.worldOffset=new Vector3(-.45f,.42f,.38f);break;
     case ElevatorClue.LobbyCall:h.worldOffset=new Vector3(.65f,.18f,-.38f);break;
     case ElevatorClue.GroundExit:h.worldOffset=new Vector3(1.65f,.35f,.38f);break;
     default:h.worldOffset=new Vector3(.70f,.40f,.30f);break;
    }
    h.transform.position=h.observation.transform.position+h.worldOffset;h.transform.localScale=Vector3.one*h.worldPixelSize;h.group.alpha=0;h.group.blocksRaycasts=false;EditorUtility.SetDirty(h);
   }
   var walls=Child(l.transform,"Scenario_TransparentBoundaries");
   // Leave room for the full capsule to cross the basketball sliding-door threshold.
   Wall(walls,"3F_TerraceDoor",new Vector3(0,9.9f,-8.4f),new Vector3(14,4,.18f));
   Wall(walls,"3F_DownStairEntrance",new Vector3(8.1f,9.8f,-1.75f),new Vector3(2.9f,4,.18f));
   Wall(walls,"1F_StairEntrance",new Vector3(5.18f,1.9f,-2.05f),new Vector3(3.1f,4,.18f));
   // The old collision name filter omitted the fixed entrance collection.
   // These filters are the fixed walls, sidelights and frames, not the animated leaves.
   foreach(var filter in l.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Model+"Meshes/")&&f.transform.parent.name.EndsWith("08 · 뒤쪽 자동문 / 진입부")))
   {
    var collider=filter.GetComponent<MeshCollider>();if(!collider)collider=filter.gameObject.AddComponent<MeshCollider>();
    collider.sharedMesh=filter.sharedMesh;collider.convex=false;collider.isTrigger=false;collider.enabled=true;EditorUtility.SetDirty(collider);
   }
   var props=Child(l.transform,"Furniture_CompoundColliders");
   var manifest=JsonUtility.FromJson<ElevatorSportsCentreSetup.Manifest>(File.ReadAllText(Model+"SportsCentreManifest.json"));
   using(var reader=new BinaryReader(File.OpenRead(Model+"SportsCentreGeometry.bytes")))
   foreach(var part in manifest.objects.Where(p=>p.type=="MESH"&&p.count>0))
   {
    bool pot=part.name.Contains("차콜 화분");
    bool seat=part.name.Contains("벤치 좌방석")||part.name.Contains("벤치 등방석")||part.name.Contains("벤치 팔걸이")||part.name.Contains("운동화 벤치")||part.name.Contains("의자");
    if(!pot&&!seat)continue;var bounds=PartBounds(part,reader);var t=Child(props,part.name);t.SetPositionAndRotation(bounds.center,Quaternion.identity);t.localScale=Vector3.one;
    if(pot){var c=t.GetComponent<CapsuleCollider>();if(!c)c=t.gameObject.AddComponent<CapsuleCollider>();c.direction=1;c.center=Vector3.zero;c.radius=Mathf.Max(bounds.extents.x,bounds.extents.z);c.height=Mathf.Max(bounds.size.y,c.radius*2);c.isTrigger=false;}
    else{var c=t.GetComponent<BoxCollider>();if(!c)c=t.gameObject.AddComponent<BoxCollider>();c.center=Vector3.zero;c.size=Vector3.Max(bounds.size,Vector3.one*.04f);c.isTrigger=false;}
   }
   l.settings.waitDialogue="도움을 요청했으니까, 옆에서 같이 기다리자.";
   foreach(var o in new Object[]{l,l.settings,l.scenario,l.pauseMenu,l.pauseMenu.canvas})EditorUtility.SetDirty(o);
   EditorSceneManager.MarkSceneDirty(l.gameObject.scene);
  }
  [MenuItem("Tools/Wait/Elevator/Apply hover and collision refinements to both scenes")]
  public static void ApplyBoth()
  {
   if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene before applying refinements.");
   foreach(var path in Scenes){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);ApplyToLesson(Object.FindFirstObjectByType<ElevatorLesson>());if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed: "+path);}AssetDatabase.SaveAssets();
  }
 }
}
#endif
