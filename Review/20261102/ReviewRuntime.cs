using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using ProjectWait.M.Elevator;
using DwtD.TestScene;
using Object = UnityEngine.Object;

public static class ProjectRuntimeReview
{
    const string Root = "Review/20261102/";
    static readonly List<object> checks = new List<object>();
    static ElevatorLesson L => Object.FindFirstObjectByType<ElevatorLesson>();
    static ElevatorDesktopControls Controls => Object.FindFirstObjectByType<ElevatorDesktopControls>();
    static void Record(string name, bool passed, object detail = null) => checks.Add(new { name, passed, detail });
    static async Task Wait(Func<bool> condition, string label, int seconds = 16)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < end) await Task.Delay(50);
        Record(label, condition());
        if (!condition()) throw new Exception("Timed out: " + label);
    }
    static async Task Shot(int number, string name)
    {
        string path = Root + number.ToString("00") + "_" + name + ".png";
        if (number < 1 || number > 20) throw new Exception("Capture limit is 20.");
        if (File.Exists(path)) throw new Exception("Capture already exists: " + path);
        Canvas.ForceUpdateCanvases();
        await Awaitable.EndOfFrameAsync();
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(200);
        await Wait(() => File.Exists(path), "Screenshot " + number + " saved", 5);
        File.AppendAllText(Root + "captures.jsonl", Newtonsoft.Json.JsonConvert.SerializeObject(new {
            number, name, path, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            frame = Time.frameCount, time = Time.unscaledTime, width = Screen.width, height = Screen.height,
            step = L ? L.Flow.Step.ToString() : null
        }) + Environment.NewLine);
    }
    static async Task Settle(int milliseconds = 1600) { await Task.Delay(milliseconds); }
    static async Task Step(ElevatorStep step, int seconds = 16) => await Wait(() => L.Flow.Step == step, "Reached " + step, seconds);
    static async Task Aim(ElevatorClue clue)
    {
        var o = L.CollectObservations().First(x => x.clue == clue && x.gameObject.activeInHierarchy);
        var coll = o.GetComponent<Collider>();
        Vector3 target = coll ? coll.bounds.center : o.transform.position;
        var angles = Quaternion.LookRotation(target - L.playerCamera.transform.position).eulerAngles;
        Controls.SetLookAngles(angles.y, Mathf.DeltaAngle(0, angles.x));
        await Settle(250);
        Controls.SendMessage("UpdateCenterRay");
        Record("Center ray targets " + clue, Controls.HoverTargetName == o.name, new { expected = o.name, actual = Controls.HoverTargetName });
    }
    public static async Task<object> Main(string part)
    {
        Directory.CreateDirectory(Root);
        string error = null;
        var errors = new List<string>();
        void OnLog(string message, string stack, LogType type) {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }
        Application.logMessageReceived += OnLog;
        try
        {
            if (part == "setup")
            {
                var assembly = typeof(Editor).Assembly;
                var type = assembly.GetType("UnityEditor.GameView");
                var view = EditorWindow.GetWindow(type);
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = sizesType.GetProperty("currentGroupType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sizes);
                var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var enumType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(enumType, "FixedResolution"), 1600, 900, "Review 1600x900" });
                var prop = type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                File.WriteAllText(Root + "editor_before.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    background = Application.runInBackground, sizeIndex = prop.GetValue(view)
                }));
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
                prop.SetValue(view, count - 1);
                view.Focus();
                Application.runInBackground = true;
                Record("Review Game view configured", true, new { index = count - 1 });
            }
            else
            {
                if (!Application.isPlaying) throw new Exception("Play mode required.");
                int startFrame = Time.frameCount;
                await Settle(300);
                Record("Player frames advance", Time.frameCount > startFrame, new { startFrame, endFrame = Time.frameCount });
                if (Time.frameCount <= startFrame) throw new Exception("Player loop is frozen.");
                if (part == "original1")
                {
                    await Settle(); await Shot(1, "elevator_lobby");
                    L.Continue(); await Step(ElevatorStep.Boarding); await Settle(); await Shot(2, "elevator_boarding");
                    L.Activate(ElevatorClue.FloorButtons); Record("1F destination selected", L.Flow.DestinationSelected);
                    L.Activate(ElevatorClue.Ball); await Step(ElevatorStep.Proposal); await Settle(); await Shot(3, "elevator_choices");
                    L.UnsafeProposal(); await Step(ElevatorStep.Stopped); await Wait(() => L.Flow.CanObserve, "Fault enters investigation"); await Settle();
                    await Shot(4, "elevator_fault");
                }
                else if (part == "original2")
                {
                    L.Activate(ElevatorClue.Intercom); await Step(ElevatorStep.Call); await Settle(); await Shot(5, "elevator_intercom");
                    L.view.InvokeChoice(0); Record("Situation reported through Next", L.Flow.SituationReported);
                    await Aim(ElevatorClue.Identity); await Settle(600); await Shot(6, "elevator_identity");
                    Controls.SelectCenterTarget();
                    Record("Identity selected through center ray", L.Flow.IdentityFound);
                    if (!L.Flow.IdentityFound) L.Activate(ElevatorClue.Identity);
                    await Settle(); L.view.InvokeChoice(0); Record("Location reported through Next", L.Flow.LocationReported);
                    L.view.InvokeChoice(0); await Step(ElevatorStep.Waiting); await Settle(); await Shot(7, "elevator_reassure");
                    L.Activate(ElevatorClue.Friend); await Step(ElevatorStep.Rescue); await Wait(() => L.Flow.ReadyToExit, "Rescuer verifies exit"); await Settle(500); await Shot(8, "elevator_rescue");
                    L.view.InvokeChoice(0); await Step(ElevatorStep.ReturnLobby); await Settle(500); L.view.InvokeChoice(0);
                    await Step(ElevatorStep.GroundFloor); await Settle(); await Shot(9, "elevator_ground_floor");
                    L.Activate(ElevatorClue.GroundExit); await Step(ElevatorStep.Complete); await Settle(); await Shot(10, "elevator_complete");
                    Record("Elevator full route completed", L.Flow.HelpRequested && L.Flow.LocationReported && L.Flow.Reassured && L.Flow.ReadyToExit, L.Flow.History.ToArray());
                }
                else if (part == "guided")
                {
                    Record("Object interaction scene enabled", L.scenario.objectGuidedScene);
                    L.Activate(ElevatorClue.LobbyCall); await Step(ElevatorStep.Boarding); await Settle(700);
                    L.Activate(ElevatorClue.FloorButtons); L.Activate(ElevatorClue.Ball); await Step(ElevatorStep.Proposal); await Settle();
                    await Shot(11, "guided_object_choices");
                    L.SafeProposal(); await Step(ElevatorStep.Stopped); await Wait(() => L.Flow.CanObserve, "Safe choice also reaches equipment fault");
                    await Wait(() => L.CanInteract(ElevatorClue.Intercom), "Fault camera transition releases input");
                    L.simulateNoResponse = true; L.Activate(ElevatorClue.Intercom); await Step(ElevatorStep.Unanswered); await Settle(); await Shot(12, "guided_no_response");
                    L.Activate(ElevatorClue.Phone); await Step(ElevatorStep.Call); Record("119 simulation used", L.Flow.UsedPhone);
                    L.pauseMenu.Toggle(); await Settle(400); float elapsed = L.Flow.Elapsed; int frame = Time.frameCount; await Settle(400);
                    Record("Pause freezes narrative while frames advance", L.IsPaused && Mathf.Abs(L.Flow.Elapsed - elapsed) < .001f && Time.frameCount > frame);
                    await Shot(13, "guided_pause"); L.pauseMenu.Toggle();
                }
                else if (part == "guidedresume")
                {
                    await Wait(() => L.CanInteract(ElevatorClue.Intercom), "Fault camera transition releases input");
                    L.simulateNoResponse = true; L.Activate(ElevatorClue.Intercom); await Step(ElevatorStep.Unanswered); await Settle(); await Shot(12, "guided_no_response");
                    L.Activate(ElevatorClue.Phone); await Step(ElevatorStep.Call); Record("119 simulation used", L.Flow.UsedPhone);
                    await Settle(); L.pauseMenu.Toggle(); await Settle(400); float elapsed = L.Flow.Elapsed; int frame = Time.frameCount; await Settle(400);
                    Record("Pause freezes narrative while frames advance", L.IsPaused && Mathf.Abs(L.Flow.Elapsed - elapsed) < .001f && Time.frameCount > frame);
                    await Shot(13, "guided_pause"); L.pauseMenu.Toggle();
                }
                else if (part == "guidedfinish")
                {
                    L.view.InvokeChoice(0); Record("Guided call reports situation", L.Flow.SituationReported);
                    await Settle(300); L.Activate(ElevatorClue.Identity); await Settle();
                    L.view.InvokeChoice(0); L.view.InvokeChoice(0); await Step(ElevatorStep.Waiting); await Settle(700);
                    L.Activate(ElevatorClue.Friend); await Step(ElevatorStep.Rescue); await Wait(() => L.Flow.ReadyToExit, "Guided rescuer verifies exit");
                    await Wait(() => L.CanInteract(ElevatorClue.Rescuer), "Rescue camera transition releases input");
                    L.Activate(ElevatorClue.Rescuer); await Step(ElevatorStep.ReturnLobby); await Settle(700);
                    L.Activate(ElevatorClue.Rescuer); await Step(ElevatorStep.GroundFloor); await Settle(700);
                    L.Activate(ElevatorClue.GroundExit); await Step(ElevatorStep.Complete);
                    Record("Guided 119 route completes", L.Flow.UsedPhone && L.Flow.HelpRequested && L.Flow.LocationReported && L.Flow.Reassured, L.Flow.History.ToArray());
                }
                else if (part == "guidedexit")
                {
                    await Wait(() => L.CanInteract(ElevatorClue.Rescuer), "Rescue camera transition releases input");
                    L.Activate(ElevatorClue.Rescuer); await Step(ElevatorStep.ReturnLobby);
                    await Wait(() => L.CanInteract(ElevatorClue.Rescuer), "Lobby transition releases input");
                    L.Activate(ElevatorClue.Rescuer); await Step(ElevatorStep.GroundFloor);
                    await Wait(() => L.CanInteract(ElevatorClue.GroundExit), "Ground floor transition releases input");
                    L.Activate(ElevatorClue.GroundExit); await Step(ElevatorStep.Complete);
                    Record("Guided 119 route completes", L.Flow.UsedPhone && L.Flow.HelpRequested && L.Flow.LocationReported && L.Flow.Reassured, L.Flow.History.ToArray());
                }
                else if (part == "corridor")
                {
                    var p = Object.FindFirstObjectByType<WetCorridorPrototype>();
                    p.Begin(); await Settle(700); await Shot(14, "corridor_observation");
                    p.OpenChoices(); await Settle(500); await Shot(15, "corridor_choices");
                    p.ChooseUnsafe(true); await Wait(() => p.CurrentPhase == WetCorridorPrototype.Phase.UnsafeExplanation, "Unsafe corridor route shows retry");
                    await Settle(400); await Shot(16, "corridor_retry");
                    p.Retry(); Record("Retry returns to choices", p.CurrentPhase == WetCorridorPrototype.Phase.Choosing && p.RetryCount == 1);
                    p.StopFriend(); p.ChooseDryPath(); await Wait(() => p.CurrentPhase == WetCorridorPrototype.Phase.Reward, "Safe corridor route reaches reward");
                    await Settle(500); await Shot(17, "corridor_reward");
                    p.CollectStamp(); Record("One stamp awarded", p.StampCount == 1); p.CollectStamp(); Record("Stamp is not duplicated", p.StampCount == 1);
                }
                else if (part == "friendcorridor")
                {
                    var p = Object.FindFirstObjectByType<WetCorridorPrototype>(); p.Begin(); p.OpenChoices(); p.ChooseUnsafe(false);
                    await Wait(() => p.CurrentPhase == WetCorridorPrototype.Phase.UnsafeExplanation, "Friend model unsafe animation reaches retry"); await Settle(600);
                    await Shot(18, "friend_corridor_fall");
                    p.Retry(); p.StopFriend(); p.ChooseDryPath(); await Wait(() => p.CurrentPhase == WetCorridorPrototype.Phase.Reward, "Friend model safe route reaches reward");
                    p.CollectStamp(); Record("Friend model route awards stamp", p.StampCount == 1);
                }
                else if (part == "animation")
                {
                    var p = Object.FindFirstObjectByType<ProjectWait.M.FriendAnimationPreview>(); p.ManualWalk(); await Settle(500);
                    var a = p.actor.GetCurrentAnimatorStateInfo(0).normalizedTime; await Settle(350);
                    Record("Walking animation advances", p.actor.GetCurrentAnimatorStateInfo(0).normalizedTime > a);
                    p.ManualFall(); await Settle(1600); await Shot(19, "friend_animation_preview");
                    p.Idle(); await Settle(300); Record("Idle animation selectable", p.CurrentMotion == "Idle");
                }
                else if (part == "empty") { await Settle(600); await Shot(20, "new_scene"); }
                else throw new Exception("Unknown part " + part);
            }
        }
        catch (Exception ex) { error = ex.ToString(); }
        finally { Application.logMessageReceived -= OnLog; }
        var report = new { part, success = error == null, checks, errors, error };
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Root + part + ".json", json);
        return json;
    }
}
