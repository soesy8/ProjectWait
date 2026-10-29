using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectWait.M.Elevator;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyCanvasGroups
{
    static readonly List<object> Checks = new List<object>();
    static ElevatorLesson lesson;
    static void Check(string name, bool passed)
    {
        Checks.Add(new { name, passed });
        if (!passed) throw new InvalidOperationException(name);
    }

    static void Visibility(string name, ElevatorStep expected)
    {
        Check(name + ": step", lesson.Flow.Step == expected);
        lesson.presentation.space.overlay.Advance(1);
        lesson.view.SetCinematicVisibility();
        var world = lesson.view.GetComponent<CanvasGroup>();
        var hud = lesson.view.spatial.hud.GetComponent<CanvasGroup>();
        bool transition = expected == ElevatorStep.Entering || expected == ElevatorStep.Exiting;
        Check(name + ": world visibility and input", world &&
            world.alpha == (transition ? 0 : 1) &&
            world.interactable == !lesson.CinematicBlocking &&
            world.blocksRaycasts == world.interactable);
        Check(name + ": HUD visibility and input", hud &&
            hud.alpha == (lesson.Flow.IsCinematic ? 0 : 1) &&
            !hud.interactable && !hud.blocksRaycasts);
    }

    public static object Run()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/M/Scene/Elevator_Safety_Prototype.unity")
            throw new InvalidOperationException("Run only in the M elevator scene in Play Mode.");
        Checks.Clear();
        lesson = Object.FindFirstObjectByType<ElevatorLesson>();
        var view = lesson.view;
        bool noResponse = lesson.simulateNoResponse;
        var temporary = new GameObject("CanvasGroup_Requirement_Verification", typeof(RectTransform));
        temporary.SetActive(false);
        try
        {
            var addedView = temporary.AddComponent<ElevatorView>();
            Check("New ElevatorView automatically receives CanvasGroup", temporary.GetComponents<CanvasGroup>().Length == 1);
            temporary.AddComponent<ElevatorSpatialUI>();
            Check("Adding spatial UI does not duplicate CanvasGroup", temporary.GetComponents<CanvasGroup>().Length == 1);
            Object.DestroyImmediate(temporary);
            temporary = new GameObject("Spatial_CanvasGroup_Requirement_Verification", typeof(RectTransform));
            temporary.SetActive(false);
            temporary.AddComponent<ElevatorSpatialUI>();
            Check("New ElevatorSpatialUI independently receives CanvasGroup", temporary.GetComponents<CanvasGroup>().Length == 1);
            Object.DestroyImmediate(temporary);

            lesson.simulateNoResponse = false;
            lesson.Restart(); Visibility("Lobby", ElevatorStep.Lobby);
            view.InvokeChoice(0); Visibility("Entering", ElevatorStep.Entering);
            int revision = lesson.Flow.Revision;
            view.InvokeChoice(0);
            Check("Cinematic blocks choice callbacks", lesson.Flow.Revision == revision);
            lesson.Advance(lesson.settings.boardingSeconds + .01f); Visibility("Boarding", ElevatorStep.Boarding);
            view.InvokeChoice(0); Visibility("Proposal", ElevatorStep.Proposal);
            lesson.Advance(1); Visibility("Jump", ElevatorStep.Jump);
            lesson.Advance(lesson.settings.friendReplySeconds + lesson.settings.jumpSeconds + lesson.settings.afterJumpSeconds + .01f);
            Visibility("Stopped dialogue", ElevatorStep.Stopped);
            lesson.Advance(2); Visibility("Stopped investigation", ElevatorStep.Stopped);
            lesson.UnsafeDoor(); Visibility("Retry", ElevatorStep.Retry);
            view.InvokeChoice(0); Visibility("Return from retry", ElevatorStep.Stopped);
            lesson.Intercom(); Visibility("Connecting", ElevatorStep.Connecting);
            lesson.Advance(lesson.settings.connectionSeconds + .01f); Visibility("Call", ElevatorStep.Call);
            view.InvokeChoice(0); Visibility("Situation reported", ElevatorStep.Call);
            lesson.FindIdentity(); Visibility("Identity found", ElevatorStep.Call);
            view.InvokeChoice(0); Visibility("Location reported", ElevatorStep.Call);
            view.InvokeChoice(0); Visibility("Waiting", ElevatorStep.Waiting);
            view.InvokeChoice(0); Visibility("Reassured", ElevatorStep.Waiting);
            lesson.Advance(lesson.settings.rescueWaitSeconds + .01f); Visibility("Rescue", ElevatorStep.Rescue);
            lesson.Advance(lesson.settings.rescueSubtitleSeconds + .01f); Visibility("Ready to exit", ElevatorStep.Rescue);
            view.InvokeChoice(0); Visibility("Exiting", ElevatorStep.Exiting);
            lesson.Advance(lesson.settings.exitSeconds + .01f); Visibility("Complete", ElevatorStep.Complete);
            Check("Notebook shown on completion", view.spatial.notebook.gameObject.activeSelf);
            view.InvokeChoice(1); Visibility("Restart through button", ElevatorStep.Lobby);

            lesson.simulateNoResponse = true;
            lesson.DeveloperJump(3); lesson.Advance(2); lesson.Intercom();
            lesson.Advance(lesson.settings.connectionSeconds + .01f); Visibility("Unanswered", ElevatorStep.Unanswered);
            view.InvokeChoice(0); Visibility("Phone fallback", ElevatorStep.Call);

            var oldHud = view.spatial.hud.GetComponent<CanvasGroup>();
            Object.DestroyImmediate(oldHud);
            Visibility("HUD missing-component recovery", ElevatorStep.Call);
            Check("HUD recovery replaces the destroyed native component", view.spatial.hud.GetComponent<CanvasGroup>() != oldHud);
            for (int i = 0; i < 30; i++) view.SetCinematicVisibility();
            Check("Repeated visibility updates do not duplicate groups", view.GetComponents<CanvasGroup>().Length == 1 && view.spatial.hud.GetComponents<CanvasGroup>().Length == 1);

            var spatial = view.spatial;
            try
            {
                view.spatial = null;
                view.SetCinematicVisibility();
                var world = view.GetComponent<CanvasGroup>();
                Check("Non-spatial visibility path", world.alpha == (lesson.CinematicBlocking ? 0 : 1) && world.interactable == !lesson.CinematicBlocking && world.blocksRaycasts == world.interactable);
            }
            finally { view.spatial = spatial; }

            Check("M scene has no missing scripts", SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0));
            return Write(null);
        }
        catch (Exception error) { Write(error.ToString()); throw; }
        finally
        {
            if (temporary) Object.DestroyImmediate(temporary);
            lesson.simulateNoResponse = noResponse;
            lesson.Restart();
            lesson.presentation.space.overlay.Advance(1);
        }
    }

    static object Write(string error)
    {
        var report = new { completedUtc = DateTime.UtcNow.ToString("o"),
            mode = "Unity Editor Play Mode; PC; real UI callbacks with compressed lesson timers; no HMD",
            checks = Checks.ToArray(), passed = Checks.Count, error };
        File.WriteAllText("Assets/M/CodexTemp/ElevatorQA/CanvasGroupVerification_20261029.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }
}
