using System.Linq;
using UnityEngine;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class InspectWallsHV { public static string Main(){var l=Object.FindFirstObjectByType<ElevatorLesson>();var filters=l.GetComponentsInChildren<MeshFilter>(true).Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SportsCentre3F/Meshes/SC_")).Select(f=>new{name=f.name,parent=f.transform.parent.name,grand=f.transform.parent.parent.name,active=f.gameObject.activeInHierarchy,collider=f.GetComponent<Collider>()!=null,bounds=f.sharedMesh.bounds.ToString()}).ToArray();System.IO.File.WriteAllText("Assets/M/CodexTemp/ElevatorHoverReview_20261030~/WallHierarchy.json",Newtonsoft.Json.JsonConvert.SerializeObject(filters,Newtonsoft.Json.Formatting.Indented));return "Saved native mesh hierarchy: "+filters.Length;}}