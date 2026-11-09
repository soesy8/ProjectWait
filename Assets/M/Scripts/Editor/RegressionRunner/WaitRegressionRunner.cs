#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using ProjectWait.M.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectWait.M.EditorTests
{
    // SessionState survives the UTF PlayMode domain reload; callbacks are registered
    // again on every reload, instead of relying on a transient static API instance.
    [InitializeOnLoad]
    public static class WaitRegressionRunner
    {
        const string Key = "ProjectWait.M.Regressions.";
        const string OutputRoot = "Assets/M/CodexTemp/RegressionResults~/";
        const string EditAssembly = "ProjectWait.M.Tests.EditMode";
        const string PlayAssembly = "ProjectWait.M.Tests.PlayMode";
        static readonly Callback Sink = new Callback();
        static TestRunnerApi api;
        static bool completing;

        static WaitRegressionRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.hideFlags = HideFlags.HideAndDontSave;
            api.RegisterCallbacks(Sink);
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuitting;
        }

        [MenuItem("Tools/Wait/Tests/Run all regressions")]
        public static void RunAll()
        {
            if (SessionState.GetBool(Key + "active", false) || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating || AnyTestRunActive())
            {
                Debug.LogWarning("Wait regressions: finish the active test/compile/play operation first.");
                return;
            }
            try
            {
                RuntimeAccess.RequireCleanEditorScenes();
                var snapshot = EditorSnapshot.Capture();
                var directory = OutputRoot + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                Directory.CreateDirectory(directory);
                SessionState.SetString(Key + "snapshot", JsonUtility.ToJson(snapshot));
                SessionState.SetString(Key + "directory", directory);
                SessionState.SetInt(Key + "phase", 0);
                SessionState.SetBool(Key + "waiting", false);
                SessionState.SetBool(Key + "active", true);
                SessionState.SetString(Key + "error", "");
                EditorApplication.delayCall += LaunchPhase;
            }
            catch (Exception ex) { Debug.LogWarning("Wait regressions not started: " + ex.Message); }
        }

        [MenuItem("Tools/Wait/Tests/Run all regressions", true)]
        static bool CanRun() => !SessionState.GetBool(Key + "active", false) &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !AnyTestRunActive();

        [MenuItem("Tools/Wait/Tests/Cancel regressions and restore editor")]
        public static void Cancel()
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            SessionState.SetString(Key + "error", "Cancelled by user");
            var job = SessionState.GetString(Key + "job", "");
            if (!string.IsNullOrEmpty(job) && TestRunnerApi.CancelTestRun(job)) return;
            SessionState.SetInt(Key + "phase", 2);
            SessionState.SetBool(Key + "waiting", false);
        }

        static bool AnyTestRunActive()
        {
            // UTF 1.6.0 exposes this only internally. Inspect the installed shape;
            // no use of nonexistent public IsTestRunActive or obsolete Execute APIs.
            var method = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.Static | BindingFlags.NonPublic);
            return method != null && (bool)method.Invoke(null, null);
        }

        static void LaunchPhase()
        {
            if (!SessionState.GetBool(Key + "active", false) || SessionState.GetBool(Key + "waiting", false)) return;
            int phase = SessionState.GetInt(Key + "phase", 0);
            if (phase >= 2) return;
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                    EditorApplication.isPlayingOrWillChangePlaymode || AnyTestRunActive()) return;
                RuntimeAccess.RequireCleanEditorScenes();
                // Disable configurable fast-enter mode for a reproducible test lifecycle.
                // The exact option/value is restored after both UTF jobs have cleaned up.
                EditorSettings.enterPlayModeOptionsEnabled = false;
                SessionState.SetBool(Key + "waiting", true);
                var settings = new ExecutionSettings(new Filter
                {
                    testMode = phase == 0 ? TestMode.EditMode : TestMode.PlayMode,
                    assemblyNames = new[] { phase == 0 ? EditAssembly : PlayAssembly },
                    categoryNames = new[] { "WaitRegression" }
                }) { runSynchronously = false };
                var job = api.Execute(settings);
                SessionState.SetString(Key + "job", job);
            }
            catch (Exception ex) { FailInfrastructure(ex.ToString()); }
        }

        static void Update()
        {
            if (!SessionState.GetBool(Key + "active", false) || completing ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (SessionState.GetBool(Key + "waiting", false))
            {
                var method=typeof(TestRunnerApi).GetMethod("IsRunning",BindingFlags.Static|BindingFlags.NonPublic);
                var job=SessionState.GetString(Key+"job","");
                if(method==null||string.IsNullOrEmpty(job)||(bool)method.Invoke(null,new object[]{job}))return;
                FailInfrastructure("UTF job ended without a completion callback (cancelled or interrupted).");
            }
            if (AnyTestRunActive() || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetInt(Key + "phase", 0) < 2) LaunchPhase();
            else Complete();
        }

        static void FailInfrastructure(string message)
        {
            SessionState.SetString(Key + "error", message);
            SessionState.SetInt(Key + "phase", 2);
            SessionState.SetBool(Key + "waiting", false);
            Debug.LogWarning("Wait regression infrastructure failed: " + message);
        }

        static void Complete()
        {
            completing = true;
            var directory = SessionState.GetString(Key + "directory", "");
            string restorationError = "";
            try
            {
                try
                {
                    var snapshot = JsonUtility.FromJson<EditorSnapshot>(SessionState.GetString(Key + "snapshot", ""));
                    snapshot?.Restore();
                }
                catch (Exception ex) { restorationError = ex.ToString(); }
                var report = new RunSummary
                {
                    completedUtc = DateTime.UtcNow.ToString("O"),
                    infrastructureError = SessionState.GetString(Key + "error", ""),
                    restorationError = restorationError,
                    editMode = ReadPhase(directory, "EditMode"),
                    playMode = ReadPhase(directory, "PlayMode")
                };
                report.passed = string.IsNullOrEmpty(report.infrastructureError) && string.IsNullOrEmpty(restorationError) &&
                    report.editMode != null && report.playMode != null && report.editMode.passed && report.playMode.passed;
                File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(report, true));
                Debug.Log("Wait regressions " + (report.passed ? "PASS" : "FAIL / incomplete") + ": " + directory + "/summary.json");
            }
            catch (Exception ex) { Debug.LogWarning("Wait regression report/restore error: " + ex); }
            finally
            {
                SessionState.SetBool(Key + "active", false);
                SessionState.SetBool(Key + "waiting", false);
                SessionState.EraseString(Key + "job");
                completing = false;
            }
        }

        static PhaseReport ReadPhase(string directory, string mode)
        {
            var path = Path.Combine(directory, mode + ".json");
            return File.Exists(path) ? JsonUtility.FromJson<PhaseReport>(File.ReadAllText(path)) : null;
        }

        static void OnQuitting()
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            const string message="Editor closed before regression completion";
            string restorationError="";
            try
            {
                // Scene restoration/Play teardown is unsafe during shutdown. Only
                // restore the persistent options that this runner changed itself.
                var snapshot=JsonUtility.FromJson<EditorSnapshot>(SessionState.GetString(Key+"snapshot",""));
                snapshot?.RestorePlayOptions();
            }
            catch(Exception ex){restorationError=ex.ToString();}
            try
            {
                string directory=SessionState.GetString(Key+"directory","");
                if(!directory.StartsWith(OutputRoot,StringComparison.Ordinal)||directory.Contains(".."))throw new InvalidOperationException("Invalid regression report path");
                Directory.CreateDirectory(directory);
                var report=new RunSummary{completedUtc=DateTime.UtcNow.ToString("O"),passed=false,infrastructureError=message,
                    restorationError=restorationError,editMode=ReadPhase(directory,"EditMode"),playMode=ReadPhase(directory,"PlayMode")};
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(report,true));
            }
            catch(Exception ex){Debug.LogWarning("Wait shutdown report: "+ex.Message);}
            finally
            {
                SessionState.SetString(Key+"error",message);SessionState.SetInt(Key+"phase",2);
                SessionState.SetBool(Key+"active",false);SessionState.SetBool(Key+"waiting",false);
            }
        }

        sealed class Callback : IErrorCallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void OnError(string message)
            {
                if (SessionState.GetBool(Key + "active", false)) FailInfrastructure(message);
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(Key + "active", false) || !SessionState.GetBool(Key + "waiting", false)) return;
                int phase = SessionState.GetInt(Key + "phase", 0);
                var mode = phase == 0 ? "EditMode" : "PlayMode";
                try
                {
                    var directory = SessionState.GetString(Key + "directory", "");
                    TestRunnerApi.SaveResultToFile(result, Path.Combine(directory, mode + ".xml"));
                    var leaves = new List<TestCaseReport>();
                    Collect(result, leaves);
                    var report = new PhaseReport
                    {
                        mode = mode, resultState = result.ResultState, durationSeconds = result.Duration,
                        passCount = result.PassCount, failCount = result.FailCount,
                        skipCount = result.SkipCount, inconclusiveCount = result.InconclusiveCount,
                        cases = leaves.ToArray()
                    };
                    report.countMatches = leaves.Count == report.passCount + report.failCount + report.skipCount + report.inconclusiveCount;
                    report.passed = report.countMatches && leaves.Count > 0 && report.passCount == leaves.Count && report.failCount == 0;
                    File.WriteAllText(Path.Combine(directory, mode + ".json"), JsonUtility.ToJson(report, true));
                    SessionState.SetInt(Key + "phase", string.IsNullOrEmpty(SessionState.GetString(Key + "error", ""))
                        ? Math.Max(phase + 1, SessionState.GetInt(Key + "phase", 0)) : 2);
                    SessionState.SetBool(Key + "waiting", false);
                }
                catch (Exception ex) { FailInfrastructure(ex.ToString()); }
            }
            static void Collect(ITestResultAdaptor result, List<TestCaseReport> cases)
            {
                if (result.HasChildren) { foreach (var child in result.Children) Collect(child, cases); return; }
                // Empty suites are not passing test cases; a zero-test run stays failed.
                if (result.Test.IsSuite) return;
                cases.Add(new TestCaseReport
                {
                    name = result.FullName, resultState = result.ResultState, durationSeconds = result.Duration,
                    message = result.Message, stackTrace = result.StackTrace, output = result.Output
                });
            }
        }

        [Serializable] sealed class TestCaseReport
        {
            public string name, resultState, message, stackTrace, output;
            public double durationSeconds;
        }
        [Serializable] sealed class PhaseReport
        {
            public string mode, resultState;
            public double durationSeconds;
            public int passCount, failCount, skipCount, inconclusiveCount;
            public bool passed, countMatches;
            public TestCaseReport[] cases;
        }
        [Serializable] sealed class RunSummary
        {
            public string completedUtc, infrastructureError, restorationError;
            public bool passed;
            public PhaseReport editMode, playMode;
        }

        [Serializable] sealed class SavedScene
        {
            // Unity's SceneSetup exposes properties; JsonUtility serializes fields only.
            public string path;
            public bool isLoaded, isActive;
            public SceneSetup ToSetup() => new SceneSetup { path = path, isLoaded = isLoaded, isActive = isActive };
        }

        [Serializable] sealed class EditorSnapshot
        {
            public SavedScene[] scenes;
            public string[] selection;
            public string qualityPipeline;
            public float timeScale, fixedDelta, volume;
            public int fps, vSync, enterPlayOptions;
            public bool audioPause, cursorVisible, runInBackground, enterPlayEnabled, paused;
            public int cursorLock;

            public static EditorSnapshot Capture() => new EditorSnapshot
            {
                scenes = EditorSceneManager.GetSceneManagerSetup().Select(s => new SavedScene
                    { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive }).ToArray(),
                selection = Selection.objects.Select(o => GlobalObjectId.GetGlobalObjectIdSlow(o).ToString()).ToArray(),
                qualityPipeline = QualitySettings.renderPipeline ? AssetDatabase.GetAssetPath(QualitySettings.renderPipeline) : "",
                timeScale = Time.timeScale, fixedDelta = Time.fixedDeltaTime, volume = AudioListener.volume,
                fps = Application.targetFrameRate, vSync = QualitySettings.vSyncCount, audioPause = AudioListener.pause,
                cursorVisible = Cursor.visible, cursorLock = (int)Cursor.lockState, runInBackground = Application.runInBackground,
                enterPlayEnabled = EditorSettings.enterPlayModeOptionsEnabled, enterPlayOptions = (int)EditorSettings.enterPlayModeOptions,
                paused = EditorApplication.isPaused
            };

            public void Restore()
            {
                try
                {
                    // Never discard newly dirty scenes, even after a failing test.
                    var current = EditorSceneManager.GetSceneManagerSetup();
                    bool same = scenes != null && current.Length == scenes.Length && current.Zip(scenes, (a, b) =>
                        a.path == b.path && a.isLoaded == b.isLoaded && a.isActive == b.isActive).All(v => v);
                    if (!same && scenes != null && scenes.Length > 0)
                    {
                        RuntimeAccess.RequireCleanEditorScenes();
                        EditorSceneManager.RestoreSceneManagerSetup(scenes.Select(s => s.ToSetup()).ToArray());
                    }
                    Selection.objects = (selection ?? Array.Empty<string>()).Select(id =>
                    {
                        GlobalObjectId global;
                        return GlobalObjectId.TryParse(id, out global) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(global) : null;
                    }).Where(o => o).ToArray();
                }
                finally
                {
                    QualitySettings.renderPipeline = string.IsNullOrEmpty(qualityPipeline) ? null : AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(qualityPipeline);
                    Application.targetFrameRate = fps; QualitySettings.vSyncCount = vSync;
                    Time.timeScale = timeScale; Time.fixedDeltaTime = fixedDelta;
                    AudioListener.pause = audioPause; AudioListener.volume = volume;
                    Cursor.lockState = (CursorLockMode)cursorLock; Cursor.visible = cursorVisible;
                    Application.runInBackground = runInBackground;
                    RestorePlayOptions();
                    EditorApplication.isPaused = paused;
                }
            }
            public void RestorePlayOptions()
            {
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)enterPlayOptions;
                EditorSettings.enterPlayModeOptionsEnabled = enterPlayEnabled;
            }
        }
    }
}
#endif
