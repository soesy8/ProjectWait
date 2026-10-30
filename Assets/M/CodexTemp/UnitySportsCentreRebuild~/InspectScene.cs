using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
public static class InspectSportsScene
{
 public static object Main(){
  var s=SceneManager.GetActiveScene();var l=UnityEngine.Object.FindFirstObjectByType<ElevatorLesson>(FindObjectsInactive.Include);
  Func<Transform,object> tree=null;tree=t=>new{name=t.name,p=new[]{t.position.x,t.position.y,t.position.z},active=t.gameObject.activeSelf,components=t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name).ToArray(),children=t.Cast<Transform>().Select(c=>new{name=c.name,active=c.gameObject.activeSelf,children=c.childCount}).ToArray()};
  Func<UnityEngine.Object,object> refs=o=>o.GetType().GetFields().Where(f=>typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType)).Select(f=>new{field=f.Name,name=(f.GetValue(o) as UnityEngine.Object)?(f.GetValue(o) as UnityEngine.Object).name:null}).ToArray();
  return Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=s.path,dirty=s.isDirty,playing=Application.isPlaying,roots=s.GetRootGameObjects().Select(g=>tree(g.transform)).ToArray(),lesson=l?tree(l.transform):null,fields=l?refs(l):null,presentation=l&&l.presentation?refs(l.presentation):null,space=l&&l.presentation&&l.presentation.space?refs(l.presentation.space):null,cabin=l?tree(l.transform.Find("Cabin")):null,camera=l?tree(l.playerCamera.transform):null});
 }
}
