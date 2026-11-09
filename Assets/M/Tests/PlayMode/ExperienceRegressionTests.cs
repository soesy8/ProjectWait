#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using R = ProjectWait.M.Tests.RuntimeAccess;

namespace ProjectWait.M.Tests.PlayMode
{
    public sealed class CleanSceneGate : IPrebuildSetup
    {
        public void Setup() => R.RequireCleanEditorScenes();
    }

    [TestFixture, Category("WaitRegression"), PrebuildSetup(typeof(CleanSceneGate))]
    public sealed class ExperienceRegressionTests
    {
        readonly List<GameObject> suspendedRoots = new List<GameObject>();
        GlobalState global;
        Scene originalActive, loaded;
        readonly HashSet<int> originalSceneHandles = new HashSet<int>();
        Behaviour lesson;
        bool lessonEnabled;
        object settings, view, flow;

        [UnitySetUp]
        public IEnumerator LoadIsolatedRealScene()
        {
            global = new GlobalState();
            loaded = default; lesson = null; lessonEnabled = false;
            originalSceneHandles.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++) originalSceneHandles.Add(SceneManager.GetSceneAt(i).handle);
            originalActive = SceneManager.GetActiveScene();
            // Keep the user's runtime roots alive, but prevent duplicate lesson/input/
            // render owners during additive loading. Never save or replace that scene.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path != R.ScenePath) continue; // UTF InitTestScene also has a path.
                foreach (var root in scene.GetRootGameObjects())
                    if (root.activeSelf) { suspendedRoots.Add(root); root.SetActive(false); }
            }
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(R.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            Assert.That(operation, Is.Not.Null);
            yield return operation;
            // The asset can already be loaded once; select our new instance by handle.
            loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Single(s => s.path == R.ScenePath && !originalSceneHandles.Contains(s.handle));
            Assert.That(loaded.path, Is.EqualTo(R.ScenePath));
            SceneManager.SetActiveScene(loaded);
            lesson = loaded.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(R.Type(R.Trial + "ElevatorLesson"), true))
                .Cast<Behaviour>().Single();
            yield return null; // actual Awake/Start, not an uninitialized edit fixture
            lessonEnabled = lesson.enabled;
            lesson.enabled = false; // deterministic manual simulation
            settings = R.Get(lesson, "settings"); view = R.Get(lesson, "view"); flow = R.Get(lesson, "Flow");
            Assert.That(flow, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator RestoreRuntimeSceneAndGlobals()
        {
            try
            {
                if (lesson) lesson.enabled = lessonEnabled;
                // Scene unload runs pause/input/render OnDisable cleanup. Find the new
                // scene even when setup failed before assigning `loaded`.
                if (!loaded.IsValid())
                    loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                        .FirstOrDefault(s => s.path == R.ScenePath && !originalSceneHandles.Contains(s.handle));
                if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            }
            finally
            {
                try
                {
                    foreach (var root in suspendedRoots) if (root) root.SetActive(true);
                    if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                }
                finally
                {
                    suspendedRoots.Clear();
                    global?.Dispose(); global = null;
                    lesson = null; settings = view = flow = null;
                }
            }
        }

        [UnityTest]
        public IEnumerator RealObjectsAndButtons_IntercomRouteCompletes()
        {
            CompleteRoute(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealObjectsAndButtons_UnansweredPhoneRouteCompletes()
        {
            CompleteRoute(true);
            yield return null;
        }

        void CompleteRoute(bool phone)
        {
            R.Set(lesson, "simulateNoResponse", phone);
            R.Call(lesson, "DeveloperJump", 0); Settle();
            Activate("LobbyCall"); Tick("boardingSeconds"); Settle();
            Activate("FloorButtons"); Activate("Ball");
            Tick("shotStorySeconds");
            R.Call(lesson, "Advance", R.Get<float>(settings, "friendReplySeconds") +
                R.Get<float>(settings, "jumpSeconds") + R.Seconds(settings, "afterJumpSeconds"));
            R.Call(lesson, "Advance", 2f); Settle();
            Activate("Intercom"); Tick("connectionSeconds"); Settle();
            if (phone) Activate("Phone");
            FinishCallQuestions(); Choice();
            Assert.That(R.Get<bool>(flow, "SearchingIdentity"), Is.True);
            Activate("Identity"); Choice();
            Assert.That(R.Get<bool>(flow, "LocationReported"), Is.True);
            Assert.That(R.Get<bool>(flow, "UsedPhone"), Is.EqualTo(phone));
            Tick("locationReportSeconds"); Choice(); Tick("doorLeanSeconds"); Settle();
            Activate("Notice"); Activate("Friend"); Choice();
            Tick("doorWarningSeconds"); Tick("safeWallMoveSeconds"); Tick("safeWallSettleSeconds");
            R.Call(lesson, "Advance", 4.81f); Tick("rescueSubtitleSeconds"); Settle();
            Activate("Rescuer"); Tick("exitSeconds"); Settle(); Activate("Rescuer");
            for (int i = 0; i < 40; i++) R.Call(lesson, "Advance", .1f);
            Settle();
            Assert.That(R.Get<int>(lesson, "Floor"), Is.EqualTo(1));
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("GroundFloor"));
            Activate("GroundExit"); R.Call(lesson, "Advance", 6.01f); Settle();
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("Complete"));
            Assert.That(R.Get<bool>(flow, "SafeWallReached"), Is.True);
        }

        [UnityTest]
        public IEnumerator IdentityDescriptionsAndContinuousHover_SurvivePresentationAndTimedHints()
        {
            R.Call(lesson, "DeveloperJump", 4); FinishCallQuestions(); Choice();
            Assert.That(R.Get<bool>(flow, "SearchingIdentity"), Is.True);
            foreach (var clue in new[] { "Notice", "FacilityGuide", "Intercom" })
            {
                Activate(clue);
                Assert.That(R.Get<bool>(flow, "HasObjectPreview"), Is.True);
                var expected = (string)R.Call(R.Type(R.Trial + "ElevatorDialogue"), "DescribeObject", lesson, R.Clue(clue));
                Assert.That(Compact(R.Get<string>(R.Get(view, "dialogueText"), "text")), Is.EqualTo(Compact(expected)), clue);
                Choice();
                Assert.That(R.Get<bool>(flow, "HasObjectPreview"), Is.False, "Actual close button must work");
            }
            var bell = Observation("Intercom");
            try
            {
                R.Call(bell, "Hover", true); LatePresentation();
                var expectedHint = R.Get<string>(R.Type(R.Trial + "ElevatorDialogue"), "IdentityPanelHint");
                Assert.That(R.Get<string>(R.Get(view, "thought"), "text"), Is.EqualTo(expectedHint));
                foreach (var field in new[] { "firstHintSeconds", "secondHintSeconds" })
                {
                    Tick(field); LatePresentation();
                    Assert.That(R.Get<bool>(bell, "IsHovered"), Is.True);
                    Assert.That(R.Get<string>(R.Get(view, "thought"), "text"), Is.EqualTo(expectedHint));
                }
                Assert.That(R.Get<bool>(flow, "IdentityFound"), Is.False);
                Assert.That(R.Get<bool>(flow, "LocationReported"), Is.False);
            }
            finally { R.Call(bell, "Hover", false); LatePresentation(); }
            Activate("Identity");
            Assert.That(R.Get<bool>(flow, "ReviewingIdentity"), Is.True);
            Assert.That(R.Get<bool>(flow, "LocationReported"), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HiddenHudStatus_StaysHiddenWhenDoorCutsceneStarts()
        {
            R.Call(lesson, "DeveloperJump", 4); FinishCallQuestions(); Choice();
            Activate("Identity"); Choice(); Tick("locationReportSeconds"); Settle();
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("CallSafetyAdvice"));
            var spatial = R.Get(view, "spatial");
            var mission = (RectTransform)R.Get(spatial, "mission");
            var hud = (Canvas)R.Get(spatial, "hud");
            var camera = (Camera)R.Get(view, "playerCamera");
            camera.transform.LookAt(((Component)view).transform.position);
            Canvas.ForceUpdateCanvases(); R.Call(spatial, "Tick");
            R.Call(spatial, "AdvanceHudVisibility", 1f); Settle();
            Assert.That(mission.gameObject.activeSelf, Is.False, "On-screen dialogue must hide the fallback status before the cutscene");
            Assert.That(hud.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

            Choice(); // Actual safety-advice button starts DoorLean without a run reset.
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("DoorLean"));
            Assert.That(mission.gameObject.activeSelf, Is.False, "A hidden status must not activate at cutscene entry");
            for (int i = 0; i < 12; i++)
            {
                R.Call(view, "Refresh"); R.Call(spatial, "Layout");
                R.Call(spatial, "AdvanceHudVisibility", .05f); R.Call(spatial, "Tick");
                Assert.That(mission.gameObject.activeSelf, Is.False, "Refresh/layout must not resurrect the status during the fade: " + i);
            }
            Assert.That(hud.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Tick("doorLeanSeconds"); Settle();
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("DoorPosterPointing"));
            Assert.That(mission.gameObject.activeSelf, Is.True, "The next interactive stage must regain its status normally");
            R.Call(spatial, "AdvanceHudVisibility", .25f);
            Assert.That(hud.GetComponent<CanvasGroup>().alpha, Is.InRange(.01f, .99f));
            R.Call(spatial, "AdvanceHudVisibility", 1f);
            Assert.That(hud.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisibleHudStatus_FadesExistingContentWithoutReappearingInCutscene()
        {
            R.Call(lesson, "DeveloperJump", 4); FinishCallQuestions(); Choice();
            Activate("Identity"); Choice(); Tick("locationReportSeconds"); Settle();
            var spatial = R.Get(view, "spatial");
            var mission = (RectTransform)R.Get(spatial, "mission");
            var hud = (Canvas)R.Get(spatial, "hud");
            var camera = (Camera)R.Get(view, "playerCamera");
            camera.transform.rotation = Quaternion.LookRotation(camera.transform.position - ((Component)view).transform.position);
            Canvas.ForceUpdateCanvases(); R.Call(spatial, "Tick");
            R.Call(spatial, "AdvanceHudVisibility", 1f); Settle();
            Assert.That(mission.gameObject.activeSelf, Is.True, "Off-screen dialogue must expose the fallback status");
            var title = R.Get(view, "title"); var subtitle = R.Get(view, "subtitle");
            string heading = R.Get<string>(title, "text"), action = R.Get<string>(subtitle, "text");
            Assert.That(heading, Is.Not.Empty); Assert.That(action, Is.Not.Empty);
            Choice();
            Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("DoorLean"));
            Assert.That(mission.gameObject.activeSelf, Is.True, "An already visible status should fade, not pop off");
            float previous = hud.GetComponent<CanvasGroup>().alpha;
            for (int i = 0; i < 6; i++)
            {
                R.Call(view, "Refresh"); R.Call(spatial, "Layout");
                if (previous > 0)
                {
                    Assert.That(R.Get<string>(title, "text"), Is.EqualTo(heading), "Keep the outgoing heading during fade");
                    Assert.That(R.Get<string>(subtitle, "text"), Is.EqualTo(action), "Do not flash a new cinematic instruction");
                }
                else Assert.That(mission.gameObject.activeSelf, Is.False, "After fade, hidden content may change but the panel must remain inactive");
                R.Call(spatial, "AdvanceHudVisibility", .1f);
                float alpha = hud.GetComponent<CanvasGroup>().alpha;
                Assert.That(alpha, Is.LessThanOrEqualTo(previous));
                if (i == 0) Assert.That(alpha, Is.InRange(.01f, .99f));
                previous = alpha;
            }
            Assert.That(previous, Is.Zero);
            R.Call(spatial, "Tick");
            Assert.That(mission.gameObject.activeSelf, Is.False);
            for (int i = 0; i < 3; i++)
            {
                R.Call(view, "Refresh"); R.Call(spatial, "Layout"); R.Call(spatial, "Tick");
                Assert.That(mission.gameObject.activeSelf, Is.False, "A completed fade must not reactivate the panel");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseResumeAndRestartButton_RestoreSimulationAndClearProgress()
        {
            R.Call(lesson, "DeveloperJump", 4); FinishCallQuestions(); Choice(); Activate("Identity"); Choice();
            var pause = R.Get(lesson, "pauseMenu");
            float priorScale = Time.timeScale; bool priorAudio = AudioListener.pause;
            try
            {
                R.Call(pause, "Pause");
                Assert.That(R.Get<bool>(lesson, "IsPaused"), Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(AudioListener.pause, Is.True);
                int revision = R.Get<int>(flow, "Revision"); float elapsed = R.Get<float>(flow, "Elapsed");
                R.Call(lesson, "Advance", 100f);
                Assert.That(R.Get<int>(flow, "Revision"), Is.EqualTo(revision));
                Assert.That(R.Get<float>(flow, "Elapsed"), Is.EqualTo(elapsed));
                ((Button)R.Get(pause, "resumeButton")).onClick.Invoke();
                Assert.That(R.Get<bool>(lesson, "IsPaused"), Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(priorScale));
                Assert.That(AudioListener.pause, Is.EqualTo(priorAudio));
                int run = R.Get<int>(lesson, "RunVersion");
                R.Call(pause, "Pause");
                ((Button)R.Get(pause, "restartButton")).onClick.Invoke();
                Assert.That(R.Get<bool>(lesson, "IsPaused"), Is.False);
                Assert.That(R.Get<int>(lesson, "RunVersion"), Is.GreaterThan(run));
                Assert.That(R.Get<bool>(flow, "IsOpening"), Is.True);
                foreach (var field in new[] { "LocationReported", "IdentityFound", "HelpRequested", "HasObjectPreview" })
                    Assert.That(R.Get<bool>(flow, field), Is.False, field);
                Assert.That(R.Get<int>(lesson, "Floor"), Is.EqualTo(3));
            }
            finally
            {
                if (R.Get<bool>(lesson, "IsPaused")) R.Call(pause, "Resume");
                Time.timeScale = priorScale; AudioListener.pause = priorAudio;
            }
            yield return null;
        }

        [TestCase(true), TestCase(false)]
        public void FrameRateOwners_RealLifecycleReleaseEitherOrderRestoresLiveBaseline(bool olderFirst)
        {
            int fps = Application.targetFrameRate, sync = QualitySettings.vSyncCount;
            GameObject a = null, b = null;
            try
            {
                a = new GameObject("Regression FPS owner A"); a.SetActive(false);
                b = new GameObject("Regression FPS owner B"); b.SetActive(false);
                var first = a.AddComponent(R.Type("ProjectWait.M.FrameRateLimiter"));
                var second = b.AddComponent(R.Type("ProjectWait.M.FrameRateLimiter"));
                R.Set(first, "targetFrameRate", 45); R.Set(second, "targetFrameRate", 60);
                a.SetActive(true); b.SetActive(true);
                (olderFirst ? a : b).SetActive(false);
                Assert.That(Application.targetFrameRate, Is.EqualTo(olderFirst ? 60 : 45));
                Assert.That(QualitySettings.vSyncCount, Is.Zero);
                (olderFirst ? b : a).SetActive(false);
                Assert.That(Application.targetFrameRate, Is.EqualTo(fps));
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(sync));
            }
            finally
            {
                try { if (b) Object.DestroyImmediate(b); if (a) Object.DestroyImmediate(a); }
                finally { Application.targetFrameRate = fps; QualitySettings.vSyncCount = sync; }
            }
        }

        [TestCase(true), TestCase(false)]
        public void PipelineOwners_RealLifecycleReleaseEitherOrderRestoresLiveBaseline(bool olderFirst)
        {
            var baseline = QualitySettings.renderPipeline;
            var source = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/M/Arts/Rendering/ElevatorScenePipeline.asset");
            Assert.That(source, Is.Not.Null);
            GameObject a = null, b = null; RenderPipelineAsset first = null, second = null;
            try
            {
                first = Object.Instantiate(source); second = Object.Instantiate(source);
                a = new GameObject("Regression pipeline owner A"); a.SetActive(false);
                b = new GameObject("Regression pipeline owner B"); b.SetActive(false);
                var x = a.AddComponent(R.Type("ProjectWait.M.Elevator.ElevatorSceneRendering"));
                var y = b.AddComponent(R.Type("ProjectWait.M.Elevator.ElevatorSceneRendering"));
                R.Set(x, "scenePipeline", first); R.Set(y, "scenePipeline", second);
                a.SetActive(true); b.SetActive(true);
                (olderFirst ? a : b).SetActive(false);
                Assert.That(QualitySettings.renderPipeline, Is.SameAs(olderFirst ? second : first));
                (olderFirst ? b : a).SetActive(false);
                Assert.That(QualitySettings.renderPipeline, Is.SameAs(baseline));
            }
            finally
            {
                try { if (b) Object.DestroyImmediate(b); if (a) Object.DestroyImmediate(a); }
                finally
                {
                    QualitySettings.renderPipeline = baseline;
                    if (second) Object.DestroyImmediate(second); if (first) Object.DestroyImmediate(first);
                }
            }
        }

        [UnityTest]
        public IEnumerator StairArrival_LongFrameStillPlacesCapsuleOnGroundFloor()
        {
            R.Call(lesson,"DeveloperJump",6);Tick("rescueSubtitleSeconds");R.Call(lesson,"Continue");
            Tick("exitSeconds");R.Call(lesson,"Continue");R.Call(lesson,"Advance",1.7f);R.Call(lesson,"Advance",1.9f);
            var space=R.Get(R.Get(lesson,"presentation"),"space");var map=R.Get(space,"sportsCentre");
            var rig=(Transform)R.Get(space,"playerRig");var movement=rig.GetComponent(R.Type(R.Trial+"ElevatorPlayerMovement"));
            Assert.That(R.Get(flow,"Step").ToString(),Is.EqualTo("GroundFloor"));
            var destination=R.Get<Vector3>(map,"groundLobby");
            Assert.That(Vector3.Distance(R.Get<Vector3>(movement,"FeetPosition"),destination),Is.LessThan(.02f));
            var before=R.Get<Vector3>(movement,"FeetPosition");R.Call(lesson,"Advance",0f);
            Assert.That(Vector3.Distance(before,R.Get<Vector3>(movement,"FeetPosition")),Is.LessThan(.001f));
            Activate("GroundExit");Assert.That(R.Get(flow,"Step").ToString(),Is.EqualTo("GoingHome"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeightMenu_StableTrackedCalibrationKeepsCapsuleOnFloor()
        {
            var controls=loaded.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(R.Type(R.Trial+"ElevatorDesktopControls"),true)).Single();
            bool desktop=R.Get<bool>(controls,"IsDesktop");var pause=R.Get(lesson,"pauseMenu");
            try
            {
                R.Set(controls,"IsDesktop",false);R.Call(lesson,"DeveloperJump",1);
                var origin=(Transform)R.Get(controls,"xrOrigin");var camera=(Camera)R.Get(controls,"targetCamera");
                camera.transform.position=new Vector3(camera.transform.position.x,origin.position.y+1.65f,camera.transform.position.z);
                R.Call(pause,"Pause");R.Call(pause,"LateUpdate");
                var button=(Button)R.Get(pause,"titleButton");Assert.That(button.IsInteractable(),Is.True);
                button.onClick.Invoke();var before=origin.position;
                Assert.That((bool)R.Call(controls,"ObserveXRHeight",true,(bool?)true,.1f),Is.False);
                Assert.That(origin.position,Is.EqualTo(before));
                bool applied=false;for(int i=0;i<8;i++)applied|=(bool)R.Call(controls,"ObserveXRHeight",true,(bool?)true,.1f);
                Assert.That(applied,Is.True);var space=R.Get(R.Get(lesson,"presentation"),"space");
                float floor=R.Get<float>(R.Get(space,"sportsCentre"),"CurrentFloorHeight");
                Assert.That(camera.transform.position.y,Is.EqualTo(floor+R.Get<float>(controls,"eyeHeight")).Within(.002f));
                Assert.That(R.Get<Vector3>(R.Get(controls,"movement"),"FeetPosition").y,Is.EqualTo(floor).Within(.002f));
                Assert.That(R.Get<bool>(lesson,"IsPaused"),Is.True);
            }
            finally{R.Set(controls,"IsDesktop",desktop);R.Call(pause,"Resume");}
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeightMenu_PausedCinematicStillObservesCalibration()
        {
            var controls=loaded.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren(R.Type(R.Trial+"ElevatorDesktopControls"),true)).Single();
            bool desktop=R.Get<bool>(controls,"IsDesktop");var pause=R.Get(lesson,"pauseMenu");
            try
            {
                R.Set(controls,"IsDesktop",false);R.Call(lesson,"Restart");
                Assert.That(R.Get<bool>(flow,"IsCinematic"),Is.True);
                var origin=(Transform)R.Get(controls,"xrOrigin");var camera=(Camera)R.Get(controls,"targetCamera");
                camera.transform.position=new Vector3(camera.transform.position.x,origin.position.y+1.65f,camera.transform.position.z);
                R.Call(pause,"Pause");R.Call(pause,"LateUpdate");((Button)R.Get(pause,"titleButton")).onClick.Invoke();
                R.Call(controls,"ObserveXRHeight",true,(bool?)true,.1f);R.Call(controls,"ObserveXRHeight",true,(bool?)true,.1f);
                var policy=R.Get(controls,"heightCalibration");Assert.That(R.Get<float>(policy,"StableSeconds"),Is.GreaterThan(0));
                // An invalid physical pose must reset stability in the actual LateUpdate,
                // independent of which real XR device is connected to the test machine.
                camera.transform.position=new Vector3(camera.transform.position.x,origin.position.y+.3f,camera.transform.position.z);
                R.Call(controls,"LateUpdate");
                Assert.That(R.Get<float>(policy,"StableSeconds"),Is.Zero,"Paused cinematic must not skip calibration observations");
                camera.transform.position=new Vector3(camera.transform.position.x,origin.position.y+1.65f,camera.transform.position.z);
                bool applied=false;for(int i=0;i<9;i++)applied|=(bool)R.Call(controls,"ObserveXRHeight",true,(bool?)true,.1f);
                Assert.That(applied,Is.True);Assert.That(R.Get<bool>(lesson,"IsPaused"),Is.True);
                Assert.That(R.Get<bool>(flow,"IsCinematic"),Is.True);
            }
            finally{R.Set(controls,"IsDesktop",desktop);R.Call(pause,"Resume");}
            yield return null;
        }

        void FinishCallQuestions()
        {
            Tick("callQuestionSeconds"); Tick("callReplySeconds"); Tick("callQuestionSeconds"); Settle();
        }
        void Tick(string field) => R.Call(lesson, "Advance", R.Seconds(settings, field));
        Component Observation(string clue) => ((IEnumerable)R.Call(lesson, "CollectObservations")).Cast<Component>()
            .First(o => o.gameObject.activeInHierarchy && R.Get(o, "clue").ToString() == clue);
        void Activate(string clue) { Settle(); var target = Observation(clue); R.Call(target, "Activate"); Settle(); }
        void Choice()
        {
            Settle();
            var button = ((IEnumerable)R.Get(view, "ChoiceButtons")).Cast<Button>().First();
            Assert.That(button.isActiveAndEnabled && button.interactable, Is.True, button.name);
            button.onClick.Invoke(); Settle();
        }
        void Settle()
        {
            var presentation = R.Get(lesson, "presentation");
            R.Call(R.Get(R.Get(presentation, "space"), "overlay"), "Advance", 1f);
            R.Call(R.Get(R.Get(view, "spatial"), "Transitions"), "Advance", 1f);
            Canvas.ForceUpdateCanvases();
        }
        void LatePresentation() { R.Call(R.Get(lesson, "scenario"), "LateUpdate"); Settle(); }
        static string Compact(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
    }
}
#endif
