using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class InspectInteractionPolish
{
    static object Node(Transform t)=>new{name=t.name,parent=t.parent?t.parent.name:null,position=t.position.ToString("F4"),localPosition=t.localPosition.ToString("F4"),rotation=t.eulerAngles.ToString("F2"),scale=t.lossyScale.ToString("F4")};
    static object Renderers(Transform t)=>t.GetComponentsInChildren<Renderer>(true).Select(r=>new{node=Node(r.transform),bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>new{path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,colour=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():""}).ToArray()}).ToArray();
    public static object Main()
    {
        var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var p=lesson.presentation;var map=p.space.sportsCentre;
        var phone=lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Phone);
        return new{scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,friend=Node(lesson.friend),bones=lesson.friend.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="pelvis"||t.name=="thigh.L"||t.name=="thigh.R"||t.name.Contains("Pocket")||t.name=="head").Select(Node).ToArray(),friendMeshes=Renderers(lesson.friend),phone=Node(phone.transform),phoneMeshes=Renderers(phone.transform),phoneCollider=phone.GetComponent<BoxCollider>().size.ToString(),playerBall=Node(p.playerBall),ballMeshes=Renderers(p.playerBall),friendBall=Node(p.friendBall),friendBallMeshes=Renderers(p.friendBall),intercom=Node(map.sideIntercom),intercomAnchor=Node(lesson.view.spatial.intercomAnchor),intercomOffset=lesson.view.spatial.intercomOffset.ToString(),shader=AssetDatabase.GetAssetPath(p.playerBall.GetComponentInChildren<Renderer>(true).sharedMaterial.shader)};
    }
}