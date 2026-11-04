using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class RescuePlaytest
{
 const string Work="Assets/M/CodexTemp/RescueIntegration_20261104~/";
 static readonly List<object> Checks=new List<object>();
 static ElevatorLesson lesson;static ElevatorRescueTeam team;static ElevatorDesktopControls controls;
 static void Check(bool pass,string text){Checks.Add(new{passed=pass,check=text});if(!pass)throw new Exception(text);}
 static string Speaker()=>((TMPro.TMP_Text)typeof(ElevatorView).GetField("speaker",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lesson.view)).text;
 static void Advance(float seconds){while(seconds>.0001f){float dt=Mathf.Min(.05f,seconds);lesson.Advance(dt);lesson.presentation.space.overlay.Advance(dt);seconds-=dt;}}
 static async Task Shot(string scene,string name){await Task.Delay(200);ScreenCapture.CaptureScreenshot(Work+scene+"_"+name+".png");await Task.Delay(250);}
 static bool FaceInView(Transform actor)
 {
  var head=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="head");var p=lesson.playerCamera.WorldToViewportPoint(head.position+Vector3.up*.18f);
  return p.z>0&&p.x>.03f&&p.x<.97f&&p.y>.04f&&p.y<.96f;
 }
 static void FloorCheck(string label)
 {
  float floor=lesson.presentation.space.sportsCentre.CurrentFloorHeight;
  foreach(var actor in new[]{team.firefighter,team.specialist})
  {
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);
   float min=mesh.vertices.Min(v=>skin.transform.TransformPoint(v).y);Object.Destroy(mesh);
   Check(Mathf.Abs(min-floor)<.065f,label+": "+actor.name+" feet remain on landing ("+(min-floor).ToString("F3")+"m)");
  }
 }
 public static async Task<object> Run()
 {
  Checks.Clear();lesson=Object.FindFirstObjectByType<ElevatorLesson>();
  Check(Application.isPlaying&&lesson&&lesson.Flow!=null,"Play session initialized");
  int first=Time.frameCount;await Task.Delay(400);Check(Time.frameCount>first+1,"Player loop advances before capture");
  team=lesson.presentation.rescueTeam;controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();
  Check(team&&team.firefighter&&team.specialist,"Both new characters are connected");
  string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  bool enabled=lesson.enabled,controlEnabled=controls.enabled;string error=null;
  var renderErrors=new List<string>();
  Application.LogCallback monitor=(message,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&renderErrors.Count<20)renderErrors.Add(message);};
  Application.logMessageReceived+=monitor;
  try
  {
   lesson.enabled=false;controls.enabled=false;
   lesson.DeveloperJump(6);controls.SetLookAngles(180,-7);Advance(2.4f);
   Check(team.firefighter.gameObject.activeInHierarchy&&team.specialist.gameObject.activeInHierarchy,"Both crew visible during safety inspection");
   Check(team.GetComponentsInChildren<SkinnedMeshRenderer>().Length==2,"Only two palette skin renderers are used");
   Check(FaceInView(team.firefighter)&&FaceInView(team.specialist),"Both faces fit existing cabin camera view");
   Check(Speaker()=="승강기 전문 구조요원","Inspection dialogue switches to lift specialist");
   Check(!lesson.CanInteract(ElevatorClue.Rescuer),"Exit remains locked during safety inspection");
   FloorCheck("Inspection");await Shot(scene,"01_Inspection");
   Advance(2.0f);lesson.presentation.space.overlay.Advance(10);
   Check(lesson.Flow.ReadyToExit&&lesson.CanInteract(ElevatorClue.Rescuer),"Exit unlocks after safety check");
   Check(Speaker()=="소방 구조대원","Firefighter delivers exit instruction");
   var crew=lesson.CollectObservations().Where(o=>o.clue==ElevatorClue.Rescuer).ToArray();
   Check(crew.Length==2&&crew.All(o=>o.CanActivate&&o.outlineTarget&&o.outlineTarget.geometry.Length==1),"Both actors support same selection and silhouette highlight");
   Check(crew[0].outlineTarget.idleColor==crew[1].outlineTarget.idleColor&&crew[0].outlineTarget.hoverColor==crew[1].outlineTarget.hoverColor,"Neither crew member reveals a special answer colour");
   var gaze=controls.LookAngles;lesson.Advance(.05f);Check(Vector2.Distance(gaze,controls.LookAngles)<.001f,"Rescue updates preserve player gaze");
   await Shot(scene,"02_Ready");
   var target=team.specialist.GetComponent<Collider>();var ray=new Ray(lesson.playerCamera.transform.position,(target.bounds.center-lesson.playerCamera.transform.position).normalized);
   Check(Physics.Raycast(ray,out var hit,10,~0,QueryTriggerInteraction.Ignore)&&hit.collider==target,"Specialist can be reached by actual PC selection ray");
   team.specialist.GetComponent<ElevatorObservation>().Activate();Check(lesson.Flow.Step==ElevatorStep.Exiting,"Selecting the specialist starts escorted exit");
   Advance(2.2f);await Shot(scene,"03_EscortedExit");Advance(lesson.settings.exitSeconds);
   lesson.presentation.space.overlay.Advance(10);Check(lesson.Flow.Step==ElevatorStep.ReturnLobby,"Escorted route reaches third-floor lobby");
   Check(team.firefighter.gameObject.activeInHierarchy&&team.specialist.gameObject.activeInHierarchy,"Both crew remain with the child in lobby");
   FloorCheck("Lobby");await Shot(scene,"04_LobbyTeam");
   team.firefighter.GetComponent<ElevatorObservation>().Activate();Check(lesson.Flow.Step==ElevatorStep.Returning,"Firefighter can continue the stair escort");
   Advance(3.6f);lesson.presentation.space.overlay.Advance(10);Check(lesson.Flow.Step==ElevatorStep.GroundFloor,"Escort reaches ground floor");
   Check(FaceInView(team.firefighter)&&FaceInView(team.specialist),"Both crew faces fit ground-floor composition");
   target=team.specialist.GetComponent<Collider>();ray=new Ray(lesson.playerCamera.transform.position,(target.bounds.center-lesson.playerCamera.transform.position).normalized);
   Check(Physics.Raycast(ray,out hit,12,~0,QueryTriggerInteraction.Ignore)&&hit.collider==target,"Specialist is unobstructed in ground-floor group");
   FloorCheck("Ground floor");await Shot(scene,"05_GroundFloorTeam");
   var friendAtGround=lesson.friend.position;lesson.Continue();Check(Vector3.Distance(friendAtGround,lesson.friend.position)<.02f,"Going-home transition preserves friend position");
   Advance(6.1f);Check(lesson.Flow.Step==ElevatorStep.Complete&&!team.gameObject.activeInHierarchy,"Story completes after the escorted rescue");
   lesson.Restart();Check(!team.gameObject.activeInHierarchy,"Restart hides rescue crew before arrival");
   Check(!Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.name=="FriendlyRescuer"),"Legacy placeholder is removed");
   Check(renderErrors.Count==0,"No new rendering errors or runtime exceptions");
  }
  catch(Exception e){error=e.ToString();}
  finally{lesson.Restart();lesson.enabled=enabled;controls.enabled=controlEnabled;Application.logMessageReceived-=monitor;}
  var report=new{scene,status=error==null?"PASS":"FAIL",checks=Checks.ToArray(),error,renderErrors};
  File.WriteAllText(Work+scene+"_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  return report;
 }
}
