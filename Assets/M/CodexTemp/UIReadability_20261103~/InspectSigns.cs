using System.IO;
using System.Linq;
using UnityEngine;
using ProjectWait.M.Elevator;
using ProjectWait.M.Editor;
using Object = UnityEngine.Object;

public static class InspectElevatorSigns
{
    public static object Main()
    {
        var map = Object.FindFirstObjectByType<ElevatorSportsCentreMap>();
        var manifest=JsonUtility.FromJson<ElevatorSportsCentreSetup.Manifest>(File.ReadAllText("Assets/M/Arts/Models/SportsCentre3F/SportsCentreManifest.json"));
        var result=new System.Collections.Generic.List<object>();
        using(var reader=new BinaryReader(File.OpenRead("Assets/M/Arts/Models/SportsCentre3F/SportsCentreGeometry.bytes")))
        foreach(var p in manifest.objects.Where(p=>p.name.Contains("숫자")||p.name.Contains("식별")||p.name.Contains("비상")||p.name.Contains("층별")||p.name.Contains("층수")||p.name.Contains("호출")||p.name.Contains("건물명")||p.name.Contains("안내")))
        {
            var q=p.rotation;var matrix=Matrix4x4.TRS(new Vector3(p.position[0],p.position[1],p.position[2]),new Quaternion(q[0],q[1],q[2],q[3]),new Vector3(p.scale[0],p.scale[1],p.scale[2]));
            var bounds=new Bounds();reader.BaseStream.Position=p.offset;int count=reader.ReadInt32();
            for(int i=0;i<count;i++){var v=matrix.MultiplyPoint3x4(new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));reader.BaseStream.Position+=24;if(i==0)bounds=new Bounds(v,Vector3.zero);else bounds.Encapsulate(v);}
            result.Add(new{p.name,position=matrix.GetColumn(3).ToString("F4"),rotation=matrix.rotation.eulerAngles.ToString("F2"),center=bounds.center.ToString("F4"),size=bounds.size.ToString("F4")});
        }
        File.WriteAllText("Assets/M/CodexTemp/UIReadability_20261103~/Signs.json", Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        File.WriteAllText("Assets/M/CodexTemp/UIReadability_20261103~/Geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(map.geometry.GetComponentsInChildren<MeshFilter>(true).Select(f=>new{f.name,mesh=UnityEditor.AssetDatabase.GetAssetPath(f.sharedMesh),vertices=f.sharedMesh.vertexCount,materials=f.GetComponent<Renderer>().sharedMaterials.Select(m=>m.name).ToArray(),position=f.transform.position.ToString("F3"),scale=f.transform.lossyScale.ToString("F3")}).ToArray(),Newtonsoft.Json.Formatting.Indented));
        return new { count = result.Count };
    }
}