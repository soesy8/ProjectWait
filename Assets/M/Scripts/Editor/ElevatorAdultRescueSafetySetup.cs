#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorAdultRescueSafetySetup
    {
        static readonly string[] Scenes={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
        const string Models="Assets/M/Arts/Models/Rescuers/";
        static float ScalePrefab(string name,float height)
        {
            string path=Models+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mesh=root.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                float scale=height/mesh.bounds.size.y;root.transform.localScale=Vector3.one*scale;
                PrefabUtility.SaveAsPrefabAsset(root,path);return scale;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        [MenuItem("Tools/Wait/Elevator/Apply adult scale and safe waiting cutscene")]
        public static void ApplyMenu(){ApplyBoth();}
        public static object ApplyBoth()
        {
            if(Application.isPlaying)throw new Exception("Stop play mode first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scene changes first");
            var original=EditorSceneManager.GetSceneManagerSetup();
            float firefighterHeight=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"Rescuer_A_Firefighter.prefab")
                .GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh.bounds.size.y*1.37f;
            float a=ScalePrefab("Rescuer_A_Firefighter",firefighterHeight),b=ScalePrefab("Rescuer_B_LiftSpecialist",2.12f);
            try
            {
                foreach(var path in Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var team=lesson.presentation.rescueTeam;
                    team.dialogueHeight=2.95f;EditorUtility.SetDirty(team);
                    team.firefighter.localScale=Vector3.one*a;team.specialist.localScale=Vector3.one*b;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(team.firefighter);PrefabUtility.RecordPrefabInstancePropertyModifications(team.specialist);
                    var safety=lesson.GetComponent<ElevatorDoorSafetyCutscene>();if(!safety)safety=lesson.gameObject.AddComponent<ElevatorDoorSafetyCutscene>();
                    safety.lesson=lesson;lesson.doorSafetyCutscene=safety;
                    safety.friendByDoor=new Vector3(-1.10f,ElevatorSportsCentreMap.ThirdFloorHeight,3.85f);
                    safety.friendAtSafeWall=new Vector3(-.86f,ElevatorSportsCentreMap.ThirdFloorHeight,4.75f);
                    var poster=lesson.posterCutscene.posterSurface;
                    var anchor=poster.transform.Find("DoorSafetyPointingAnchor");
                    if(!anchor){anchor=new GameObject("DoorSafetyPointingAnchor").transform;anchor.SetParent(poster.transform,false);}
                    // The first illustrated row of the existing poster says not to lean on doors.
                    anchor.position=poster.bounds.center+Vector3.right*.10f+Vector3.up*(poster.bounds.size.y*.205f);
                    safety.doorPosterAnchor=anchor;
                    var settings=lesson.settings;
                    settings.locationReportSeconds=3.5f;settings.safetyAdviceSeconds=3.5f;settings.doorLeanSeconds=3;
                    settings.doorWarningSeconds=3;settings.safeWallMoveSeconds=4;settings.safeWallSettleSeconds=3;
                    EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
                    EditorUtility.SetDirty(lesson);EditorUtility.SetDirty(safety);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(original);}
            return new{scenes=Scenes,firefighterHeight,specialistHeight=2.12f,firefighterScale=a,specialistScale=b};
        }
    }
}
#endif
