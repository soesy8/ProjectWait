using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class RescueInspect
{
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Run()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var p=l?l.presentation:null;
  var c=l?l.playerCamera:null;var scene=SceneManager.GetActiveScene();
  var result=new{playing=Application.isPlaying,scene=scene.path,dirty=scene.isDirty,
   roots=scene.GetRootGameObjects().Select(o=>o.name).ToArray(),
   lesson=l?l.name:null,rescuer=p&&p.rescuer?new{name=p.rescuer.name,position=V(p.rescuer.transform.position),rotation=V(p.rescuer.transform.eulerAngles),scale=V(p.rescuer.transform.lossyScale),renderers=p.rescuer.GetComponentsInChildren<Renderer>(true).Select(r=>new{name=r.name,center=V(r.bounds.center),size=V(r.bounds.size),materials=r.sharedMaterials.Where(m=>m).Select(m=>new{name=m.name,shader=m.shader.name}).ToArray()}).Take(8).ToArray(),bones=p.rescuer.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"root","pelvis","head","upper_arm.L","forearm.L","hand.L"}.Contains(t.name)).Select(t=>new{name=t.name,position=V(t.position)}).ToArray()}:null,
   camera=c?new{position=V(c.transform.position),rotation=V(c.transform.eulerAngles),fov=c.fieldOfView}:null,
   leftDoor=p&&p.leftDoor?V(p.leftDoor.position):null,rightDoor=p&&p.rightDoor?V(p.rightDoor.position):null,
   observations=l?l.CollectObservations().Where(o=>o.clue==ElevatorClue.Rescuer).Select(o=>new{name=o.name,hasDescription=o.hasDescription,outline=o.outlineTarget?o.outlineTarget.GetType().FullName:null,components=o.GetComponents<Component>().Select(x=>x.GetType().FullName).ToArray()}).ToArray():null};
  File.WriteAllText("Assets/M/CodexTemp/RescueIntegration_20261104~/Inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
  return result;
 }
}
