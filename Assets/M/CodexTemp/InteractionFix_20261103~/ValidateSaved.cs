using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class ValidateSavedInteractionFix
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new Exception("Saved Edit Mode scenes are required.");
        string original=SceneManager.GetActiveScene().path;
        var paths=new[]{"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
        var results=paths.Select(path=>
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            var lesson=Object.FindFirstObjectByType<ElevatorLesson>();
            var observations=lesson.CollectObservations();
            var friend=observations.First(o=>o.clue==ElevatorClue.Friend);
            var ball=observations.First(o=>o.clue==ElevatorClue.Ball);
            bool separated=!friend.outlineTarget.geometry.Any(g=>g.sourceRenderer&&g.sourceRenderer.transform.IsChildOf(ball.transform));
            bool populated=observations.Where(o=>o.hasDescription&&o.outlineTarget).All(o=>o.outlineTarget.geometry.Length>0);
            bool prefab=lesson.view.spatial.storybook.thoughtBubblePrefab;
            bool xrReach=Object.FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene).All(r=>r.maxRaycastDistance>=lesson.playerCamera.farClipPlane);
            if(!separated||!populated||!prefab||!xrReach)throw new Exception("Saved scene validation failed: "+path);
            return new{path,separated,populated,prefab,xrReach};
        }).ToArray();
        EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
        return new{success=true,results,compilationFailed=EditorUtility.scriptCompilationFailed,restoredScene=original};
    }
}