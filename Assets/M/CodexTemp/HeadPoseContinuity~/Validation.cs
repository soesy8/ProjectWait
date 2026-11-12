using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectWait.M.SceneTransition;
using ProjectWait.M.TitleScene;
using ProjectWait.M.Elevator.DialoguePreview;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class HeadPoseContinuityValidation
{
    const string Output = "Assets/M/CodexTemp/HeadPoseContinuity~/";
    static readonly List<object> checks = new List<object>();
    static readonly List<string> errors = new List<string>();
    static int stage, frames;
    static double began, heldAt;
    static float oldScale, height;
    static Quaternion rotation;
    static TitlePlazaPlayer player;
    static StorybookSceneTransition transition;
    static bool turnedAway, checkedGate;

    static void Check(string name, bool passed, object actual = null)
    {
        checks.Add(new { name, passed, actual });
        if (!passed) errors.Add(name);
    }
    static bool Near(float a, float b, float tolerance = .001f) => Mathf.Abs(a - b) < tolerance;
    static void Write(string file) => File.WriteAllText(Output + file,
        JsonConvert.SerializeObject(new { checks, errors, passed = errors.Count == 0 }, Formatting.Indented));

    public static object EditChecks()
    {
        checks.Clear(); errors.Clear();
        foreach (float h in new[] { .72f, 1.35f, 1.82f })
        {
            var a = new GameObject("source head fixture");
            var b = new GameObject("destination head fixture");
            try
            {
                a.transform.SetPositionAndRotation(new Vector3(4, 0, 2), Quaternion.Euler(0, 42, 0));
                var sourceEye = new GameObject("eye").transform; sourceEye.SetParent(a.transform, false);
                sourceEye.localPosition = new Vector3(.65f, h, -.3f);
                sourceEye.localRotation = Quaternion.Euler(-12, 35, 3);
                var pose = new SceneTransitionHeadPose(sourceEye, 0, true);
                b.transform.SetPositionAndRotation(new Vector3(-1.8f, 7.92f, -13), Quaternion.Euler(0, 130, 0));
                var offset = new GameObject("Camera Offset").transform; offset.SetParent(b.transform, false);
                offset.localPosition = Vector3.up * 1.1064756f;
                var eye = new GameObject("eye").transform; eye.SetParent(offset, false);
                eye.localPosition = new Vector3(.65f, .24f, -.3f);
                eye.localRotation = sourceEye.localRotation;
                Vector3 spawn = b.transform.position;
                Vector3 roomOffset = eye.position - spawn; roomOffset.y = 0;
                b.transform.position -= roomOffset; // Presentation already centered the head.
                Vector3 local = eye.localPosition; Quaternion localRotation = eye.localRotation;
                pose.Apply(b.transform, eye, 7.92f, true, spawn);
                Check("XR floor-relative eye height " + h, Near(eye.position.y - 7.92f, h), eye.position.y - 7.92f);
                Check("XR head footprint, not origin " + h, Near(eye.position.x, -1.8f) && Near(eye.position.z, -13), eye.position.ToString());
                Check("XR world gaze preserved " + h, Quaternion.Angle(eye.rotation, sourceEye.rotation) < .01f);
                Check("XR local tracked pose untouched " + h, eye.localPosition == local && Quaternion.Angle(eye.localRotation, localRotation) < .01f);
                eye.localRotation = Quaternion.Euler(8,42,-2);
                pose.Apply(b.transform, eye, 7.92f, true, spawn);
                Check("XR head rotation during loading remains live " + h, Quaternion.Angle(eye.rotation, a.transform.rotation * eye.localRotation) < .01f);
                eye.localPosition += Vector3.up * .1f;
                Check("XR physical head remains live " + h, Near(eye.position.y - 7.92f, h + .1f));
                var reverse = new SceneTransitionHeadPose(eye, 7.92f, true);
                reverse.Apply(a.transform, sourceEye, 0, true, a.transform.position);
                Check("XR reverse scene handoff " + h, Near(sourceEye.position.y, h + .1f));
                sourceEye.rotation = Quaternion.Euler(26, 77, 0);
                var pc = new SceneTransitionHeadPose(sourceEye, 0, false);
                pc.Apply(b.transform, eye, 7.92f, false, spawn);
                Check("PC yaw/pitch/height preserved " + h, Quaternion.Angle(eye.rotation, pc.EyeRotation) < .01f && Near(eye.position.y - 7.92f, pc.EyeHeight));
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
        var calibration = new ElevatorXRHeightCalibration();
        calibration.AcceptTransferredHeight();
        Check("Transferred height skips duplicate automatic calibration", calibration.Calibrated && !calibration.Observe(1.7f, true, true, .1f) && !calibration.Pending);
        calibration.Observe(1.7f, true, false, .1f);
        Check("Headset removal still requests calibration", calibration.Pending && !calibration.Calibrated);
        calibration.Request(true);
        bool calibrated = false;
        for (int i = 0; i < 10; i++) calibrated |= calibration.Observe(1.7f, true, null, .1f);
        Check("Explicit recalibration still works", calibrated && calibration.Calibrated);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/SceneTransition/StoryTransitionKorean.asset");
        Check("Every Korean guidance glyph exists in the static font", SceneTransitionView.FaceForwardHint.All(c => font.HasCharacter(c)));
        Check("Scripts compiled", !EditorUtility.scriptCompilationFailed);
        Write("edit-validation.json");
        return new { count = checks.Count, errors };
    }

    public static object StartRuntime()
    {
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/M/Scene/Wait_Robby.unity") throw new Exception("Start in the saved M lobby Play Mode.");
        checks.Clear(); errors.Clear(); stage = frames = 0; turnedAway = checkedGate = false; heldAt = 0;
        began = EditorApplication.timeSinceStartup; oldScale = Time.timeScale;
        Application.logMessageReceived += Log;
        transition = StorybookSceneTransition.Current;
        transition.fadeOutSeconds = .35f; transition.fadeInSeconds = .35f;
        transition.minimumHoldSeconds = .5f; transition.faceForwardSeconds = .2f;
        player = Object.FindFirstObjectByType<TitlePlazaPlayer>();
        player.eyeHeight = 1.57f; player.viewCamera.transform.localPosition = Vector3.up * player.eyeHeight;
        player.AddLookDelta(new Vector2(270, -8 / player.lookSensitivity));
        height = player.viewCamera.transform.position.y - player.FloorHeight;
        rotation = player.viewCamera.transform.rotation;
        Time.timeScale = 0;
        Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None).First(p => p.destinationScene.EndsWith("Wait_Elevator.unity")).Activate();
        Check("Real lobby portal started transition", StorybookSceneTransition.IsBusy);
        EditorApplication.update += Tick;
        return new { started = true, height, rotation = rotation.eulerAngles.ToString() };
    }

    public static object FinalizeEditor()
    {
        checks.Clear(); errors.Clear();
        var original = SceneManager.GetActiveScene();
        Check("Play Mode ended", !Application.isPlaying);
        Check("Original saved lobby restored", original.path == "Assets/M/Scene/Wait_Robby.unity" && !original.isDirty && SceneManager.sceneCount == 1);
        foreach (string path in new[] { "Assets/M/Scene/Wait_Robby.unity", "Assets/M/Scene/Wait_Elevator.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
                int missingScripts = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
                int missingReferences = 0;
                foreach (var component in objects.SelectMany(g => g.GetComponents<Component>()).Where(c => c))
                {
                    using (var so = new SerializedObject(component))
                    {
                        var property = so.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0) missingReferences++;
                    }
                }
                Check(path + " no Missing Script", missingScripts == 0, missingScripts);
                Check(path + " no missing serialized reference", missingReferences == 0, missingReferences);
                Check(path + " no temporary runtime dependency", !AssetDatabase.GetDependencies(path, true).Any(p => p.StartsWith("Assets/M/CodexTemp/")));
                var settings = objects.Select(g => g.GetComponent<StorybookSceneTransition>()).FirstOrDefault(c => c);
                Check(path + " default forward hold", settings && Near(settings.faceForwardSeconds, .4f));
            }
            finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(original);
        Check("No scene was saved or dirtied", !original.isDirty && SceneManager.sceneCount == 1);
        Check("No script compilation error", !EditorUtility.scriptCompilationFailed);
        Write("scene-validation.json");
        return new { count = checks.Count, errors };
    }

    static void Log(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text);
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - began > 60) throw new Exception("Runtime handoff test timeout: stage=" + stage + ", phase=" + transition.Phase);
            if (stage == 0)
            {
                if (!turnedAway && transition.Phase == StorybookSceneTransition.TransitionPhase.Holding)
                { player.AddLookDelta(new Vector2(700, 0)); turnedAway = true; heldAt = EditorApplication.timeSinceStartup; }
                if (turnedAway && !checkedGate && EditorApplication.timeSinceStartup - heldAt > 1.2)
                {
                    Check("Looking away cannot activate destination", transition.Phase == StorybookSceneTransition.TransitionPhase.Holding);
                    Check("Level head-height guide", Near(transition.View.PanelAnchor.y, player.viewCamera.transform.position.y));
                    Check("Visible forward instruction", transition.View.StatusText.text == SceneTransitionView.FaceForwardHint);
                    Capture(player.viewCamera, "01-forward-guide.png");
                    player.AddLookDelta(new Vector2(-700, 0)); checkedGate = true;
                }
                if (SceneManager.GetActiveScene().name == "Wait_Elevator" && transition.Phase == StorybookSceneTransition.TransitionPhase.Revealing)
                {
                    var controls = Object.FindFirstObjectByType<ElevatorDesktopControls>();
                    var eye = controls.targetCamera.transform;
                    Check("PC entry height matches source head", Near(eye.position.y - 7.92f, height), eye.position.y - 7.92f);
                    Check("PC entry yaw/pitch matches source gaze", Quaternion.Angle(eye.rotation, rotation) < .05f, eye.eulerAngles.ToString());
                    Check("PC stored calibration height matches transfer", Near(controls.eyeHeight, height));
                    Check("Capsule feet stay on destination floor", Near(controls.movement.FeetPosition.y, 7.92f));
                    Capture(controls.targetCamera, "02-elevator-arrival.png");
                    stage = 1; frames = 0;
                }
            }
            else if (stage == 1 && !StorybookSceneTransition.IsBusy)
            {
                var controls = Object.FindFirstObjectByType<ElevatorDesktopControls>();
                if (++frames < 20) return;
                Check("Following PC frames do not undo transferred gaze", Quaternion.Angle(controls.targetCamera.transform.rotation, rotation) < .05f);
                // Exercise the authored walk that previously overwrote the origin's Y offset.
                controls.lesson.Advance(3);
                controls.lesson.Advance(.5f);
                Check("Authored opening walk preserves head height", Near(controls.targetCamera.transform.position.y - 7.92f, height), controls.targetCamera.transform.position.y - 7.92f);
                controls.SetLookAngles(49, 0); rotation = controls.targetCamera.transform.rotation;
                bool loaded = transition.TryLoad("Assets/M/Scene/Wait_Robby.unity", "로비", "다음 이야기", controls.targetCamera);
                Check("Reverse elevator-to-lobby load accepted", loaded);
                stage = 2; frames = 0;
            }
            else if (stage == 2 && SceneManager.GetActiveScene().name == "Wait_Robby" && transition.Phase == StorybookSceneTransition.TransitionPhase.Revealing)
            {
                player = Object.FindFirstObjectByType<TitlePlazaPlayer>();
                Check("Reverse head height preserved", Near(player.viewCamera.transform.position.y - player.FloorHeight, height), player.viewCamera.transform.position.y - player.FloorHeight);
                Check("Reverse gaze preserved", Quaternion.Angle(player.viewCamera.transform.rotation, rotation) < .05f);
                Check("Lobby also reveals with droplets", transition.ArrivingFade && transition.ArrivingFade.SceneTransitionControlled);
                stage = 3;
            }
            else if (stage == 3 && !StorybookSceneTransition.IsBusy)
            {
                Time.timeScale = 1;
                if (++frames < 25) return;
                Check("Gravity does not drop the returned eye height", Near(player.viewCamera.transform.position.y - player.FloorHeight, height));
                Check("Returned capsule stays on the lobby ground", Mathf.Abs(player.FloorHeight) < .06f, player.FloorHeight);
                player.AddLookDelta(new Vector2(10, 0));
                Check("Lobby mouse yaw accumulator follows transferred gaze", Mathf.Abs(Mathf.DeltaAngle(player.viewCamera.transform.eulerAngles.y, 49 + 10 * player.lookSensitivity)) < .05f);
                Check("No transition error", string.IsNullOrEmpty(transition.LastError), transition.LastError);
                rotation = player.viewCamera.transform.rotation;
                Check("No-load preview still starts", transition.Preview()); stage = 4;
            }
            else if (stage == 4 && !StorybookSceneTransition.IsBusy)
            {
                Check("No-load preview preserves head height and gaze", Near(player.viewCamera.transform.position.y - player.FloorHeight, height) && Quaternion.Angle(player.viewCamera.transform.rotation, rotation) < .05f);
                Check("No-load preview does not change scene", SceneManager.GetActiveScene().name == "Wait_Robby");
                var effect = Object.FindFirstObjectByType<LobbyDissolveTest>();
                transition.Preview(); transition.Advance(.1f); transition.enabled = false;
                Check("Cancellation releases sphere effect and UI", !StorybookSceneTransition.IsBusy && effect.State == LobbyDissolveTest.FadeState.Visible && transition.View.CardAlpha == 0);
                transition.enabled = true; Finish();
            }
        }
        catch (Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    static void Capture(Camera camera, string filename)
    {
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var rt = new RenderTexture(960, 600, 24); var image = new Texture2D(960, 600, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 960, 600), 0, 0); image.Apply();
            File.WriteAllBytes(Output + filename, image.EncodeToPNG());
        }
        finally { camera.targetTexture = previous; RenderTexture.active = active; Object.Destroy(rt); Object.Destroy(image); }
    }
    static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        Time.timeScale = oldScale;
        Write("runtime-validation.json");
    }
}
