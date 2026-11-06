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

public static class ObservationTrialRuntimeChecks
{
    const string Evidence = "Assets/M/Documentation/ObservationTrial_Confirmation_20261106/";
    static readonly List<string> checks = new List<string>();
    static ElevatorLesson lesson;
    static ElevatorDesktopControls controls;
    static void Check(bool pass, string label) { if (!pass) throw new Exception(label); checks.Add(label); }
    static async Task Frames(int count = 4) { for (int i = 0; i < count; i++) { EditorApplication.QueuePlayerLoopUpdate(); await Task.Delay(30); } }
    static void Save(string name, object value) { Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence + name + ".json", JsonConvert.SerializeObject(value, Formatting.Indented)); }
    static async Task Settle() { await Frames(16); }
    static void AdvanceFor(float seconds)
    {
        while(seconds>0) { float step=Mathf.Min(.1f,seconds); lesson.Advance(step); seconds-=step; }
    }
    static void Face(Vector3 position)
    {
        Vector3 direction = position - lesson.playerCamera.transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        controls.SetLookAngles(rotation.eulerAngles.y, Mathf.DeltaAngle(0, rotation.eulerAngles.x));
    }
    static ElevatorObservation Target(ElevatorClue clue) => lesson.CollectObservations().First(o => o.clue == clue && o.gameObject.activeInHierarchy);
    static void Activate(ElevatorClue clue) { Target(clue).Activate(); }
    static bool IsolatedFont(TMP_FontAsset font, HashSet<TMP_FontAsset> visited)
    {
        if(!font||!AssetDatabase.GetAssetPath(font).StartsWith("Assets/M/Arts/Fonts/ObservationTrial/",StringComparison.Ordinal))return false;
        if(!visited.Add(font))return true;
        return font.fallbackFontAssetTable.All(f=>IsolatedFont(f,visited));
    }
    static async Task ClickObject(ElevatorClue clue)
    {
        await Settle();
        var target=Target(clue);
        var interaction=target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        var collider=interaction.colliders.First(c=>c&&c.enabled&&c.gameObject.activeInHierarchy);
        Face(collider.bounds.center); await Frames(4);
        Check(controls.HoverTargetName==target.name,"centre ray resolves world clue "+clue);
        controls.SelectCenterTarget(); await Frames(4);
    }
    static async Task Click(int index)
    {
        await Settle();
        Button button = lesson.view.ChoiceButtons[index];
        var rect = (RectTransform)button.transform;
        Face(rect.TransformPoint(rect.rect.center));
        await Frames(4);
        Check(controls.HoverTargetName == button.name, "centre ray resolves actual " + button.name);
        controls.SelectCenterTarget();
        await Frames(4);
    }
    static async Task Capture(string name)
    {
        await Settle();
        var texture = new RenderTexture(1440, 900, 24);
        var old = lesson.playerCamera.targetTexture;
        var active = RenderTexture.active;
        var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
        try
        {
            lesson.playerCamera.targetTexture = texture;
            lesson.playerCamera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Evidence + name + ".png", image.EncodeToPNG());
        }
        finally { lesson.playerCamera.targetTexture = old; RenderTexture.active = active; Object.DestroyImmediate(image); Object.DestroyImmediate(texture); }
    }
    static void Ready()
    {
        if (!Application.isPlaying) throw new Exception("Start Play Mode in the isolated trial first.");
        lesson = Object.FindFirstObjectByType<ElevatorLesson>();
        controls = Object.FindFirstObjectByType<ElevatorDesktopControls>();
        if (!lesson || lesson.gameObject.scene.path != ProjectWait.M.Editor.ElevatorObservationTrialSetup.ScenePath) throw new Exception("Wrong scene.");
    }
    public static async Task<object> Run()
    {
        Ready(); checks.Clear();
        // Manual state advancement uses the same lesson API as Update. Freeze only
        // automatic story ticks for deterministic checks; animations and input stay live.
        lesson.enabled = false;
        try
        {
            lesson.Restart(); await Settle();
            Activate(ElevatorClue.LobbyCall);
            Check(lesson.Flow.Step==ElevatorStep.Entering&&!lesson.Flow.HasObjectPreview,"initial lobby call immediately starts boarding without confirmation");
            AdvanceFor(lesson.settings.boardingSeconds + .01f);
            Activate(ElevatorClue.FloorButtons);
            Check(lesson.Flow.DestinationSelected&&!lesson.Flow.HasObjectPreview,"initial floor button remains a direct action");
            Activate(ElevatorClue.Ball); lesson.Advance(lesson.settings.shotStorySeconds + .01f);
            await Settle();
            Check(lesson.Flow.Step == ElevatorStep.PosterPointing && !lesson.CinematicBlocking, "first poster is an interactive observation checkpoint");
            Check(Target(ElevatorClue.Friend).CanActivate && Target(ElevatorClue.Notice).CanActivate, "friend and poster are both observable");
            await ClickObject(ElevatorClue.Notice);
            Check(lesson.Flow.Step==ElevatorStep.PosterPointing&&lesson.Flow.HasObjectPreview&&string.IsNullOrEmpty(lesson.Flow.FirstJudgment),"poster click previews without automatically warning the friend");
            Face(lesson.view.dialoguePanel.position); await Capture("00_PosterActionPreview");
            await Click(1);
            Check(!lesson.Flow.HasObjectPreview&&lesson.Flow.AwaitingPoster,"preview can close without advancing or recording a decision");
            await ClickObject(ElevatorClue.Notice); await Click(0);
            Check(lesson.Flow.Step == ElevatorStep.PosterWarning, "contextual poster action explicitly warns the friend");
            lesson.Advance(lesson.settings.posterWarningSeconds + .01f);
            lesson.Advance(lesson.settings.friendReplySeconds + lesson.settings.jumpSeconds + lesson.settings.afterJumpSeconds + .01f);
            lesson.Advance(2);
            await Settle();
            Face(lesson.friend.position + Vector3.up * 1.1f); await Frames();
            await Capture("01_FaultObservation");
            Activate(ElevatorClue.Door); Activate(ElevatorClue.FloorButtons);
            Check(lesson.Flow.Step==ElevatorStep.Stopped&&lesson.Flow.PreviewClue==ElevatorClue.FloorButtons&&!lesson.Flow.HelpRequested&&!lesson.Flow.CanConfirmObject,"multiple fault objects can be inspected without progressing or punishment");
            await Click(0);
            lesson.Advance(20);
            Check(lesson.Flow.HintLevel == 1 && !lesson.scenario.GuidanceActive, "first assistance is descriptive, not a failing timer");
            lesson.Advance(20);
            Check(lesson.Flow.HintLevel == 2, "second assistance tier is available");
            lesson.scenario.AdvanceGuidance(40);
            Check(lesson.scenario.GuidanceActive && lesson.scenario.GuidanceTarget==ElevatorClue.Intercom,"second tier highlights the required object");
            await ClickObject(ElevatorClue.Intercom);
            Check(lesson.Flow.Step==ElevatorStep.Stopped&&lesson.Flow.CanConfirmObject,"intercom preview waits for the help-request button");
            Face(lesson.view.dialoguePanel.position); await Capture("01b_IntercomActionPreview");
            await Click(0);
            Check(lesson.Flow.Step == ElevatorStep.Connecting, "help-request action initiates connection");
            lesson.Advance(lesson.settings.connectionSeconds + .01f);
            lesson.Advance(lesson.settings.callQuestionSeconds + .01f);
            lesson.Advance(lesson.settings.callReplySeconds + .01f);
            lesson.Advance(lesson.settings.callQuestionSeconds + .01f);
            lesson.Advance(lesson.settings.callLocationGuideSeconds + .01f);
            await Settle();
            await ClickObject(ElevatorClue.Identity);
            Check(lesson.Flow.ReviewingIdentity && !lesson.Flow.LocationReported, "actual identity object opens confirmation, not automatic reporting");
            Face(lesson.view.dialoguePanel.position); await Frames();
            Check(lesson.view.dialogueText.fontSize >= 34, "location information has enlarged Korean type");
            lesson.view.dialogueText.ForceMeshUpdate();
            Check(!lesson.view.dialogueText.isTextOverflowing,"all location information is visible without memorization or pagination");
            await Capture("02_IdentityConfirmation");
            await Click(1);
            Check(lesson.Flow.SearchingIdentity && !lesson.Flow.LocationReported, "actual close button returns to observation");
            await ClickObject(ElevatorClue.Identity);
            await Click(0);
            Check(lesson.Flow.LocationReported && lesson.Flow.Step == ElevatorStep.CallLocationResponse, "actual confirm button reports the checked information");
            lesson.Advance(lesson.settings.locationReportSeconds + .01f);
            lesson.Advance(lesson.settings.safetyAdviceSeconds + .01f);
            ElevatorDoorSafetyCutscene.Describe(lesson,out _,out string motive);
            Check(motive.Contains("후들")&&!motive.Contains("아파"),"friend motivation is tension after the sudden stop, not leg pain");
            lesson.Advance(lesson.settings.doorLeanSeconds + .01f);
            await Settle();
            Face(lesson.friend.position + Vector3.up * 1.05f); await Frames();
            await Capture("03_DoorObservation");
            await ClickObject(ElevatorClue.Door);
            await Settle();
            Check(lesson.Flow.Step==ElevatorStep.DoorPosterPointing&&!lesson.Flow.DoorRetry&&lesson.Flow.CanConfirmObject,"door inspection alone never records an unsafe suggestion");
            var staleDoorAction=lesson.view.ChoiceButtons[0].onClick;
            var doorStyle=(Image)lesson.view.ChoiceButtons[0].targetGraphic;
            var doorSprite=doorStyle.sprite; var doorColor=doorStyle.color;
            await ClickObject(ElevatorClue.Notice);
            staleDoorAction.Invoke(); await Frames();
            Check(lesson.Flow.PreviewClue==ElevatorClue.Notice&&!lesson.Flow.DoorRetry&&!lesson.Flow.DoorWarningGiven,"switching objects invalidates the old action button");
            Check(lesson.view.ChoiceButtons.Count == 2, "poster shows one contextual action and one close button, not another choice screen");
            var buttons = lesson.view.ChoiceButtons;
            var image0 = buttons[0].targetGraphic as Image;
            Check(image0&&image0.sprite==doorSprite&&image0.color==doorColor,"safe and unsafe object actions use identical visual styling");
            foreach(var button in buttons) button.GetComponentInChildren<TMP_Text>().ForceMeshUpdate();
            Check(buttons.All(b=>!b.GetComponentInChildren<TMP_Text>().isTextOverflowing),"all judgment action labels fit their buttons");
            Face(lesson.view.dialoguePanel.position); await Frames();
            await Capture("04_PosterActionPreview");
            var stalePosterAction=buttons[0].onClick;
            await ClickObject(ElevatorClue.Friend);
            stalePosterAction.Invoke(); await Frames();
            Check(lesson.Flow.PreviewClue==ElevatorClue.Friend&&!lesson.Flow.CanConfirmObject&&!lesson.Flow.DoorWarningGiven,"friend can be inspected while a preview is open without committing the previous action");
            await ClickObject(ElevatorClue.Door);
            Face(lesson.view.dialoguePanel.position); await Capture("04b_DoorActionPreview");
            lesson.pauseMenu.Pause();
            float elapsed = lesson.Flow.Elapsed;
            lesson.Advance(20); lesson.ConfirmObject(); await Frames();
            Check(lesson.Flow.Elapsed == elapsed && !Target(ElevatorClue.Notice).CanActivate&&!lesson.Flow.DoorRetry, "pause blocks story time, object activation and confirmation");
            lesson.pauseMenu.Resume(); await Settle();
            await Click(0);
            Check(lesson.Flow.DoorRetry && !lesson.Flow.DoorWarningGiven, "actual unsafe action opens a local, non-injury result");
            Face(lesson.view.dialoguePanel.position); await Frames();
            await Capture("05_UnsafeReason");
            await Click(0);
            Check(lesson.Flow.Step == ElevatorStep.DoorPosterPointing && lesson.Flow.Phase == InteractionPhase.Investigation && !lesson.Flow.HasObjectPreview && lesson.Flow.LocationReported && lesson.Flow.JumpCount == 1, "actual retry returns only to this judgment with no pending action");
            await ClickObject(ElevatorClue.Notice); await Click(1);
            Check(!lesson.Flow.HasObjectPreview&&Target(ElevatorClue.Door).CanActivate,"close button restores observation and leaves other objects selectable");
            await ClickObject(ElevatorClue.Notice); await Click(0);
            Check(lesson.Flow.DoorWarningGiven, "actual safe action resumes authored friend warning");
            lesson.Advance(lesson.settings.doorWarningSeconds + .01f);
            lesson.Advance(lesson.settings.safeWallMoveSeconds + .01f);
            Check(lesson.Flow.SafeWallReached && lesson.Flow.Reassured, "existing friend movement reaches the safe wall");
            lesson.Advance(lesson.settings.safeWallSettleSeconds + .01f); lesson.Advance(4.81f);
            lesson.Advance(lesson.settings.rescueSubtitleSeconds + .01f);
            lesson.Continue(); AdvanceFor(lesson.settings.exitSeconds + .01f);
            lesson.Continue(); AdvanceFor(3.51f); lesson.Continue(); AdvanceFor(6.01f);
            await Settle();
            Check(lesson.Flow.Step == ElevatorStep.Complete && lesson.view.spatial.completionRows.Length == 4, "original rescue ends with four-action notebook");
            Check(lesson.view.spatial.completionMarks.All(t => t.text == "완료"), "notebook reflects actual completed actions, not a score");
            Check(lesson.view.spatial.completionRows.All(r=>!r.Find("StepLabel").GetComponent<TMP_Text>().isTextOverflowing),"four notebook action labels remain readable");
            Face(lesson.view.spatial.notebook.position); await Frames();
            await Capture("06_ActionNotebook");
            var texts=lesson.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true));
            Check(texts.All(t=>IsolatedFont(t.font,new HashSet<TMP_FontAsset>())),"all runtime text and fallback fonts remain isolated from originals");
            lesson.Restart(); await Settle();
            Check(!lesson.Flow.IdentityReviewOpen && !lesson.Flow.DoorPosterRead && !lesson.Flow.PreviewClue.HasValue && lesson.Flow.Step == ElevatorStep.Lobby, "restart closes all trial UI and clears state");
            var report = new { status = "PASS", count = checks.Count, checks, coverage = "Play Mode; actual 3D observation activation and desktop centre-ray button events. Not injected physical mouse input or HMD hardware." };
            Save("RuntimeValidation", report);
            return report;
        }
        catch (Exception e)
        {
            Save("RuntimeValidation", new { status = "FAIL", error = e.ToString(), checks, step = lesson.Flow.Step.ToString(), phase = lesson.Flow.Phase.ToString(), buttons = lesson.view.ChoiceButtons.Select(b => new { b.name, text = b.GetComponentInChildren<TMP_Text>().text, active = b.IsActive(), b.interactable, position = new[] { b.transform.position.x, b.transform.position.y, b.transform.position.z } }).ToArray(), hovered = controls.HoverTargetName });
            throw;
        }
        finally { lesson.enabled = true; }
    }
}
