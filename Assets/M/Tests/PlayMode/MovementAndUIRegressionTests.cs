#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using R = ProjectWait.M.Tests.RuntimeAccess;

namespace ProjectWait.M.Tests.PlayMode
{
    [TestFixture("ObservationTrial", "Assets/M/Scene/Elevator_Obsevation_Choice_Test.unity")]
    [TestFixture("DialoguePreview", "Assets/M/Scene/Elevator_Dialogue_Preview_Test.unity")]
    [Category("WaitRegression"), PrebuildSetup(typeof(CleanSceneGate))]
    public sealed class MovementAndUIRegressionTests
    {
        readonly string runtime, scenePath;
        readonly List<GameObject> suspendedRoots = new List<GameObject>();
        readonly HashSet<int> originalHandles = new HashSet<int>();
        GlobalState globals;
        Scene originalActive, loaded;
        Behaviour lesson, controls;
        object flow, settings, view, spatial, map, space;
        Transform rig;
        Camera camera;

        public MovementAndUIRegressionTests(string runtimeName, string path)
        {
            runtime = "ProjectWait.M.Elevator." + runtimeName + ".";
            scenePath = path;
        }

        [UnitySetUp]
        public IEnumerator LoadRealExperience()
        {
            globals = new GlobalState();
            originalActive = SceneManager.GetActiveScene();
            originalHandles.Clear(); loaded = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                originalHandles.Add(scene.handle);
                if (scene.path != R.ScenePath && scene.path != "Assets/M/Scene/Elevator_Dialogue_Preview_Test.unity") continue;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.activeSelf) { suspendedRoots.Add(root); root.SetActive(false); }
            }
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Single(s => s.path == scenePath && !originalHandles.Contains(s.handle));
            SceneManager.SetActiveScene(loaded);
            lesson = loaded.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(R.Type(runtime + "ElevatorLesson"), true)).Cast<Behaviour>().Single();
            controls = loaded.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(R.Type(runtime + "ElevatorDesktopControls"), true)).Cast<Behaviour>().Single();
            yield return null;
            lesson.enabled = false; controls.enabled = false;
            flow = R.Get(lesson, "Flow"); settings = R.Get(lesson, "settings"); view = R.Get(lesson, "view");
            spatial = R.Get(view, "spatial"); space = R.Get(R.Get(lesson, "presentation"), "space");
            map = R.Get(space, "sportsCentre"); rig = (Transform)R.Get(space, "playerRig"); camera = (Camera)R.Get(lesson, "playerCamera");
        }

        [UnityTearDown]
        public IEnumerator RestoreExperience()
        {
            try
            {
                if (!loaded.IsValid())
                    loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                        .FirstOrDefault(s => s.path == scenePath && !originalHandles.Contains(s.handle));
                if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            }
            finally
            {
                try
                {
                    foreach (var root in suspendedRoots) if (root) root.SetActive(true);
                    if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                }
                finally { suspendedRoots.Clear(); globals?.Dispose(); globals = null; }
            }
        }

        [UnityTest]
        public IEnumerator ScriptedTeleport_CentresOffsetCameraAndCapsule()
        {
            var local = camera.transform.localPosition;
            var movement = rig.GetComponent(R.Type(runtime + "ElevatorPlayerMovement"));
            foreach (var offset in new[] { new Vector3(.6f, 0, 1.1f), new Vector3(-.8f, 0, -.5f) })
            foreach (bool resetHeading in new[] { false, true })
            {
                camera.transform.localPosition = local;
                Jump(1);
                camera.transform.localPosition += offset;
                var destination = R.Get<Vector3>(map, "inside");
                R.Call(map, "MoveRig", destination, resetHeading, 180f);
                R.Call(movement, "UpdateCapsule", true);
                AssertHorizontal(camera.transform.position, destination, "Place the camera footprint, not the tracking origin");
                AssertHorizontal(rig.TransformPoint(rig.GetComponent<CharacterController>().center), destination, "Capsule follows the offset camera");
                AssertHorizontal(R.Get<Vector3>(movement, "FeetPosition"), destination, "Reported feet must match the capsule footprint");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Opening_CompletedTeleportDoesNotRecenterLaterTrackedMovement()
        {
            R.Call(lesson, "Restart"); R.Call(lesson, "Advance", 4.3f);
            Assert.That(Step, Is.EqualTo("GymDeparture"));
            R.Call(map, "MoveRig", R.Get<Vector3>(map, "lobby"), false, 0f);
            // Seed the actual completion flag written by the XR branch without
            // requiring a physical XR display; subsequent transition is real flow.
            R.Set(map, "teleported", true);
            camera.transform.localPosition += new Vector3(.35f, 0, .25f);
            var trackedPosition = camera.transform.position;
            R.Call(lesson, "Advance", R.Get<float>(R.Type(runtime + "ElevatorOpeningSequence"), "DepartureSeconds"));
            Assert.That(Step, Is.EqualTo("Lobby"));
            AssertHorizontal(camera.transform.position, trackedPosition, "A completed opening teleport must not cancel later room-scale movement");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Opening_LongFrameStillCommitsMissedArrival()
        {
            R.Call(lesson, "Restart");
            R.Call(lesson, "Advance", R.Get<float>(R.Type(runtime + "ElevatorOpeningSequence"), "DepartureSeconds") + .1f);
            Assert.That(Step, Is.EqualTo("Lobby"));
            AssertHorizontal(camera.transform.position, R.Get<Vector3>(map, "lobby"), "Missing the opening movement interval must still reach the lobby");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boarding_LongFrameStillCommitsArrival()
        {
            Jump(0); R.Call(lesson, "Continue"); R.Call(lesson, "Advance", 3f);
            R.Call(lesson, "Advance", R.Get<float>(settings, "boardingSeconds") - 3f + .1f);
            Assert.That(Step, Is.EqualTo("Boarding"));
            AssertHorizontal(camera.transform.position, R.Get<Vector3>(map, "inside"), "Skipped boarding frames must not leave the player outside");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Exiting_LongFrameStillCommitsArrival()
        {
            BeginExit(); R.Call(lesson, "Advance", 2f);
            R.Call(lesson, "Advance", R.Get<float>(settings, "exitSeconds") - 2f + .1f);
            Assert.That(Step, Is.EqualTo("ReturnLobby"));
            AssertHorizontal(camera.transform.position, R.Get<Vector3>(map, "lobby"), "Skipped exit frames must not leave the player in the cabin");
            yield return null;
        }

        [UnityTest]
        public IEnumerator GoingHome_LongFrameStillCommitsArrival()
        {
            BeginExit(); R.Call(lesson, "Advance", R.Seconds(settings, "exitSeconds"));
            R.Call(lesson, "Continue"); R.Call(lesson, "Advance", 3.6f); R.Call(lesson, "Continue");
            R.Call(lesson, "Advance", 2.8f); R.Call(lesson, "Advance", 3.3f);
            Assert.That(Step, Is.EqualTo("Complete"));
            AssertHorizontal(camera.transform.position, R.Get<Vector3>(map, "groundExit"), "Completion must include the final walk/teleport");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingWorldCard_FadesAtItsOriginalPose()
        {
            Jump(3); R.Call(lesson, "Advance", 2f); Settle(); Activate("Notice");
            R.Call(spatial, "Layout"); Settle();
            var panel = (RectTransform)R.Get(view, "dialoguePanel");
            var speaker = (RectTransform)R.Get(spatial, "speakerTab");
            var localRotation = panel.localRotation; var localScale = panel.localScale; float localZ = panel.localPosition.z;
            var button = ((IEnumerable)R.Get(view, "ChoiceButtons")).Cast<Button>().First();
            var buttonRect = (RectTransform)button.transform;
            var panelCorners = Corners(panel); var speakerCorners = Corners(speaker); var buttonCorners = Corners(buttonRect);
            Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(.99f));
            var angles = R.Get<Vector2>(controls, "LookAngles");
            R.Call(controls, "SetLookAngles", angles.x + 65f, angles.y);
            button.onClick.Invoke();
            Assert.That(R.Get<bool>(flow, "HasObjectPreview"), Is.False);
            Assert.That(panel.gameObject.activeSelf && panel.GetComponent<CanvasGroup>().alpha > .01f, Is.True, "Keep the outgoing card alive for its fade");
            AssertCorners(panel, panelCorners, "Closing card moved before fading");
            AssertCorners(speaker, speakerCorners, "Closing speaker moved before fading");
            AssertCorners(buttonRect, buttonCorners, "Retired close button moved before fading");
            for (int i = 0; i < 8; i++)
            {
                R.Call(view, "Refresh"); R.Call(spatial, "Layout"); R.Call(spatial, "Tick");
                R.Call(R.Get(spatial, "Transitions"), "Advance", .04f);
                if (panel.gameObject.activeSelf && panel.GetComponent<CanvasGroup>().alpha > .001f)
                    AssertCorners(panel, panelCorners, "Layout must not move a fading card");
                yield return null;
            }
            R.Call(R.Get(spatial, "Transitions"), "Advance", 1f);
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Activate("Notice");
            Assert.That(panel.gameObject.activeSelf, Is.True, "The next card must open normally");
            Assert.That(Corners(panel).Zip(panelCorners, Vector3.Distance).Max(), Is.GreaterThan(.2f), "Reopened content may choose a new readable position");
            Assert.That(Quaternion.Angle(panel.localRotation, localRotation), Is.LessThan(.01f), "A reopened card must discard fade-time rotation compensation");
            Assert.That(Vector3.Distance(panel.localScale, localScale), Is.LessThan(.001f), "A reopened card must restore its authored scale");
            Assert.That(panel.localPosition.z, Is.EqualTo(localZ).Within(.001f), "Fade-time depth compensation must not send the next card far away");
            button = ((IEnumerable)R.Get(view, "ChoiceButtons")).Cast<Button>().First(); button.onClick.Invoke();
            R.Call(lesson, "Restart"); R.Call(lesson, "Advance", 8f); Settle();
            Assert.That(Step, Is.EqualTo("Lobby"));
            Assert.That(Quaternion.Angle(panel.localRotation, localRotation), Is.LessThan(.01f), "Restart must discard outgoing local rotation");
            Assert.That(panel.localPosition.z, Is.EqualTo(localZ).Within(.001f), "Restart must retain readable card depth");
        }

        [UnityTest]
        public IEnumerator DesktopTutorial_UsesMouseInstructionAndIcon()
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive) Assert.Ignore("Desktop instruction check requires the desktop input mode.");
            Jump(0);
            var tutorial = loaded.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(R.Type(runtime + "ElevatorTutorialPreview"), true)).Single();
            R.Call(tutorial, "Tick", 2f); R.Call(tutorial, "Tick", 2f);
            Assert.That(R.Get<bool>(tutorial, "IsVisible"), Is.True);
            Assert.That(R.Get<string>(tutorial, "Instruction"), Does.Contain("클릭"));
            Assert.That(R.Get<string>(tutorial, "Instruction"), Does.Not.Contain("트리거"));
            var icon = tutorial.GetComponentsInChildren(R.Type(runtime + "ElevatorVRSelectGraphic"), true).Single();
            Assert.That(R.Get<bool>(icon, "ControllerMode"), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boarding_ShowsBothTasksTogetherAndAcceptsEitherObjectOrder()
        {
            var tutorial = loaded.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(R.Type(runtime + "ElevatorTutorialPreview"), true)).Single();
            foreach (bool floorFirst in new[] { true, false })
            {
                Jump(1);
                var text = R.Get<string>(R.Get(view, "dialogueText"), "text");
                Assert.That(text, Does.Contain("1층"));
                Assert.That(text, Does.Contain("공"), "Both requests must appear in the first friend card, including scenario overrides");
                R.Call(tutorial, "Tick", 0f); R.Call(tutorial, "Tick", 2f); R.Call(tutorial, "Tick", 2f);
                Assert.That(R.Get<bool>(tutorial, "IsVisible"), Is.True, "Show both tasks immediately, not only after an eight-second reminder");
                Assert.That(R.Get(tutorial, "DisplayedStep").ToString(), Is.EqualTo("BoardingActions"));
                Assert.That(R.Get<string>(tutorial, "Instruction"), Does.Contain("순서"));
                var observations = ((IEnumerable)R.Call(lesson, "CollectObservations")).Cast<Component>().ToArray();
                var ball = observations.First(o => R.Get(o, "clue").ToString() == "Ball");
                var floor = observations.First(o => o.gameObject.activeInHierarchy && R.Get(o, "clue").ToString() == "FrontFloorButtons");
                R.Call(ball, "Hover", true); R.Call(tutorial, "Tick", .1f);
                Assert.That(R.Get(tutorial, "Target"), Is.SameAs(ball), "The combined guide must not force the floor target first");
                R.Call(ball, "Hover", false); R.Call(floor, "Hover", true); R.Call(tutorial, "Tick", .1f);
                Assert.That(R.Get(tutorial, "Target"), Is.SameAs(floor));
                R.Call(floor, "Hover", false);
                Activate(floorFirst ? "FrontFloorButtons" : "Ball");
                Assert.That(Step, Is.EqualTo("Boarding"));
                Assert.That(R.Get<bool>(flow, "PlayerHasBall"), Is.EqualTo(!floorFirst));
                Assert.That(((Transform)R.Get(R.Get(lesson, "presentation"), "friendBall")).gameObject.activeSelf, Is.EqualTo(floorFirst));
                text = R.Get<string>(R.Get(view, "dialogueText"), "text");
                Assert.That(text, Does.Contain(floorFirst ? "공" : "1층"), "Only the remaining request should be repeated");
                R.Call(tutorial, "Tick", 0f); R.Call(tutorial, "Tick", 2f); R.Call(tutorial, "Tick", 2f);
                Assert.That(R.Get(tutorial, "DisplayedStep").ToString(), Is.EqualTo(floorFirst ? "ReceiveBall" : "ChooseFloor"));
                Activate(floorFirst ? "Ball" : "FloorButtons");
                Assert.That(Step, Is.EqualTo("Proposal"));
                Assert.That(R.Get<bool>(flow, "DestinationSelected"), Is.True);
                Assert.That(R.Get<bool>(flow, "PlayerHasBall"), Is.True);
                R.Call(tutorial, "Tick", 0f); R.Call(tutorial, "Tick", 2f);
                Assert.That(R.Get<bool>(tutorial, "IsVisible"), Is.False);
            }
            R.Call(lesson, "Restart");
            Assert.That(R.Get<bool>(flow, "BallReceived"), Is.False);
            Assert.That(R.Get<bool>(flow, "DestinationSelected"), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoorSafety_DialogueExplainsReliefAndRestInsteadOfFear()
        {
            Jump(5);
            Activate("Friend");
            string text = R.Get<string>(R.Get(view, "dialogueText"), "text");
            Assert.That(text, Does.Contain("안심"), "The friend has just heard that rescuers are coming");
            Assert.That(text, Does.Contain("다리"));
            Assert.That(text, Does.Contain("쉴"));
            Assert.That(text, Does.Not.Contain("무서"));
            Assert.That(text, Does.Not.Contain("불안"));
            var dialogue = R.Type(runtime + "ElevatorDialogue");
            Assert.That(R.Get<string>(dialogue, "FriendSettled"), Does.Contain("쉬"));
            Assert.That(R.Get<string>(dialogue, "FriendWaiting"), Does.Not.Contain("무서"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoorSafety_RescueAdviceRelaxesFaceAndLegsBeforeTheLean()
        {
            var friend = (Transform)R.Get(lesson, "friend");
            var performance = lesson.GetComponent(R.Type(runtime + "ElevatorCharacterPerformance"));
            var expressions = friend.GetComponentsInChildren(R.Type(runtime + "FriendImprovedExpressions"), true).Single();
            var door = R.Get(lesson, "doorSafetyCutscene");
            var presentation = R.Get(lesson, "presentation");
            Action pose = () =>
            {
                R.Call(presentation, "LateUpdate"); R.Call(performance, "LateUpdate");
                R.Call(door, "LateUpdate"); R.Call(expressions, "LateUpdate");
            };
            var face = (SkinnedMeshRenderer)R.Get(expressions, "face");
            var normalMesh = face.sharedMesh; // Optional legacy panic meshes are unassigned in both retained scenes.
            Func<string, float> weight = name => face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex(name));
            Jump(3); pose();
            Assert.That(R.Get<bool>(performance, "PanicVisible") || weight("Surprised") > 50, Is.True, "The original fault should still be startling");
            Jump(4);
            foreach (var field in new[] { "callQuestionSeconds", "callReplySeconds", "callQuestionSeconds" })
                R.Call(lesson, "Advance", R.Seconds(settings, field));
            R.Call(lesson, "Continue"); Activate("Identity"); R.Call(lesson, "Report");
            R.Call(lesson, "Advance", R.Seconds(settings, "locationReportSeconds"));
            Assert.That(Step, Is.EqualTo("CallSafetyAdvice"));
            pose();
            Assert.That(R.Get<bool>(performance, "PanicVisible"), Is.False, "Rescue assurance must remove the panic-face overlay before the door scene");
            Assert.That(face.sharedMesh, Is.SameAs(normalMesh));
            Assert.That(weight("Worried"), Is.Zero);
            Assert.That(weight("Smile"), Is.InRange(5f, 20f), "Use a small relieved smile, not a celebratory grin");
            R.Call(lesson, "Continue"); pose();
            var knee = (Transform)R.Get(performance, "leftKnee");
            var rest = knee.localRotation;
            var inside = friend.position;
            float duration = R.Get<float>(settings, "doorLeanSeconds");
            R.Call(lesson, "Advance", duration * .15f); pose();
            AssertHorizontal(friend.position, inside, "Let the sigh read before stepping toward the door");
            R.Call(lesson, "Advance", duration * .25f); pose(); pose();
            Assert.That(Quaternion.Angle(knee.localRotation, rest), Is.InRange(2f, 12f), "The knees soften slightly without a collapse");
            Assert.That(R.Get<bool>(performance, "PanicVisible"), Is.False);
            Assert.That(weight("Worried"), Is.Zero);
            R.Call(lesson, "Advance", duration * .61f); Settle(); pose();
            Assert.That(Step, Is.EqualTo("DoorPosterPointing"));
            Assert.That(weight("Smile"), Is.InRange(5f, 20f));
            Activate("Notice"); Activate("Friend");
            ((IEnumerable)R.Get(view, "ChoiceButtons")).Cast<Button>().First().onClick.Invoke(); Settle();
            Assert.That(Step, Is.EqualTo("DoorPosterWarning"));
            foreach (var field in new[] { "doorWarningSeconds", "safeWallMoveSeconds" })
                R.Call(lesson, "Advance", R.Seconds(settings, field));
            pose(); pose();
            Assert.That(Step, Is.EqualTo("SafeWallWaiting"));
            Assert.That(R.Get<bool>(flow, "SafeWallReached"), Is.True);
            Assert.That(Quaternion.Angle(knee.localRotation, rest), Is.LessThan(.1f), "The supported waiting pose must release the temporary knee bend");
            Assert.That(weight("Worried"), Is.Zero);
            Assert.That(R.Get<bool>(performance, "PanicVisible"), Is.False);
            R.Call(lesson, "Restart"); pose();
            Assert.That(Quaternion.Angle(knee.localRotation, rest), Is.LessThan(.1f));
            yield return null;
        }

        string Step => R.Get(flow, "Step").ToString();
        void BeginExit() { Jump(6); R.Call(lesson, "Advance", R.Seconds(settings, "rescueSubtitleSeconds")); R.Call(lesson, "Continue"); }
        void Jump(int stage) { R.Call(lesson, "DeveloperJump", stage); Settle(); }
        void Settle()
        {
            R.Call(R.Get(space, "overlay"), "Advance", 1f);
            R.Call(R.Get(spatial, "Transitions"), "Advance", 1f);
            Canvas.ForceUpdateCanvases();
        }
        void Activate(string clue)
        {
            Settle();
            var observation = ((IEnumerable)R.Call(lesson, "CollectObservations")).Cast<Component>()
                .First(o => o.gameObject.activeInHierarchy && R.Get(o, "clue").ToString() == clue);
            R.Call(observation, "Activate"); Settle();
        }
        static Vector3[] Corners(RectTransform rect) { var values = new Vector3[4]; rect.GetWorldCorners(values); return values; }
        static void AssertHorizontal(Vector3 actual, Vector3 expected, string message) =>
            Assert.That(Vector2.Distance(new Vector2(actual.x, actual.z), new Vector2(expected.x, expected.z)), Is.LessThan(.002f), message);
        static void AssertCorners(RectTransform rect, Vector3[] expected, string message) =>
            Assert.That(Corners(rect).Zip(expected, Vector3.Distance).Max(), Is.LessThan(.002f), message);
    }
}
#endif
