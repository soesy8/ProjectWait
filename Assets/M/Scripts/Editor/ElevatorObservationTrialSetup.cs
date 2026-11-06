using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectWait.M.Elevator;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Trial = ProjectWait.M.Elevator.ObservationTrial;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    /// <summary>Only isolated trial assets are saved. No prefab Apply or global SaveAssets.</summary>
    public static class ElevatorObservationTrialSetup
    {
        public const string ScenePath = "Assets/M/Scenes/Elevator_Observation_Choice_Test.unity";
        public const string Evidence = "Assets/M/Documentation/ObservationTrial_Dialogue_20261106/";
        const string SettingsPath = "Assets/M/Scenes/Elevator_Observation_Settings.asset";
        const string FontRoot = "Assets/M/Arts/Fonts/ObservationTrial/";

        static void Write(string name, object value)
        {
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + name + ".json", JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        [MenuItem("Tools/Wait/Elevator/Observation trial/Open isolated test scene")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("An open scene has unsaved user changes. Save it before opening the trial.");
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static object PrepareAndSave()
        {
            Open();
            IsolateTextAssets();
            // Reload after GUID remapping so no scene object keeps a shared dynamic font.
            Open();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var lesson = Object.FindFirstObjectByType<Trial.ElevatorLesson>();
            if (!lesson) throw new Exception("The isolated lesson is missing; do not modify the original scene.");
            var settings = AssetDatabase.LoadAssetAtPath<ElevatorSettings>(SettingsPath);
            if (!settings)
            {
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(lesson.settings), SettingsPath))
                    throw new Exception("Could not make independent trial settings.");
                settings = AssetDatabase.LoadAssetAtPath<ElevatorSettings>(SettingsPath);
            }
            lesson.settings = settings;
            settings.firstHintSeconds = 20;
            settings.secondHintSeconds = 40;
            settings.shotStorySeconds = 1.2f;
            settings.friendReplySeconds = 1.2f;
            settings.callQuestionSeconds = 3;
            settings.callReplySeconds = 4.5f;
            settings.locationReportSeconds = 4;
            settings.doorLeanSeconds = 5;
            settings.doorWarningSeconds = 4;
            settings.rescueSubtitleSeconds = 6;
            settings.lobbyDialogue = Trial.ElevatorDialogue.Lobby;
            settings.handoffDialogue = Trial.ElevatorDialogue.Handoff;
            settings.proposal = Trial.ElevatorDialogue.ShotGesture;
            settings.stoppedDialogue = Trial.ElevatorDialogue.Stopped;
            settings.reportDialogue = Trial.ElevatorDialogue.SituationReport;
            settings.waitDialogue = Trial.ElevatorDialogue.Reassure;
            settings.doorWarning = Trial.ElevatorDialogue.RetryDoor;
            settings.safetyNotice = Trial.ElevatorDialogue.SafetyNotice;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            lesson.scenario.objectGuidedScene = true;
            lesson.scenario.guidanceDelay = settings.secondHintSeconds;
            var gaze = lesson.posterCutscene;
            if (gaze)
            {
                gaze.enabled = false;
                if (gaze.screenCanvas) gaze.screenCanvas.gameObject.SetActive(false);
                if (gaze.worldCanvas) gaze.worldCanvas.gameObject.SetActive(false);
                foreach (var group in new[] { gaze.arrowGroup, gaze.ringGroup, gaze.instructionGroup })
                    if (group) { group.alpha = 0; group.interactable = group.blocksRaycasts = false; }
                EditorUtility.SetDirty(gaze);
            }

            var ui = lesson.view.spatial;
            ui.readingDistance = 1.4f;
            if (!ui.focusWindow) ui.focusWindow = ui.GetComponent<Trial.ElevatorUIFocusWindow>();
            if (!ui.focusWindow) ui.focusWindow = ui.gameObject.AddComponent<Trial.ElevatorUIFocusWindow>();
            ui.focusWindow.owner = ui;
            ui.focusWindow.radiusScale = 2;
            ui.focusWindow.windowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/ElevatorUIFocusWindow.shader");
            if (!ui.focusWindow.windowShader) throw new Exception("The isolated focus-window shader is missing.");
            EditorUtility.SetDirty(ui.focusWindow);
            if (ui.completionRows.Length == 3)
            {
                var row = Object.Instantiate(ui.completionRows[2], ui.completionRows[2].parent, false);
                row.name = "CompletedStep3";
                ui.completionRows = ui.completionRows.Concat(new[] { row }).ToArray();
                ui.completionMarks = ui.completionMarks.Concat(new[] { row.Find("DoneBadge/Done").GetComponent<TMP_Text>() }).ToArray();
            }
            string[] labels = { "도움 요청", "위치 전달", "안전 대기", "안내 따르기" };
            for (int i = 0; i < ui.completionRows.Length; i++)
            {
                var row = ui.completionRows[i];
                row.sizeDelta = new Vector2(430, 75);
                row.anchoredPosition = new Vector2(0, 150 - i * 85);
                var label = row.Find("StepLabel").GetComponent<TMP_Text>();
                label.text = labels[i];
                label.fontSize = 24;
            }
            var footer = ui.leftPage.Find("NotebookFooter").GetComponent<TMP_Text>();
            footer.text = "도움을 요청하고,\n안전한 곳에서 기다려요.";
            footer.rectTransform.anchoredPosition = new Vector2(0, -220);
            footer.rectTransform.sizeDelta = new Vector2(416, 64);
            foreach (var component in new Object[] { lesson, lesson.scenario, ui }) EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Test scene could not be saved.");
            return ValidateScene();
        }

        static void IsolateTextAssets()
        {
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            var fonts = dependencies.Where(p => !p.StartsWith(FontRoot, StringComparison.Ordinal) &&
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p)).ToArray();
            if (fonts.Length == 0) return;
            var fontPaths = new HashSet<string>(fonts);
            var materials = dependencies.Where(p =>
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(p);
                return material && material.HasProperty("_MainTex") && material.mainTexture &&
                    fontPaths.Contains(AssetDatabase.GetAssetPath(material.mainTexture));
            }).ToArray();
            // Runtime-instantiated thought bubbles and signage must not carry the old
            // fallback even for the frame between Instantiate and font assignment.
            var prefabs = dependencies.Where(p => p.EndsWith(".prefab", StringComparison.Ordinal) &&
                AssetDatabase.GetDependencies(p, true).Any(fontPaths.Contains)).ToArray();
            var copies = new Dictionary<string, string>();
            foreach (string source in fonts.Concat(materials).Concat(prefabs).Distinct())
            {
                string root = fontPaths.Contains(source) ? FontRoot : source.EndsWith(".mat", StringComparison.Ordinal)
                    ? "Assets/M/Arts/Materials/ObservationTrialText/" : "Assets/M/Prefabs/ObservationTrialText/";
                Directory.CreateDirectory(root);
                string destination = root + Path.GetFileNameWithoutExtension(source) + "_" +
                    AssetDatabase.AssetPathToGUID(source).Substring(0, 8) + Path.GetExtension(source);
                if (!File.Exists(destination) && !AssetDatabase.CopyAsset(source, destination))
                    throw new IOException("Could not isolate text asset: " + source);
                copies.Add(source, destination);
            }
            var guids = copies.ToDictionary(p => AssetDatabase.AssetPathToGUID(p.Key), p => AssetDatabase.AssetPathToGUID(p.Value));
            foreach (string path in copies.Values.Concat(new[] { ScenePath }))
            {
                string before = File.ReadAllText(path), after = before;
                foreach (var pair in guids) after = after.Replace(pair.Key, pair.Value);
                if (before == after) continue;
                File.WriteAllText(path, after, new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            Write("FontIsolation", new { fonts, copies, globalFallbackSettingsUnchanged = true });
        }

        public static object ValidateScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Validation is restricted to the test scene.");
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            int missingScripts = objects.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var missingReferences = new List<string>();
            foreach (var component in objects.SelectMany(t => t.GetComponents<Component>()).Where(c => c))
            {
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        missingReferences.Add(component.name + ":" + property.propertyPath);
            }
            var lesson = Object.FindFirstObjectByType<Trial.ElevatorLesson>();
            var errors = objects.SelectMany(t => t.GetComponents<Renderer>()).SelectMany(r => r.sharedMaterials)
                .Where(m => m && (!m.shader || ShaderUtil.ShaderHasError(m.shader))).Select(m => m.name).Distinct().ToArray();
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            var sharedFonts = dependencies.Where(p => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p) &&
                !p.StartsWith(FontRoot, StringComparison.Ordinal)).ToArray();
            var report = new
            {
                scene = scene.path,
                missingScripts,
                missingReferences,
                shaderErrors = errors,
                isolatedLessons = objects.Sum(t => t.GetComponents<Trial.ElevatorLesson>().Length),
                originalLessons = objects.Sum(t => t.GetComponents<ProjectWait.M.Elevator.ElevatorLesson>().Length),
                observations = lesson.CollectObservations().Length,
                notebookRows = lesson.view.spatial.completionRows.Length,
                temporaryDependencies = dependencies.Where(p => p.StartsWith("Assets/M/CodexTemp/", StringComparison.Ordinal)).ToArray(),
                independentSettings = AssetDatabase.GetAssetPath(lesson.settings) == SettingsPath,
                sharedFontDependencies = sharedFonts,
                nearReadingUI = HasNearReadingConfiguration(lesson.view.spatial),
                focusWindowScale = lesson.view.spatial.focusWindow ? lesson.view.spatial.focusWindow.radiusScale : 0,
                dialogueSettingsUpdated = lesson.settings.reportDialogue == Trial.ElevatorDialogue.SituationReport &&
                    lesson.settings.handoffDialogue == Trial.ElevatorDialogue.Handoff,
                gazeGuidanceRetired = lesson.posterCutscene && !lesson.posterCutscene.enabled &&
                    !lesson.posterCutscene.screenCanvas.gameObject.activeSelf && !lesson.posterCutscene.worldCanvas.gameObject.activeSelf,
                guidance = new { textSeconds = lesson.settings.firstHintSeconds, highlightSeconds = lesson.scenario.guidanceDelay },
                readableTiming = lesson.settings.callQuestionSeconds >= 3 && lesson.settings.callReplySeconds >= 4 &&
                    lesson.settings.doorLeanSeconds >= 5 && lesson.settings.rescueSubtitleSeconds >= 6
            };
            Write("SceneValidation", report);
            if (missingScripts != 0 || missingReferences.Count != 0 || errors.Length != 0 || report.originalLessons != 0 ||
                report.isolatedLessons != 1 || report.notebookRows != 4 || report.temporaryDependencies.Length != 0 ||
                !report.independentSettings || sharedFonts.Length != 0 || !report.readableTiming || !report.gazeGuidanceRetired || !report.nearReadingUI ||
                report.focusWindowScale != 2 || !report.dialogueSettingsUpdated)
                throw new Exception("Trial scene validation failed. See " + Evidence + "SceneValidation.json");
            return report;
        }

        static bool HasNearReadingConfiguration(Trial.ElevatorSpatialUI ui)=>ui.readingDistance>=1.2f&&ui.readingDistance<=1.6f&&
            ui.focusWindow&&ui.focusWindow.windowShader&&!ShaderUtil.ShaderHasError(ui.focusWindow.windowShader);

        [MenuItem("Tools/Wait/Elevator/Observation trial/Validate isolated flow")]
        public static void ValidateFlowMenu() { ValidateFlow(); }

        public static object ValidateFlow()
        {
            var checks = new List<string>();
            Action<bool, string> check = (value, label) => { if (!value) throw new Exception(label); checks.Add(label); };
            var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ElevatorSettings>(SettingsPath));
            try
            {
                foreach (bool phone in new[] { false, true })
                {
                    string route = phone ? "phone: " : "intercom: ";
                    var f = new Trial.ElevatorFlow(settings) { NoResponse = phone };
                    f.ReportLocation(); f.FindIdentity(); f.ChooseDoorAction(true); f.PreviewObject(Trial.ElevatorClue.Friend);
                    check(f.Step == Trial.ElevatorStep.Lobby, route + "out-of-order commands cannot skip introduction");
                    f.Continue(); f.Tick(settings.boardingSeconds + .01f);
                    f.Continue(); check(f.Step == Trial.ElevatorStep.Boarding, route + "destination is required");
                    f.PreviewObject(Trial.ElevatorClue.FloorButtons);
                    check(!f.HasObjectPreview, route + "boarding controls never require confirmation");
                    f.SelectDestination(); f.Continue(); f.Tick(settings.shotStorySeconds + .01f);
                    check(f.Step == Trial.ElevatorStep.Jump && f.JumpCount == 1 && string.IsNullOrEmpty(f.FirstJudgment), route + "short introduction has no mandatory poster task or ignored player decision");
                    f.Tick(settings.friendReplySeconds + settings.jumpSeconds + settings.afterJumpSeconds + .01f); f.Tick(2);
                    check(f.Step == Trial.ElevatorStep.Stopped && f.CanObserve && !f.IsCinematic, route + "fault ends in object observation");
                    int revision = f.Revision;
                    f.ChooseDoorAction(false); f.FindIdentity(); f.ReportLocation();
                    check(f.Revision == revision, route + "irrelevant choices do not advance fault observation");
                    float elapsed = f.Elapsed; f.Tick(float.NaN); f.Tick(float.PositiveInfinity); f.Tick(-1);
                    check(f.Elapsed == elapsed, route + "invalid deltas are ignored");
                    f.PreviewObject(Trial.ElevatorClue.Door); f.ConfirmObject(); f.CloseObjectPreview();
                    check(f.Step == Trial.ElevatorStep.Stopped && !f.HelpRequested, route + "looking at a door is never treated as forcing it");
                    f.RequestIntercom();
                    check(f.Step == Trial.ElevatorStep.Connecting && !f.HasObjectPreview, route + "physical emergency button starts connection immediately");
                    f.Tick(settings.connectionSeconds + .01f);
                    if (phone)
                    {
                        check(f.Step == Trial.ElevatorStep.Unanswered, route + "unanswered connection offers fallback");
                        f.RequestPhone();
                        check(f.Step == Trial.ElevatorStep.CallQuestion, route + "phone starts the simulated call directly");
                    }
                    f.Tick(settings.callQuestionSeconds + .01f); f.Tick(settings.callReplySeconds + .01f);
                    f.Tick(settings.callQuestionSeconds + .01f); f.Tick(120);
                    check(f.Step == Trial.ElevatorStep.CallIdentityGuide && f.AwaitingCallContinue && !f.IsCinematic, route + "essential location directions wait without a reading timeout");
                    f.Continue();
                    check(f.SearchingIdentity && !f.LocationReported, route + "location remains unknown before direct inspection");
                    f.ReportLocation(); check(f.SearchingIdentity, route + "cannot report unseen location");
                    f.FindIdentity();
                    check(f.ReviewingIdentity && f.IdentityFound && !f.LocationReported && !f.IsCinematic, route + "finding opens information without reporting");
                    f.Tick(120); check(f.ReviewingIdentity && !f.LocationReported, route + "information waits for explicit confirmation");
                    f.ReinspectIdentity(); check(f.SearchingIdentity && !f.LocationReported, route + "information can be closed and inspected again");
                    f.FindIdentity(); f.ReportLocation();
                    check(f.LocationReported && f.Step == Trial.ElevatorStep.CallLocationResponse, route + "explicit report resumes existing dialogue");
                    revision = f.Revision; f.ReportLocation(); f.FindIdentity();
                    check(f.Revision == revision, route + "duplicate report cannot skip dialogue");
                    f.Tick(settings.locationReportSeconds + .01f); f.Tick(120);
                    check(f.Step == Trial.ElevatorStep.CallSafetyAdvice && f.AwaitingCallContinue, route + "safety guidance remains until explicitly read");
                    f.Continue(); f.Tick(settings.doorLeanSeconds + .01f);
                    check(f.Step == Trial.ElevatorStep.DoorPosterPointing && f.Phase == Trial.InteractionPhase.Investigation && !f.IsCinematic, route + "door movement ends in a clue observation");
                    f.Tick(120); f.ChooseDoorAction(true); f.ConfirmObject();
                    check(!f.DoorWarningGiven && !f.SafeWallReached, route + "speaking requires opening the friend interaction, not clicking empty space");
                    int history = f.History.Count;
                    f.PreviewObject(Trial.ElevatorClue.Door);
                    f.ChooseDoorAction(false);
                    check(!f.CanConfirmObject && !f.DoorRetry && f.History.Count == history, route + "door inspection offers no invented unsafe reply");
                    if (phone)
                    {
                        f.PointAtSafetyPoster(); f.ConfirmObject();
                        check(f.DoorPosterRead && !f.CanConfirmObject && !f.DoorWarningGiven, route + "optional poster informs but cannot speak for the player");
                    }
                    f.PreviewObject(Trial.ElevatorClue.Friend);
                    check(f.CanConfirmObject && !f.DoorWarningGiven && !f.DoorRetry, route + "friend click opens replies without choosing one");
                    f.ChooseDoorAction(false);
                    check(f.DoorRetry && !f.DoorWarningGiven && !f.SafeWallReached, route + "unsafe suggestion pauses before an accident");
                    f.Tick(120); check(f.DoorRetry, route + "reason remains readable without a timeout");
                    f.Retry();
                    check(f.Step == Trial.ElevatorStep.DoorPosterPointing && f.Phase == Trial.InteractionPhase.Investigation && !f.HasObjectPreview && f.LocationReported && f.JumpCount == 1, route + "retry preserves completed actions and reopens only this observation");
                    f.PreviewObject(Trial.ElevatorClue.Friend); f.ReobserveDoor();
                    check(!f.HasObjectPreview && !f.CanConfirmObject, route + "judgment can return to observation without a third step");
                    f.PreviewObject(Trial.ElevatorClue.Friend); f.ChooseDoorAction(true);
                    check(f.DoorWarningGiven && f.Step == Trial.ElevatorStep.DoorPosterWarning && f.DoorPosterRead == phone, route + "safe reply works with or without reading the poster");
                    f.Tick(settings.doorWarningSeconds + .01f); f.Tick(settings.safeWallMoveSeconds + .01f);
                    check(f.SafeWallReached && f.Reassured, route + "friend moves to safe wall");
                    f.Tick(settings.safeWallSettleSeconds + .01f); f.Tick(4.81f); f.Tick(settings.rescueSubtitleSeconds + .01f);
                    f.Continue(); f.Tick(settings.exitSeconds + .01f); f.Continue(); f.Tick(3.51f); f.Continue(); f.Tick(6.01f);
                    check(f.Step == Trial.ElevatorStep.Complete && f.HelpRequested && f.LocationReported && f.SafeWallReached && f.ReadyToExit, route + "four real actions complete the rescue notebook");
                    f.Reset();
                    check(f.Step == Trial.ElevatorStep.Lobby && !f.IdentityFound && !f.IdentityReviewOpen && !f.LocationReported && !f.DoorPosterRead && !f.SafeWallReached && !f.PreviewClue.HasValue && f.History.Count == 0, route + "restart clears every trial flag");
                    f.DeveloperJump(5);
                    check(f.Step == Trial.ElevatorStep.DoorPosterPointing && !f.DoorPosterRead, route + "developer shortcut opens the actual friend decision");
                    f.PreviewObject(Trial.ElevatorClue.Friend); f.DeveloperJump(2);
                    check(!f.PreviewClue.HasValue && !f.HasObjectPreview, route + "developer stage changes cannot retain pending actions");
                }
                var report = new { status = "PASS", count = checks.Count, checks };
                Write("FlowValidation", report);
                return report;
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
