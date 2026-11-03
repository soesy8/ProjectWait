using System.Linq;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class PocketProbe
{
 public static object Main()
 {
  var l=Object.FindFirstObjectByType<ElevatorLesson>();var r=l.friend.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=new Mesh();r.BakeMesh(mesh);
  var vertices=mesh.vertices.Select(v=>l.friend.InverseTransformPoint(r.transform.TransformPoint(v))).ToArray();
  var result=Enumerable.Range(0,9).Select(i=>{float y=.30f+i*.04f;var a=vertices.Where(v=>v.x>.10f&&v.x<.27f&&v.y>y&&v.y<y+.04f).ToArray();return new{y,count=a.Length,minZ=a.Length>0?a.Min(v=>v.z):0,maxZ=a.Length>0?a.Max(v=>v.z):0,minX=a.Length>0?a.Min(v=>v.x):0,maxX=a.Length>0?a.Max(v=>v.x):0};}).ToArray();Object.DestroyImmediate(mesh);return result;
 }
}