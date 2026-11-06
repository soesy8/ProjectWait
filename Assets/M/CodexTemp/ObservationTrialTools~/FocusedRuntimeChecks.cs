using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProjectWait.M.Elevator.ObservationTrial;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FocusedRuntimeChecks
{
    const string Evidence="Assets/M/Documentation/ObservationTrial_Dialogue_20261106/";
    static ElevatorLesson lesson;
    static ElevatorDesktopControls controls;
    static readonly List<string> checks=new List<string>();
    static readonly List<object> placements=new List<object>();
    static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks.Add(label);}
    static void Save(string name,object value){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+name+".json",JsonConvert.SerializeObject(value,Formatting.Indented));}
    static async Task Frames(int count=16){for(int i=0;i<count;i++){EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(30);}}
    static void Ready()
    {
        lesson=Object.FindFirstObjectByType<ElevatorLesson>();controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();
        if(!Application.isPlaying||!lesson||lesson.gameObject.scene.path!=ProjectWait.M.Editor.ElevatorObservationTrialSetup.ScenePath)throw new Exception("Play the isolated test scene first.");
    }
    static ElevatorObservation Target(ElevatorClue clue)=>lesson.CollectObservations().First(o=>o.clue==clue&&o.gameObject.activeInHierarchy);
    static void Activate(ElevatorClue clue)=>Target(clue).Activate();
    static void AdvanceFor(float seconds){while(seconds>0){float dt=Mathf.Min(.1f,seconds);lesson.Advance(dt);seconds-=dt;}}
    static void Face(Vector3 point)
    {
        var q=Quaternion.LookRotation(point-lesson.playerCamera.transform.position);
        controls.SetLookAngles(q.eulerAngles.y,Mathf.DeltaAngle(0,q.eulerAngles.x));
    }
    static async Task ClickObject(ElevatorClue clue)
    {
        await Frames();var target=Target(clue);
        var collider=target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().colliders.First(c=>c&&c.enabled&&c.gameObject.activeInHierarchy);
        Face(collider.bounds.center);await Frames(4);
        Check(controls.HoverTargetName==target.name,"centre ray resolves "+clue);
        controls.SelectCenterTarget();await Frames();
    }
    static async Task Click(int index)
    {
        await Frames();var button=lesson.view.ChoiceButtons[index];var r=(RectTransform)button.transform;
        Face(r.TransformPoint(r.rect.center));await Frames(4);
        Check(controls.HoverTargetName==button.name,"centre ray resolves "+button.name);
        controls.SelectCenterTarget();await Frames();
    }
    static bool Fits(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        return corners.Select(p=>lesson.playerCamera.WorldToViewportPoint(p)).All(p=>p.z>0&&p.x>=.01f&&p.x<=.99f&&p.y>=.01f&&p.y<=.99f);
    }
    static void CheckColumn(string stage)
    {
        Check(Fits(lesson.view.dialoguePanel)&&Fits(lesson.view.spatial.speakerTab)&&lesson.view.ChoiceButtons.All(b=>Fits((RectTransform)b.transform)),stage+": full ribbon, body and actions fit at placement");
        lesson.view.dialogueText.ForceMeshUpdate();
        Check(!lesson.view.dialogueText.isTextOverflowing&&lesson.view.ChoiceButtons.All(b=>!b.GetComponentInChildren<TMP_Text>().isTextOverflowing),stage+": Korean body and action labels do not overflow");
        var corners=new Vector3[4];Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
        foreach(var rect in new[]{lesson.view.dialoguePanel,lesson.view.spatial.speakerTab}.Concat(lesson.view.ChoiceButtons.Select(b=>(RectTransform)b.transform)))
        {
            rect.GetWorldCorners(corners);
            foreach(var corner in corners){var p=lesson.playerCamera.WorldToViewportPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        }
        var column=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        var regions=new List<ElevatorUIPlacement.Region>();ElevatorUIPlacement.Collect(lesson,regions);
        var overlaps=regions.Select(r=>new{name=r.name,weight=r.weight,area=ElevatorUIPlacement.Overlap(column,r.rect)}).ToArray();
        placements.Add(new{stage,column=new[]{column.x,column.y,column.width,column.height},overlaps});Save("PlacementMeasurements",placements);
        // This revision explicitly permits overlap in exchange for a near, readable
        // card. Record overlap, but do not retain the previous no-overlap requirement.
        Check(lesson.view.spatial.PlacedReadingDistance>=1.2f&&lesson.view.spatial.PlacedReadingDistance<=1.601f,stage+": opening distance stays near instead of retreating from obstacles");
        var body=lesson.view.dialogueText;var center=body.rectTransform.TransformPoint(body.rectTransform.rect.center);
        var a=lesson.playerCamera.WorldToViewportPoint(center);
        var b=lesson.playerCamera.WorldToViewportPoint(center+body.rectTransform.up*body.fontSize*body.rectTransform.lossyScale.y);
        Check(Mathf.Abs(a.y-b.y)*1080>=28,stage+": projected Korean em size remains at least 28px at 1080p");
    }
    static void CheckStatus(string contains,string stage)
    {
        var ui=lesson.view.spatial;
        Check(ui.mission.gameObject.activeInHierarchy&&ui.hud.GetComponent<CanvasGroup>().alpha>.99f&&lesson.view.title.text.Contains(contains),stage+": situation remains in the existing HUD");
        lesson.view.title.ForceMeshUpdate();lesson.view.subtitle.ForceMeshUpdate();
        Check(!lesson.view.title.isTextOverflowing&&!lesson.view.subtitle.isTextOverflowing&&lesson.view.subtitle.textInfo.lineCount<=2,stage+": situation and next action fit without shrinking text");
        var corners=new Vector3[4];ui.mission.GetWorldCorners(corners);
        var camera=ui.hud.renderMode==RenderMode.ScreenSpaceOverlay?null:lesson.playerCamera;
        Check(corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).All(p=>p.x>=Screen.width*.01f&&p.x<=Screen.width*.99f&&p.y>=Screen.height*.01f&&p.y<=Screen.height*.99f),stage+": HUD keeps all four edges within the screen");
        Check(!lesson.posterCutscene.enabled&&!lesson.posterCutscene.screenCanvas.gameObject.activeInHierarchy&&!lesson.posterCutscene.worldCanvas.gameObject.activeInHierarchy,stage+": no look-at arrow, ring or instruction is active");
    }
    static bool IsolatedFont(TMP_FontAsset font,HashSet<TMP_FontAsset> seen)
    {
        if(!font||!AssetDatabase.GetAssetPath(font).StartsWith("Assets/M/Arts/Fonts/ObservationTrial/",StringComparison.Ordinal))return false;
        return !seen.Add(font)||font.fallbackFontAssetTable.All(f=>IsolatedFont(f,seen));
    }
    static async Task<Color> Capture(string name,bool opaque=false)
    {
        await Frames();Directory.CreateDirectory(Evidence);
        // Native game-view dimensions keep the temporary camera HUD capture at the
        // same CanvasScaler ratio as the actual Overlay presentation.
        int width=Screen.width,height=Screen.height;
        var camera=lesson.playerCamera;var target=new RenderTexture(width,height,24);
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var hud=lesson.view.spatial.hud;var mode=hud.renderMode;float plane=hud.planeDistance;
        try
        {
            if(opaque)lesson.view.spatial.focusWindow.ResetWindow();
            camera.targetTexture=target;
            // Include the existing read-only HUD in a camera render; restore its actual
            // Overlay mode before yielding. No scene or asset is saved by capture.
            if(mode==RenderMode.ScreenSpaceOverlay){hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=camera;hud.planeDistance=.5f;}
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Evidence+name+".png",image.EncodeToPNG());
            return image.GetPixel(width/2+14,height/2+12);
        }
        finally{hud.renderMode=mode;hud.planeDistance=plane;camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(target);if(opaque)lesson.view.spatial.focusWindow.Advance(1);}
    }
    static async Task CheckFocusAndFacing()
    {
        var ui=lesson.view.spatial;var focus=ui.focusWindow;var panel=lesson.view.dialoguePanel;
        Check(Mathf.Abs(focus.RadiusTangents.x/Mathf.Tan(12*Mathf.Deg2Rad)-2)<.001f&&
            Mathf.Abs(focus.RadiusTangents.y/Mathf.Tan(10*Mathf.Deg2Rad)-2)<.001f,"focus window horizontal and vertical radii are exactly twice the previous version");
        // Aim at the paper margin rather than a letter or the centre-ray marker.
        var point=panel.TransformPoint(new Vector3(panel.rect.width*.43f,0,0));
        Face(point);await Frames(10);
        Check(focus.Amount>.95f&&focus.BackgroundCount>=4&&focus.OpacityAt(point)<.16f,"central gaze softly reveals only the registered background images");
        var eyes=lesson.playerCamera.transform;float depth=Vector3.Distance(eyes.position,point);
        var feather=eyes.position+eyes.forward*depth+eyes.right*(focus.RadiusTangents.x*depth*.72f);
        var outside=eyes.position+eyes.forward*depth+eyes.right*(focus.RadiusTangents.x*depth*1.2f);
        Check(focus.OpacityAt(feather)>.2f&&focus.OpacityAt(feather)<.9f&&focus.OpacityAt(outside)>.99f,"focus window has a feathered local edge, not whole-card fading");
        Check(!ElevatorUIFocusWindow.IsFocusMaterial(lesson.view.dialogueText.fontSharedMaterial)&&lesson.view.ChoiceButtons.All(b=>!ElevatorUIFocusWindow.IsFocusMaterial(b.targetGraphic.material)&&!b.GetComponent<ElevatorUIFocusRaycastFilter>()),"dialogue glyphs and action buttons are not made transparent or click-through");
        var reader=panel.Find("DialogueReader").GetComponent<ElevatorUIFocusRaycastFilter>();
        Check(reader&&!reader.Allows(point)&&!reader.IsRaycastLocationValid(lesson.playerCamera.WorldToScreenPoint(point),lesson.playerCamera),"transparent reader area uses the same pass-through rule for desktop and XR raycasts");
        var opaque=await Capture("05c_FocusOpaqueReference",true);var revealed=await Capture("05d_FocusWindow");
        Check(Vector3.Distance(new Vector3(opaque.r,opaque.g,opaque.b),new Vector3(revealed.r,revealed.g,revealed.b))>.05f,"camera-rendered background pixels actually change through the focus-window shader");
        var position=ui.transform.position;var cameraPosition=eyes.position;var rotation=ui.transform.rotation;
        eyes.position+=new Vector3(.28f,.04f,0);await Frames(4);
        Check(Vector3.Distance(position,ui.transform.position)<.001f&&Quaternion.Angle(rotation,ui.transform.rotation)>1&&Quaternion.Angle(ui.transform.rotation,Quaternion.LookRotation(position-eyes.position,Vector3.up))<.1f,"walking beside a card changes its facing without moving its world position");
        var cameraRotation=eyes.rotation;eyes.rotation*=Quaternion.Euler(0,0,25);ui.Tick();
        Check(Mathf.Abs(Vector3.Dot(ui.transform.right,Vector3.up))<.001f,"world cards do not copy head roll");
        eyes.SetPositionAndRotation(cameraPosition,cameraRotation);ui.Tick();
        var angles=controls.LookAngles;controls.SetLookAngles(angles.x+150,0);await Frames(10);
        Check(focus.Amount<.01f,"paper opacity returns when looking away without a hold or confirmation stage");
        controls.SetLookAngles(angles.x,angles.y);await Frames(4);
    }
    public static async Task<object> Review()
    {
        Ready();lesson.enabled=false;
        try
        {
            lesson.DeveloperJump(5);await Frames();Activate(ElevatorClue.Friend);await Frames();
            Face(lesson.friend.position+Vector3.up*1.24f);await Frames();
            await Capture("05e_EdgeWindowAndFacing");
            return new{status="CAPTURED",distance=lesson.view.spatial.PlacedReadingDistance};
        }
        finally{lesson.Restart();lesson.enabled=true;}
    }
    public static async Task<object> Run()
    {
        Ready();checks.Clear();placements.Clear();lesson.enabled=false;
        try
        {
            lesson.Restart();await Frames();Activate(ElevatorClue.LobbyCall);
            Check(lesson.Flow.Step==ElevatorStep.Entering,"lobby call remains a direct physical action");
            AdvanceFor(lesson.settings.boardingSeconds+.01f);Activate(ElevatorClue.FloorButtons);Activate(ElevatorClue.Ball);
            lesson.Advance(lesson.settings.shotStorySeconds+.01f);
            Check(lesson.Flow.Step==ElevatorStep.Jump&&string.IsNullOrEmpty(lesson.Flow.FirstJudgment),"introduction proceeds without a poster puzzle or ignored player choice");
            lesson.Advance(lesson.settings.friendReplySeconds+lesson.settings.jumpSeconds+lesson.settings.afterJumpSeconds+.01f);lesson.Advance(2);await Frames();
            Check(lesson.Flow.CanObserve&&lesson.view.spatial.mission.gameObject.activeInHierarchy&&!lesson.view.dialoguePanel.gameObject.activeInHierarchy,"help task uses one short HUD prompt, not a large clipped dialogue");
            lesson.view.subtitle.ForceMeshUpdate();
            Check(lesson.view.subtitle.textInfo.lineCount<=2&&!lesson.view.subtitle.isTextOverflowing,"task prompt uses two readable lines without a stranded Korean ending");
            var orange=lesson.CollectObservations().Where(o=>o.isActiveAndEnabled&&o.gameObject.activeInHierarchy&&o.PersistentSelectable).ToArray();
            Check(orange.Length==4&&orange.Select(o=>o.clue).OrderBy(c=>c).SequenceEqual(new[]{ElevatorClue.Friend,ElevatorClue.Intercom,ElevatorClue.Identity,ElevatorClue.Notice}.OrderBy(c=>c)),"four stable cabin choices remain visibly selectable without aiming first");
            Check(orange.All(o=>o.CanActivate&&o.outlineTarget.Visible&&o.outlineTarget.idleWidth>=2&&o.outlineTarget.idleColor.r>.95f&&o.outlineTarget.idleColor.g<.8f),"all four available choices share a thin orange outline, not answer-dependent colours");
            await Capture("01_HelpTask");
            await ClickObject(ElevatorClue.Door);
            Check(lesson.Flow.PreviewClue==ElevatorClue.Door&&!lesson.Flow.CanConfirmObject&&lesson.view.ChoiceButtons.Count==1,"door inspection is passive, with no invented unsafe action");
            CheckColumn("door inspection");await Capture("01b_DoorControlsClear");
            await Click(0);await ClickObject(ElevatorClue.FloorButtons);
            CheckColumn("side panel inspection");await Capture("01c_SidePanelClear");
            await ClickObject(ElevatorClue.Intercom);
            Check(lesson.Flow.Step==ElevatorStep.Connecting&&!lesson.Flow.HasObjectPreview,"one actual emergency-button click immediately starts connection");
            lesson.Advance(lesson.settings.connectionSeconds+.01f);lesson.Advance(lesson.settings.callQuestionSeconds+.01f);
            lesson.Advance(lesson.settings.callReplySeconds+.01f);lesson.Advance(lesson.settings.callQuestionSeconds+.01f);await Frames();
            lesson.Advance(120);Check(lesson.Flow.AwaitingCallContinue,"location directions have no automatic reading deadline");
            CheckColumn("location guide");await Capture("02_ReadableLocationGuide");await Click(0);
            Check(lesson.Flow.SearchingIdentity,"readiness action starts identity search");
            await ClickObject(ElevatorClue.Identity);
            Check(lesson.Flow.ReviewingIdentity&&!lesson.Flow.LocationReported,"identity inspection still waits for explicit reporting");
            CheckColumn("identity");await Capture("03_IdentityReport");await Click(0);
            Check(lesson.Flow.LocationReported,"explicit identity action reports location");
            lesson.Advance(lesson.settings.locationReportSeconds+.01f);await Frames();lesson.Advance(120);
            Check(lesson.Flow.Step==ElevatorStep.CallSafetyAdvice,"safety guidance also remains until read");
            CheckColumn("safety guide");await Click(0);
            var away=controls.LookAngles;controls.SetLookAngles(away.x+160,0);await Frames();
            CheckStatus("문에 기대려","missed door approach");
            var freeLook=controls.LookAngles;
            lesson.Advance(lesson.settings.doorLeanSeconds+.01f);await Frames();
            Check(Vector2.Distance(freeLook,controls.LookAngles)<.01f,"friend approach never steers the camera or waits for gaze");
            Check(!lesson.Flow.DoorPosterRead&&lesson.view.spatial.mission.gameObject.activeInHierarchy,"friend task starts without requiring the poster");
            CheckStatus("문에 기대려","return after missed approach");
            await Capture("04_FriendTask");
            lesson.Advance(40);lesson.scenario.AdvanceGuidance(40);lesson.posterCutscene.Advance(.2f);
            Check(lesson.scenario.GuidanceTarget==ElevatorClue.Friend&&!lesson.posterCutscene.Awaiting&&lesson.posterCutscene.RingAlpha==0,"delayed help guides to the friend, never an optional-poster ring");
            await ClickObject(ElevatorClue.Friend);
            Check(lesson.Flow.CanConfirmObject&&lesson.view.ChoiceButtons.Count==3&&!lesson.Flow.DoorWarningGiven,"friend click opens two replies and an observation return");
            var buttons=lesson.view.ChoiceButtons;var first=(Image)buttons[0].targetGraphic;var second=(Image)buttons[1].targetGraphic;
            Check(first.sprite==second.sprite&&first.color==second.color&&buttons[0].GetComponentInChildren<TMP_Text>().color==buttons[1].GetComponentInChildren<TMP_Text>().color,"both replies have identical artwork and colour");
            CheckColumn("friend replies");await Capture("05_FriendReplies");
            await CheckFocusAndFacing();
            var position=lesson.view.transform.position;var rotation=lesson.view.transform.rotation;var angles=controls.LookAngles;
            controls.SetLookAngles(angles.x+160,0);await Frames(8);
            Check(Vector3.Distance(position,lesson.view.transform.position)<.001f&&Quaternion.Angle(rotation,lesson.view.transform.rotation)<.1f,"reading card does not chase head movement");
            CheckStatus("문에 기대려","friend replies off-screen");await Capture("05b_OffscreenRecovery");
            controls.SetLookAngles(angles.x,angles.y);await Frames(4);
            float aspect=lesson.playerCamera.aspect;lesson.playerCamera.aspect=4f/3;lesson.view.ApplyCompactLayout();await Frames();CheckColumn("4:3 replies");
            lesson.playerCamera.aspect=aspect;lesson.view.ApplyCompactLayout();await Frames();
            var staleReply=buttons[1].onClick;await ClickObject(ElevatorClue.Door);staleReply.Invoke();await Frames();
            Check(!lesson.Flow.DoorRetry&&!lesson.Flow.CanConfirmObject,"changing target invalidates the previous reply button");
            await Click(0);await ClickObject(ElevatorClue.Friend);
            lesson.pauseMenu.Pause();float elapsed=lesson.Flow.Elapsed;lesson.ChooseUnsafeWall();lesson.Advance(20);await Frames();
            Check(lesson.Flow.Elapsed==elapsed&&!lesson.Flow.DoorRetry&&!Target(ElevatorClue.Friend).CanActivate,"pause blocks time, object actions and replies");
            lesson.pauseMenu.Resume();await Frames();await Click(1);
            Check(lesson.Flow.DoorRetry&&!lesson.Flow.DoorWarningGiven,"unsafe reply pauses safely before contact or injury");
            CheckColumn("local retry");await Capture("06_LocalRetry");await Click(0);
            Check(!lesson.Flow.HasObjectPreview&&lesson.Flow.LocationReported&&lesson.Flow.Step==ElevatorStep.DoorPosterPointing,"retry restores only the friend task and preserves reporting");
            await ClickObject(ElevatorClue.Friend);await Click(0);
            Check(lesson.Flow.DoorWarningGiven&&!lesson.Flow.DoorPosterRead,"safe friend reply works without ever inspecting the poster");
            controls.SetLookAngles(angles.x+160,0);await Frames();
            lesson.Advance(lesson.settings.doorWarningSeconds+.01f);await Frames();CheckStatus("문에서 떨어지고","missed safe-wall movement");
            lesson.Advance(lesson.settings.safeWallMoveSeconds+.01f);await Frames();
            Check(lesson.Flow.SafeWallReached&&lesson.Flow.Reassured,"friend moves to the safe wall and settles");
            CheckStatus("함께 기다려요","safe waiting after missed movement");await Capture("06b_SafeWaiting");
            lesson.Advance(lesson.settings.safeWallSettleSeconds+.01f);lesson.Advance(4.81f);await Frames();
            var rescueAngles=controls.LookAngles;controls.SetLookAngles(rescueAngles.x+160,0);await Frames();
            CheckStatus("도착했어요","rescue arrival while looking away");await Capture("06c_RescueArrival");
            lesson.Advance(lesson.settings.rescueSubtitleSeconds+.01f);await Frames();
            CheckStatus("안전 확인","rescue clearance while looking away");await Capture("06d_RescueClearance");
            lesson.Continue();AdvanceFor(lesson.settings.exitSeconds+.01f);lesson.Continue();AdvanceFor(3.51f);lesson.Continue();AdvanceFor(6.01f);await Frames();
            Check(lesson.Flow.Step==ElevatorStep.Complete&&lesson.view.spatial.completionMarks.All(t=>t.text=="완료"),"existing rescue and notebook record the three core actions plus guided exit");
            Face(lesson.view.spatial.notebook.position);await Capture("07_ActionNotebook");
            var texts=lesson.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true));
            Check(texts.All(t=>IsolatedFont(t.font,new HashSet<TMP_FontAsset>())),"all runtime fonts and fallbacks remain isolated");
            lesson.DeveloperJump(5);await Frames();await ClickObject(ElevatorClue.Notice);
            Check(lesson.Flow.DoorPosterRead&&!lesson.Flow.CanConfirmObject&&lesson.view.ChoiceButtons.Count==1,"optional poster is only reading material, not the answer gate");
            await Capture("08_OptionalPoster");await Click(0);await ClickObject(ElevatorClue.Friend);await Click(2);
            Check(!lesson.Flow.HasObjectPreview&&!lesson.Flow.DoorWarningGiven,"observation return cancels the conversation without choosing a reply");
            lesson.Restart();await Frames();
            Check(lesson.Flow.Step==ElevatorStep.Lobby&&!lesson.Flow.PreviewClue.HasValue&&!lesson.Flow.IdentityReviewOpen,"restart clears all interaction state");
            var report=new{status="PASS",count=checks.Count,checks,coverage="Deterministic Play Mode, real centre-ray object and UI events; not a novice usability study or HMD test."};Save("RuntimeValidation",report);return report;
        }
        catch(Exception e){Save("RuntimeValidation",new{status="FAIL",error=e.ToString(),checks,step=lesson.Flow.Step.ToString(),hovered=controls.HoverTargetName});throw;}
        finally{lesson.enabled=true;}
    }

    static async Task Wait(Func<bool> condition,int seconds)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();
        while(!condition()){if(clock.Elapsed.TotalSeconds>seconds)throw new Exception("Realtime timeout at "+lesson.Flow.Step);EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(50);}
        await Frames();
    }
    public static async Task<object> Realtime()
    {
        Ready();lesson.enabled=true;var clock=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            lesson.Restart();await Frames();Activate(ElevatorClue.LobbyCall);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.Boarding,12);Activate(ElevatorClue.FloorButtons);Activate(ElevatorClue.Ball);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.Stopped&&lesson.Flow.CanObserve,15);Activate(ElevatorClue.Intercom);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.CallIdentityGuide,25);lesson.view.InvokeChoice(0);
            await Wait(()=>lesson.Flow.SearchingIdentity,5);Activate(ElevatorClue.Identity);await Frames();lesson.view.InvokeChoice(0);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.CallSafetyAdvice,10);lesson.view.InvokeChoice(0);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.DoorPosterPointing,10);Activate(ElevatorClue.Friend);await Frames();lesson.view.InvokeChoice(0);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.Rescue&&lesson.Flow.ReadyToExit,35);Activate(ElevatorClue.Rescuer);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.ReturnLobby,10);Activate(ElevatorClue.Rescuer);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.GroundFloor,8);Activate(ElevatorClue.GroundExit);
            await Wait(()=>lesson.Flow.Step==ElevatorStep.Complete,10);
            if(!lesson.Flow.HelpRequested||!lesson.Flow.LocationReported||!lesson.Flow.SafeWallReached||lesson.Flow.DoorPosterRead)throw new Exception("Realtime completion flags do not match focused route.");
            var report=new{status="PASS",elapsedSeconds=Math.Round(clock.Elapsed.TotalSeconds,1),coverage="One full automatic-timing route. Lesson Update enabled; no manual Tick/Advance or developer stage skips. Actions invoked programmatically, not a human usability test."};
            Save("RealtimeValidation",report);lesson.Restart();return report;
        }
        catch(Exception e){Save("RealtimeValidation",new{status="FAIL",error=e.ToString(),step=lesson.Flow.Step.ToString(),elapsedSeconds=clock.Elapsed.TotalSeconds});throw;}
    }
}
