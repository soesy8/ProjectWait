using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProjectWait.M.EditorTools;
using ProjectWait.M.Elevator.DialoguePreview;
using ProjectWait.M.SceneTransition;
using ProjectWait.M.TitleScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Object = UnityEngine.Object;

// Explicitly invoked through Pipeline run_script. Never saves a scene or project settings.
public static class ResumeValidation
{
    const string Output = "Assets/M/CodexTemp/ResumeWork_20261116~";
    static readonly List<string> checks = new List<string>();
    [Serializable] sealed class Result
    {
        public string utc, mode;
        public bool passed;
        public string[] checks, errors;
    }
    static void Need(bool value, string text)
    {
        if (!value) throw new Exception(text);
        checks.Add("PASS: " + text);
    }
    static string Save(string mode, bool passed, IEnumerable<string> errors = null)
    {
        Directory.CreateDirectory(Output);
        string path = Output + "/" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + mode + ".json";
        File.WriteAllText(path, JsonUtility.ToJson(new Result {
            utc = DateTime.UtcNow.ToString("O"), mode = mode, passed = passed,
            checks = checks.ToArray(), errors = errors?.ToArray() ?? Array.Empty<string>()
        }, true));
        return path;
    }
    public static string Edit()
    {
        checks.Clear();
        try
        {
            Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Editor is outside Play Mode");
            VRUIComfortValidation.Run();
            Need(!File.ReadAllLines("Assets/M/CodexTemp/VRUIComfort_20261113~/validation.txt").Any(s => s.StartsWith("FAIL:")), "Latest UI suite in original project");
            VRImprovementValidation.Run();
            Need(!File.ReadAllLines("Assets/M/CodexTemp/VRImprovement_20261113~/validation.txt").Any(s => s.StartsWith("FAIL:")), "Saved scene / controller / stereo suite");
            foreach (string path in new[] { "Assets/M/Scene/Wait_Robby.unity", "Assets/M/Scene/Wait_Elevator.unity" })
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                    Need(all.All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), path + ": no Missing Script");
                    int missing = 0;
                    foreach (var component in all.SelectMany(t => t.GetComponents<Component>()).Where(c => c))
                    {
                        var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceInstanceIDValue != 0 && !property.objectReferenceValue)
                                missing++;
                    }
                    Need(missing == 0, path + ": no broken serialized object reference");
                    var materials = all.SelectMany(t => t.GetComponents<Renderer>()).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
                    Need(materials.All(m => m.shader && !ShaderUtil.ShaderHasError(m.shader)), path + ": renderer shaders resolve without errors");
                    Need(!AssetDatabase.GetDependencies(path, true).Any(p => p.StartsWith("Assets/M/CodexTemp/")), path + ": no temporary-folder dependency");
                    var preview = all.FirstOrDefault(t => t.name == "VR_Preview_Test");
                    if (preview) Need(!preview.gameObject.activeSelf && preview.CompareTag("EditorOnly"), "Mock preview remains disabled and EditorOnly");
                    var controls = all.Select(t => t.GetComponent<ElevatorDesktopControls>()).FirstOrDefault(c => c);
                    if (controls) Need(controls.enabled && controls.movement && controls.xrOrigin, "PC_Test_Controls is required PC/XR spawn/input, not disposable test code");
                    var effect = all.Select(t => t.GetComponent<LobbyDissolveTest>()).FirstOrDefault(c => c);
                    Need(effect && (effect.useSceneMaterials || all.Any(t => { var transition = t.GetComponent<StorybookSceneTransition>(); return transition && transition.lobbyFade == effect; })), path + ": droplet implementation retained for official scene transition");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            checks.Add("NOT TESTED: headset optics/comfort/native input; APK/performance; team-scene integration.");
            return Save("edit", true);
        }
        catch (Exception ex) { Save("edit", false, new[] { ex.ToString() }); throw; }
    }
    static void Pump() { EditorApplication.QueuePlayerLoopUpdate(); }
    public static async Task<string> Smoke()
    {
        checks.Clear(); var errors = new List<string>();
        Application.LogCallback collect = (text, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text + "\n" + stack);
        };
        bool background = Application.runInBackground;
        Application.logMessageReceived += collect; EditorApplication.update += Pump;
        try
        {
            Need(EditorApplication.isPlaying && !EditorApplication.isPaused, "Live unpaused Play Mode");
            Need(!XRSettings.isDeviceActive, "PC verification does not replace a live headset session");
            Application.runInBackground = true;
            int first = Time.frameCount;
            for (int i = 0; i < 50 && Time.frameCount <= first; i++) { Pump(); await Task.Delay(30); }
            Need(Time.frameCount > first, "Player frames actually advance");
            var portals = Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None);
            Need(portals.Length == 3, "Three lobby portals available");
            foreach (var portal in portals.Where(p => string.IsNullOrEmpty(p.destinationScene)))
            {
                portal.Activate(); Need(!portal.IsLoading && portal.statusText.text == "준비 중", "Unconnected exhibit safely shows preparation notice: " + portal.experienceTitle);
            }
            portals.Single(p => p.destinationScene.EndsWith("Wait_Elevator.unity")).Activate();
            for (int i = 0; i < 1000 && (SceneManager.GetActiveScene().name != "Wait_Elevator" || StorybookSceneTransition.IsBusy); i++)
            {
                var transition = StorybookSceneTransition.Current;
                if (transition) transition.Advance(.1f);
                Pump(); await Task.Delay(20);
            }
            Need(SceneManager.GetActiveScene().name == "Wait_Elevator" && !StorybookSceneTransition.IsBusy, "Actual lobby portal loads elevator and finishes reveal");
            var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
            Need(lesson && lesson.Flow != null && lesson.SpawnReady, "Original elevator scene initialized and spawn ready");
            await Route(lesson, false, false);
            await Route(lesson, true, true);
            Need(errors.Count == 0, "No runtime errors during full PC callbacks/playthrough");
            checks.Add("NOTE: scenario time advanced deterministically; not physical mouse/controller input or frame-time measurement.");
            return Save("pc-smoke", true, errors);
        }
        catch (Exception ex) { errors.Add(ex.ToString()); Save("pc-smoke", false, errors); throw; }
        finally
        {
            var menu = Object.FindFirstObjectByType<ElevatorPauseMenu>(); if (menu && menu.IsPaused) menu.Resume();
            Application.runInBackground = background;
            Application.logMessageReceived -= collect; EditorApplication.update -= Pump;
        }
    }
    static ElevatorCinematicOverlay Overlay(ElevatorLesson lesson) => lesson.presentation.space.overlay;
    static void Tick(ElevatorLesson lesson)
    {
        lesson.Advance(.1f); Overlay(lesson).Advance(.1f);
        lesson.view.spatial.Tick(); lesson.view.spatial.Transitions?.Advance(.1f);
    }
    static void Activate(ElevatorLesson lesson, ElevatorClue clue)
    {
        var target = lesson.CollectObservations().FirstOrDefault(o => o.clue == clue && o.CanActivate);
        if (!target) throw new Exception("No active world callback for " + clue + " at " + lesson.Flow.Step);
        target.Activate();
    }
    static async Task Route(ElevatorLesson lesson, bool phone, bool floorFirst)
    {
        lesson.simulateNoResponse = phone; lesson.Restart();
        bool paused = false, retried = false; var visited = new HashSet<ElevatorStep>();
        for (int i = 0; i < 5000 && lesson.Flow.Step != ElevatorStep.Complete; i++)
        {
            var f = lesson.Flow; visited.Add(f.Step); Tick(lesson);
            if (!paused && f.Step == ElevatorStep.CallResponse)
            {
                lesson.view.spatial.Transitions?.Advance(1); lesson.view.SetCinematicVisibility();
                var speech = lesson.GetComponentInChildren<ElevatorPlayerSpeechPanel>(true);
                Need(speech && speech.IsVisible && lesson.view.GetComponent<CanvasGroup>().alpha == 0, "Player response uses only bottom screen panel: " + (phone ? "119" : "management"));
                float elapsed = f.Elapsed; lesson.pauseMenu.Pause(); lesson.Advance(1);
                Need(lesson.IsPaused && f.Elapsed == elapsed && !speech.IsVisible, "Pause stops story time and hides player panel");
                lesson.pauseMenu.Resume(); Need(!lesson.IsPaused && speech.IsVisible, "Resume restores first-person speech"); paused = true;
            }
            if (!lesson.CinematicBlocking)
            {
                if (f.Step == ElevatorStep.Lobby) Activate(lesson, ElevatorClue.LobbyCall);
                else if (f.Step == ElevatorStep.Boarding)
                {
                    if (floorFirst ? !f.DestinationSelected : f.BallReceived) Activate(lesson, ElevatorClue.FloorButtons);
                    else Activate(lesson, ElevatorClue.Ball);
                }
                else if (f.Step == ElevatorStep.Stopped) Activate(lesson, ElevatorClue.Intercom);
                else if (f.Step == ElevatorStep.Unanswered) Activate(lesson, ElevatorClue.Phone);
                else if (f.SearchingIdentity) Activate(lesson, ElevatorClue.Identity);
                else if (f.ReviewingIdentity) lesson.view.InvokeChoice(0);
                else if (f.Step == ElevatorStep.DoorPosterPointing)
                {
                    if (!f.CanConfirmObject) Activate(lesson, ElevatorClue.Friend);
                    else lesson.view.InvokeChoice(phone && !retried ? 1 : 0);
                }
                else if (f.Step == ElevatorStep.Retry) { retried = true; lesson.view.InvokeChoice(0); }
                else if (f.Step == ElevatorStep.Rescue && f.ReadyToExit || f.Step == ElevatorStep.ReturnLobby) Activate(lesson, ElevatorClue.Rescuer);
                else if (f.Step == ElevatorStep.GroundFloor) Activate(lesson, ElevatorClue.GroundExit);
            }
            if (i % 20 == 0) { Pump(); await Task.Delay(10); }
        }
        Need(lesson.Flow.Step == ElevatorStep.Complete && lesson.Floor == 1, "Full route reaches ground-floor completion: " + (phone ? "119 / floor first" : "management / ball first"));
        Need(lesson.Flow.HelpRequested && lesson.Flow.LocationReported && lesson.Flow.SafeWallReached && lesson.Flow.ReadyToExit, "Completion retains all safety milestones");
        Need(lesson.Flow.UsedPhone == phone && paused && (!phone || retried), "Call route / pause / local retry exercised");
        Need(visited.Contains(ElevatorStep.Returning) && visited.Contains(ElevatorStep.GoingHome), "Stairs and home sequence exercised");
        int run = lesson.RunVersion; lesson.Restart();
        Need(lesson.RunVersion == run + 1 && !lesson.Flow.HelpRequested && !lesson.Flow.LocationReported, "Restart clears progress without reloading assets");
    }
    public static string PlayerVisual()
    {
        Need(EditorApplication.isPlaying && !XRSettings.isDeviceActive && !StorybookSceneTransition.IsBusy, "PC visual preview only");
        var lesson = Object.FindFirstObjectByType<ElevatorLesson>(); lesson.simulateNoResponse = false; lesson.Restart();
        for (int i = 0; i < 2000 && lesson.Flow.Step != ElevatorStep.CallResponse; i++)
        {
            Tick(lesson); if (lesson.CinematicBlocking) continue;
            switch (lesson.Flow.Step)
            {
                case ElevatorStep.Lobby: Activate(lesson, ElevatorClue.LobbyCall); break;
                case ElevatorStep.Boarding: Activate(lesson, lesson.Flow.BallReceived ? ElevatorClue.FloorButtons : ElevatorClue.Ball); break;
                case ElevatorStep.Stopped: Activate(lesson, ElevatorClue.Intercom); break;
            }
        }
        Need(lesson.Flow.Step == ElevatorStep.CallResponse, "Player line reached through original callbacks");
        lesson.view.spatial.Transitions?.Advance(1); Overlay(lesson).Advance(1); lesson.view.SetCinematicVisibility();
        EditorApplication.isPaused = true;
        return Save("player-visual-setup", true);
    }
}
