using System;
using System.Linq;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class DebugSmallerB
{
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 public static object Run()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();l.enabled=false;
  var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();controls.enabled=false;
  l.DeveloperJump(6);for(int i=0;i<48;i++)l.Advance(.05f);controls.SetLookAngles(180,-7);
  var team=l.presentation.rescueTeam;
  var records=new System.Collections.Generic.List<object>();
  foreach(var actor in new[]{team.firefighter,team.specialist})
  {
   var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=new Mesh();skin.BakeMesh(mesh);
   var points=mesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);Object.Destroy(mesh);
   var head=skin.bones.First(x=>x.name=="head");
   records.Add(new{name=actor.name,actor=V(actor.position),scale=V(actor.lossyScale),skin=V(skin.transform.position),skinScale=V(skin.transform.lossyScale),
    active=skin.gameObject.activeInHierarchy,enabled=skin.enabled,visible=skin.isVisible,layer=skin.gameObject.layer,
    rendererCenter=V(skin.bounds.center),rendererSize=V(skin.bounds.size),bakedCenter=V(b.center),bakedSize=V(b.size),
    rootBone=V(skin.rootBone.position),rootRotation=V(skin.rootBone.eulerAngles),localBoundsCenter=V(skin.localBounds.center),localBoundsSize=V(skin.localBounds.size),head=V(head.position),headViewport=V(l.playerCamera.WorldToViewportPoint(head.position)),
    bones=skin.bones.Length,vertices=skin.sharedMesh.vertexCount,material=skin.sharedMaterial.name});
  }
  return records;
 }
}
