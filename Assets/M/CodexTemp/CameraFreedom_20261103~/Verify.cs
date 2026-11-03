using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class VerifyCameraFreedom
{
    const string Dir="Assets/M/CodexTemp/CameraFreedom_20261103~/";
    static readonly List<object> checks=new List<object>();
    static ElevatorLesson lesson;
    static ElevatorDesktopControls controls;
    static ElevatorPlayerMovement movement;
    static ElevatorSportsCentreMap map;
    static Mouse mouse;
    static Keyboard keyboard;
    static void Check(bool passed,string name)
    {
        checks.Add(new{name,passed});if(!passed)throw new Exception(name);
    }
    static void Place(Vector3 feet,float yaw=67,float pitch=-13)
    {
        var controller=movement.Controller;bool wasEnabled=controller.enabled;
        controller.enabled=false;movement.transform.position=feet-Vector3.up*movement.FeetOffset;
        controller.enabled=wasEnabled;movement.ResetVerticalMotion();controls.SetLookAngles(yaw,pitch);Physics.SyncTransforms();
    }
    static float LookDifference(Vector2 a,Vector2 b)=>Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(a.x,b.x)),Mathf.Abs(a.y-b.y));
    static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void SetPending(string name,object value)=>typeof(ElevatorDesktopControls).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controls,value);
    static object GetPending(string name)=>typeof(ElevatorDesktopControls).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controls);
    static async Task NoReframe(string name,Action action,int milliseconds=180)
    {
        var position=movement.transform.position;var originRotation=controls.xrOrigin.rotation;var look=controls.LookAngles;
        action();await Task.Delay(milliseconds);
        Check(Vector3.Distance(position,movement.transform.position)<.0001f,name+": no position recentering");
        Check(LookDifference(look,controls.LookAngles)<.001f&&Quaternion.Angle(originRotation,controls.xrOrigin.rotation)<.001f,name+": no forced gaze or rig rotation");
    }
    static void SettleOverlay(){lesson.presentation.space.overlay.Advance(10);}
    static async Task MoveMouse(Vector2 delta,bool down=false)
    {
        var state=new MouseState{position=new Vector2(4,4),delta=delta};if(down)state=state.WithButton(MouseButton.Left,true);
        InputSystem.QueueStateEvent(mouse,state);await Task.Delay(70);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(4,4)});await Task.Delay(70);
    }
    static async Task Keys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));await Task.Delay(100);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Task.Delay(100);
    }
    static async Task CinematicInputs(string name)
    {
        Check(lesson.Flow.IsCinematic&&lesson.CinematicBlocking,name+": cinematic gameplay lock active");
        SettleOverlay();var position=movement.transform.position;
        var observation=lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Notice&&o.isActiveAndEnabled);
        Check(!movement.CanMove&&!observation.CanActivate,name+": movement and observation activation blocked");
        Check(!observation.Process(null,null)&&!((IXRHoverFilter)observation).Process(null,null),name+": XR select and hover filters blocked");
        int revision=lesson.Flow.Revision,run=lesson.RunVersion,history=lesson.Flow.History.Count,clicks=controls.ClickCount;
        bool developer=lesson.view.developerPanel.gameObject.activeSelf;
        observation.Hover(true);observation.Activate();lesson.view.InvokeChoice(0);controls.SelectCenterTarget();
        Check(!observation.IsHovered&&lesson.Flow.Revision==revision&&lesson.Flow.History.Count==history,name+": direct object and UI entry points reject actions");
        var angles=controls.LookAngles;
        await MoveMouse(new Vector2(70,-40));
        Check(LookDifference(angles,controls.LookAngles)>1,name+": actual mouse input can still look around");
        movement.Move(Vector2.one,.4f,false);movement.Move(Vector2.one,.4f,true);
        await Keys(Key.W,Key.D,Key.Digit1,Key.R,Key.F8);await MoveMouse(Vector2.zero,true);
        Check(lesson.RunVersion==run&&lesson.Flow.Revision==revision&&lesson.Flow.History.Count==history,name+": click, choice, restart and developer shortcuts blocked");
        Check(controls.ClickCount==clicks&&lesson.view.developerPanel.gameObject.activeSelf==developer,name+": no hidden click or developer UI activation");
        Check(Vector3.Distance(position,movement.transform.position)<.0001f,name+": attempted walking cannot move player");
        Check((int)GetPending("pendingChoice")==-1&&!(bool)GetPending("pendingClick")&&!(bool)GetPending("pendingRestart")&&!(bool)GetPending("pendingDeveloper"),name+": blocked inputs are discarded, not queued");
        Check(!controls.centerLaser.enabled&&!controls.reticle.gameObject.activeSelf,name+": gameplay pointer is hidden");
        Check(!string.IsNullOrEmpty(lesson.view.dialogueText.text),name+": story dialogue remains available");
        var preserve=controls.LookAngles;await Task.Delay(150);
        Check(LookDifference(preserve,controls.LookAngles)<.001f,name+": no auto-rotation after mouse input ends");
    }
    public static async Task<object> Main()
    {
        checks.Clear();lesson=Object.FindFirstObjectByType<ElevatorLesson>();
        if(!Application.isPlaying||!lesson||lesson.Flow==null)throw new Exception("Enter Play Mode first.");
        controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();movement=controls.movement;map=lesson.presentation.space.sportsCentre;
        if(!controls.IsDesktop)throw new Exception("Desktop verification requires no active physical HMD.");
        bool lessonEnabled=lesson.enabled,movementEnabled=movement.enabled,background=Application.runInBackground;
        var settings=InputSystem.settings;var temporarySettings=Object.Instantiate(settings);
        var realMouse=Mouse.current;var realKeyboard=Keyboard.current;
        bool mouseEnabled=realMouse!=null&&realMouse.enabled,keyEnabled=realKeyboard!=null&&realKeyboard.enabled;
        string error=null;
        temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=temporarySettings;Application.runInBackground=true;
        if(mouseEnabled)InputSystem.DisableDevice(realMouse);if(keyEnabled)InputSystem.DisableDevice(realKeyboard);
        mouse=InputSystem.AddDevice<Mouse>("CameraFreedomMouse");keyboard=InputSystem.AddDevice<Keyboard>("CameraFreedomKeyboard");mouse.MakeCurrent();keyboard.MakeCurrent();
        lesson.enabled=false;movement.enabled=false;
        try
        {
            lesson.DeveloperJump(1);SettleOverlay();await Task.Delay(80);
            Place(map.inside+new Vector3(.22f,0,-.38f));
            Check(Vector3.Distance(movement.FeetPosition,map.inside)>.3f,"Test begins away from the old interior recenter anchor");
            await NoReframe("Select floor",()=>lesson.SelectFloor());
            await NoReframe("Take basketball / Proposal",()=>lesson.Continue(),750);
            await NoReframe("Inspect proposal",()=>lesson.ObserveProposal());
            await NoReframe("Jump story begins",()=>lesson.UnsafeProposal());
            await CinematicInputs("Jump");
            await NoReframe("Fault / Stopped dialogue",()=>lesson.Advance(lesson.settings.friendReplySeconds+lesson.settings.jumpSeconds+lesson.settings.afterJumpSeconds+.1f),1000);
            await CinematicInputs("Stopped dialogue");
            await NoReframe("Stopped investigation",()=>lesson.Advance(2));SettleOverlay();
            await NoReframe("Hint revision",()=>lesson.Advance(lesson.settings.secondHintSeconds+1));
            await NoReframe("Emergency call / Connecting",()=>lesson.Intercom());
            await CinematicInputs("Connecting");
            // With the overlay disabled, the flow itself must still protect actions.
            var overlay=lesson.presentation.space.overlay;bool overlayEnabled=overlay.enabled;overlay.enabled=false;
            Check(lesson.CinematicBlocking,"Flow-level cinematic lock does not depend on an active overlay component");overlay.enabled=overlayEnabled;
            await NoReframe("Call connected",()=>lesson.Advance(lesson.settings.connectionSeconds+.1f));SettleOverlay();
            await NoReframe("Report situation",()=>lesson.ReportSituation());
            await NoReframe("Find identity panel",()=>lesson.FindIdentity(),750);
            await NoReframe("Report location",()=>lesson.Report());
            await NoReframe("Follow waiting advice",()=>lesson.Advice());
            await NoReframe("Reassure friend / Waiting fade",()=>lesson.Reassure());
            await CinematicInputs("Waiting fade");
            await NoReframe("Rescuer arrival",()=>lesson.Advance(4.9f));
            await CinematicInputs("Rescue");
            lesson.Advance(lesson.settings.rescueSubtitleSeconds+.1f);SettleOverlay();
            Check(!lesson.CinematicBlocking&&movement.CanMove,"Movement/actions unlock after cinematic and fade finish");
            int clickRun=lesson.RunVersion;
            Check(clickRun==lesson.RunVersion&&!(bool)GetPending("pendingRestart"),"Cinematic restart input does not replay after unlock");
            // Pause is an intentional safety/menu exception during cinematics.
            lesson.DeveloperJump(2);SettleOverlay();lesson.UnsafeProposal();await Task.Delay(60);
            await Keys(Key.Escape);Check(lesson.IsPaused&&Time.timeScale==0,"Escape can pause during a cinematic");
            await Keys(Key.Escape);Check(!lesson.IsPaused&&Time.timeScale==1&&lesson.CinematicBlocking,"Escape resumes back into the cinematic lock");
            // A queued event at an input/update boundary is also discarded.
            SetPending("pendingRestart",true);SetPending("pendingDeveloper",true);SetPending("pendingChoice",0);SetPending("pendingClick",true);
            int protectedRun=lesson.RunVersion;Invoke(controls,"Update");
            Check(lesson.RunVersion==protectedRun&&!(bool)GetPending("pendingRestart")&&!(bool)GetPending("pendingDeveloper")&&(int)GetPending("pendingChoice")==-1,"Already-pending inputs cannot cross the cinematic boundary");
            // Check the safe branch separately: it used to reframe on every step.
            lesson.DeveloperJump(2);SettleOverlay();Place(map.inside+new Vector3(.22f,0,-.38f),102,16);
            await NoReframe("Safe travel starts",()=>lesson.SafeProposal());await CinematicInputs("Travel");
            await NoReframe("Safe travel fault",()=>lesson.Advance(3),1000);
            // Scripted path travel is preserved, without owning yaw or pitch.
            lesson.Restart();SettleOverlay();controls.SetLookAngles(41,-12);var gaze=controls.LookAngles;
            lesson.Continue();Check(lesson.Flow.Step==ElevatorStep.Entering,"Boarding path still starts");
            lesson.Advance(2);lesson.Advance(2);await Task.Delay(80);
            Check(LookDifference(gaze,controls.LookAngles)<.001f,"Boarding path does not turn camera");
            controls.SetLookAngles(118,9);lesson.Advance(.6f);lesson.Advance(lesson.settings.boardingSeconds);SettleOverlay();
            var boardingPosition=movement.transform.position;var boardingGaze=controls.LookAngles;await Task.Delay(150);
            Check(lesson.Flow.Step==ElevatorStep.Boarding&&Vector3.Distance(movement.FeetPosition,map.inside)<.02f,"Boarding path reaches elevator interior");
            Check(Vector3.Distance(boardingPosition,movement.transform.position)<.0001f&&LookDifference(boardingGaze,controls.LookAngles)<.001f,"Boarding completion does not snap or reframe");
            lesson.DeveloperJump(6);lesson.Advance(lesson.settings.rescueSubtitleSeconds+.1f);SettleOverlay();Place(map.inside,74,-17);
            lesson.Continue();gaze=controls.LookAngles;lesson.Advance(2);lesson.Advance(2);await Task.Delay(80);
            Check(lesson.Flow.Step==ElevatorStep.Exiting&&LookDifference(gaze,controls.LookAngles)<.001f,"Exit path preserves player gaze");
            lesson.Advance(lesson.settings.exitSeconds);SettleOverlay();
            Check(lesson.Flow.Step==ElevatorStep.ReturnLobby,"Exit still reaches landing");
            lesson.Continue();gaze=controls.LookAngles;lesson.Advance(2);await Task.Delay(80);
            Check(movement.FeetPosition.y<.1f&&LookDifference(gaze,controls.LookAngles)<.001f,"Faded stair/floor transition preserves gaze and reaches ground floor");
            lesson.Advance(2);SettleOverlay();lesson.Continue();gaze=controls.LookAngles;lesson.Advance(3);lesson.Advance(2.5f);await Task.Delay(80);
            Check(lesson.Flow.Step==ElevatorStep.GoingHome&&LookDifference(gaze,controls.LookAngles)<.001f,"Going-home path does not force camera rotation");
            lesson.Advance(1);SettleOverlay();Check(lesson.Flow.Step==ElevatorStep.Complete,"Full scenario still completes");
            // Input walk is tested without relying only on CanMove.
            lesson.DeveloperJump(4);SettleOverlay();Place(map.inside+new Vector3(.22f,0,-.38f));movement.enabled=true;
            var walkingStart=movement.transform.position;await Keys(Key.W);
            Check(Vector3.Distance(walkingStart,movement.transform.position)>.03f,"Actual WASD movement resumes outside cinematics");movement.enabled=false;
            lesson.ReportSituation();lesson.FindIdentity();SettleOverlay();
            var friend=lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Friend);
            var ball=lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Ball);
            Check(!friend.outlineTarget.geometry.Any(g=>g.sourceRenderer&&g.sourceRenderer.transform.IsChildOf(ball.transform)),"Previous friend/ball highlight separation is preserved");
            Check(lesson.view.spatial.storybook.thoughtBubblePrefab,"Previous current thought-bubble prefab is preserved");
        }
        catch(Exception exception){error=exception.ToString();}
        finally
        {
            if(lesson.IsPaused)lesson.pauseMenu.Resume();controls.ClearTarget();lesson.Restart();SettleOverlay();
            lesson.enabled=lessonEnabled;movement.enabled=movementEnabled;
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
            if(mouseEnabled)InputSystem.EnableDevice(realMouse);if(keyEnabled)InputSystem.EnableDevice(realKeyboard);realMouse?.MakeCurrent();realKeyboard?.MakeCurrent();
            InputSystem.settings=settings;Object.Destroy(temporarySettings);Application.runInBackground=background;
        }
        var result=new{success=error==null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,checks=checks.ToArray(),error,actualHMD=false};
        File.WriteAllText(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}