#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using R = ProjectWait.M.Tests.RuntimeAccess;

namespace ProjectWait.M.Tests.EditMode
{
    [TestFixture, Category("WaitRegression")]
    public sealed class FlowAndOwnershipTests
    {
        [TestCase(false), TestCase(true)]
        public void RealFlow_CallBranchesRequireExplicitIdentityAndSafetyConfirmation(bool phone)
        {
            var settings = ScriptableObject.CreateInstance(R.Type("ProjectWait.M.Elevator.ElevatorSettings"));
            try
            {
                var flow = Activator.CreateInstance(R.Type(R.Trial + "ElevatorFlow"), settings, true);
                R.Set(flow, "NoResponse", phone);
                R.Call(flow, "Tick", R.Get<float>(R.Type(R.Trial + "ElevatorOpeningSequence"), "DepartureSeconds") + .01f);
                Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("Lobby"));
                R.Call(flow, "Continue");
                Tick(flow, settings, "boardingSeconds");
                R.Call(flow, "Continue");
                Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo("Boarding"), "No floor choice, no departure");
                R.Call(flow, "SelectDestination");
                R.Call(flow, "Continue");
                Tick(flow, settings, "shotStorySeconds");
                R.Call(flow, "Tick", R.Get<float>(settings, "friendReplySeconds") +
                    R.Get<float>(settings, "jumpSeconds") + R.Seconds(settings, "afterJumpSeconds"));
                R.Call(flow, "Tick", 2f);
                R.Call(flow, "RequestIntercom");
                Tick(flow, settings, "connectionSeconds");
                Assert.That(R.Get(flow, "Step").ToString(), Is.EqualTo(phone ? "Unanswered" : "CallQuestion"));
                if (phone) R.Call(flow, "RequestPhone");
                Tick(flow, settings, "callQuestionSeconds");
                Tick(flow, settings, "callReplySeconds");
                Tick(flow, settings, "callQuestionSeconds");
                R.Call(flow, "Tick", 120f);
                Assert.That(R.Get<bool>(flow, "AwaitingCallContinue"), Is.True, "Directions must not time out");
                R.Call(flow, "Continue");
                foreach (var clue in new[] { "Notice", "FacilityGuide", "Intercom" })
                {
                    R.Call(flow, "PreviewObject", R.Clue(clue));
                    Assert.That(R.Get<bool>(flow, "HasObjectPreview"), Is.True);
                    R.Call(flow, "CloseObjectPreview");
                }
                R.Call(flow, "ReportLocation");
                Assert.That(R.Get<bool>(flow, "SearchingIdentity"), Is.True);
                Assert.That(R.Get<bool>(flow, "IdentityFound"), Is.False);
                Assert.That(R.Get<bool>(flow, "LocationReported"), Is.False);
                Assert.That(R.Get<bool>(flow, "DoorPosterRead"), Is.False);
                R.Call(flow, "FindIdentity");
                Assert.That(R.Get<bool>(flow, "ReviewingIdentity"), Is.True);
                R.Call(flow, "ReportLocation");
                Tick(flow, settings, "locationReportSeconds");
                Assert.That(R.Get<bool>(flow, "LocationReported"), Is.True);
                Assert.That(R.Get<bool>(flow, "HelpRequested"), Is.True);
                Assert.That(R.Get<bool>(flow, "UsedPhone"), Is.EqualTo(phone));
                R.Call(flow, "Tick", 120f);
                Assert.That(R.Get<bool>(flow, "AwaitingCallContinue"), Is.True);
                R.Call(flow, "Reset");
                Assert.That(R.Get<bool>(flow, "IsOpening"), Is.True);
                Assert.That(R.Get<bool>(flow, "LocationReported"), Is.False);
                Assert.That(R.Get<bool>(flow, "IdentityFound"), Is.False);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [TestCase(-1f), TestCase(float.NaN), TestCase(float.PositiveInfinity), TestCase(float.NegativeInfinity)]
        public void RealFlow_InvalidDeltaCannotMutateState(float delta)
        {
            var settings = ScriptableObject.CreateInstance(R.Type("ProjectWait.M.Elevator.ElevatorSettings"));
            try
            {
                var flow = Activator.CreateInstance(R.Type(R.Trial + "ElevatorFlow"), settings, false);
                R.Call(flow, "DeveloperJump", 3);
                R.Call(flow, "Tick", 2f);
                var revision = R.Get<int>(flow, "Revision");
                var elapsed = R.Get<float>(flow, "Elapsed");
                var step = R.Get(flow, "Step");
                R.Call(flow, "Tick", delta);
                Assert.That(R.Get<int>(flow, "Revision"), Is.EqualTo(revision));
                Assert.That(R.Get<float>(flow, "Elapsed"), Is.EqualTo(elapsed));
                Assert.That(R.Get(flow, "Step"), Is.EqualTo(step));
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [TestCase(true), TestCase(false)]
        public void FrameRateOwners_ReleaseEitherOrderRestoresExactBaseline(bool olderFirst)
        {
            var global = new GlobalState();
            GameObject a = null, b = null;
            Component first = null, second = null;
            bool acquiredFirst = false, acquiredSecond = false;
            var ownerType = R.Type("ProjectWait.M.FrameRateLimiter");
            var owners = (System.Collections.IList)R.Get(ownerType, "owners");
            Assert.That(owners.Count, Is.Zero, "EditMode must start without live FPS owners");
            var priorSync = R.Get(ownerType, "previousVSyncCount");
            var priorFps = R.Get(ownerType, "previousFrameRate");
            var priorInstalled = R.Get(ownerType, "installedFrameRate");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Application.targetFrameRate = 73;
                QualitySettings.vSyncCount = 2;
                a = new GameObject("Regression FPS owner A"); a.SetActive(false);
                b = new GameObject("Regression FPS owner B"); b.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a, scene);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b, scene);
                first = a.AddComponent(R.Type("ProjectWait.M.FrameRateLimiter"));
                second = b.AddComponent(R.Type("ProjectWait.M.FrameRateLimiter"));
                R.Set(first, "targetFrameRate", 45);
                R.Set(second, "targetFrameRate", 60);
                // OnEnable deliberately refuses EditMode. Seed only its ownership
                // preconditions; exercise the REAL release code and ApplyLimit here.
                // Actual acquisition lifecycle is covered separately in PlayMode.
                R.Set(ownerType, "previousVSyncCount", 2);
                R.Set(ownerType, "previousFrameRate", 73);
                owners.Add(first); R.Set(first, "hasSavedSettings", true); acquiredFirst = true;
                R.Call(first, "ApplyLimit");
                owners.Add(second); R.Set(second, "hasSavedSettings", true); acquiredSecond = true;
                R.Call(second, "ApplyLimit");
                if (olderFirst) { R.Call(first, "OnDisable"); acquiredFirst = false; }
                else { R.Call(second, "OnDisable"); acquiredSecond = false; }
                Assert.That(Application.targetFrameRate, Is.EqualTo(olderFirst ? 60 : 45));
                Assert.That(QualitySettings.vSyncCount, Is.Zero);
                if (olderFirst) { R.Call(second, "OnDisable"); acquiredSecond = false; }
                else { R.Call(first, "OnDisable"); acquiredFirst = false; }
                Assert.That(Application.targetFrameRate, Is.EqualTo(73), "Restore prior FPS, not an assumed -1");
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(2));
            }
            finally
            {
                try
                {
                    try { if (acquiredSecond && second) R.Call(second, "OnDisable"); }
                    finally { if (acquiredFirst && first) R.Call(first, "OnDisable"); }
                }
                finally
                {
                    try { EditorSceneManager.ClosePreviewScene(scene); }
                    finally
                    {
                        owners.Clear();
                        R.Set(ownerType, "previousVSyncCount", priorSync);
                        R.Set(ownerType, "previousFrameRate", priorFps);
                        R.Set(ownerType, "installedFrameRate", priorInstalled);
                        global.Dispose();
                    }
                }
            }
        }

        [Test]
        public void HeightCalibration_WearConfirmationStablePoseAndRewear()
        {
            var policy=Activator.CreateInstance(R.Type(R.Trial+"ElevatorXRHeightCalibration"));
            Func<float,bool,bool?,float,bool> observe=(h,t,p,d)=>(bool)R.Call(policy,"Observe",h,t,p,d);
            for(int i=0;i<20;i++)Assert.That(observe(.3f,true,false,.1f),Is.False,"Unworn tracking cannot commit height");
            for(int i=0;i<20;i++)Assert.That(observe(1.65f,true,null,.1f),Is.False,"Missing presence requires explicit confirmation");
            R.Call(policy,"Request",true);
            Assert.That(observe(1.65f,true,null,.1f),Is.False);
            bool applied=false;for(int i=0;i<8;i++)applied|=observe(1.65f,true,null,.1f);
            Assert.That(applied,Is.True);Assert.That(observe(1.65f,true,null,1f),Is.False);
            observe(1.65f,true,false,.1f);Assert.That(observe(1.7f,true,true,.1f),Is.False);
            applied=false;for(int i=0;i<8;i++)applied|=observe(1.7f,true,true,.1f);
            Assert.That(applied,Is.True,"Rewear must permit a fresh stable calibration");
            R.Call(policy,"Request",true);Assert.That(observe(float.NaN,true,true,1f),Is.False);
            Assert.That(observe(1.7f,false,true,1f),Is.False);
        }

        [Test]
        public void RegressionGate_RejectsCleanUntitledSceneBeforeSnapshot()
        {
            // UTF already owns an untitled bootstrap scene in EditMode, so use
            // an isolated preview scene to exercise the same per-scene gate.
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                Assert.That(scene.isDirty,Is.False);Assert.That(scene.rootCount,Is.Zero);
                Assert.Throws<InvalidOperationException>(()=>R.Call(typeof(R),"RequireSavedEditorScene",scene));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void RegressionSnapshot_QuitRestoresPersistentPlayOptionsWithoutSceneRestore()
        {
            var runner=R.Type("ProjectWait.M.EditorTests.WaitRegressionRunner");
            var snapshotType=runner.GetNestedType("EditorSnapshot",System.Reflection.BindingFlags.NonPublic);
            const string key="ProjectWait.M.Regressions.";
            bool active=UnityEditor.SessionState.GetBool(key+"active",false),waiting=UnityEditor.SessionState.GetBool(key+"waiting",false);
            int phase=UnityEditor.SessionState.GetInt(key+"phase",-1);
            var oldStrings=new System.Collections.Generic.Dictionary<string,string>();
            foreach(var name in new[]{"snapshot","directory","error","job"})oldStrings[name]=UnityEditor.SessionState.GetString(key+name,"");
            bool enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;var options=UnityEditor.EditorSettings.enterPlayModeOptions;
            try
            {
                UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;
                UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;
                var snapshot=R.Call(snapshotType,"Capture");
                UnityEditor.SessionState.SetString(key+"snapshot",JsonUtility.ToJson(snapshot));
                UnityEditor.SessionState.SetString(key+"directory","Assets/M/CodexTemp/RegressionResults~/QuitRegression");
                UnityEditor.SessionState.SetBool(key+"active",true);
                UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;
                UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.None;
                R.Call(runner,"OnQuitting");
                Assert.That(UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,Is.True);
                Assert.That(UnityEditor.EditorSettings.enterPlayModeOptions,Is.EqualTo(UnityEditor.EnterPlayModeOptions.DisableDomainReload));
                Assert.That(UnityEditor.SessionState.GetBool(key+"active",true),Is.False);
            }
            finally
            {
                UnityEditor.EditorSettings.enterPlayModeOptions=options;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=enabled;
                UnityEditor.SessionState.SetBool(key+"active",active);UnityEditor.SessionState.SetBool(key+"waiting",waiting);UnityEditor.SessionState.SetInt(key+"phase",phase);
                foreach(var pair in oldStrings)UnityEditor.SessionState.SetString(key+pair.Key,pair.Value);
            }
        }

        static void Tick(object flow, object settings, string field) => R.Call(flow, "Tick", R.Seconds(settings, field));
    }
}
#endif
