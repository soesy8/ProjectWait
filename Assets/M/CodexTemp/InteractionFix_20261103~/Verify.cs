using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class VerifyInteractionFix
{
    const string DirectoryPath = "Assets/M/CodexTemp/InteractionFix_20261103~/";
    static readonly List<object> checks = new List<object>();
    static void Check(bool pass, string name)
    {
        checks.Add(new { name, passed = pass });
        if (!pass) throw new Exception(name);
    }
    static async Task Settle(ElevatorLesson lesson)
    {
        lesson.presentation.space.overlay.Advance(10);
        for (int i = 0; i < 120 && lesson.CinematicBlocking; i++)
        {
            lesson.presentation.space.overlay.Advance(10);
            await Task.Delay(50);
        }
        Check(!lesson.CinematicBlocking, "Story transitions settle before interaction checks");
    }
    static void Aim(ElevatorDesktopControls controls, Vector3 position)
    {
        var angles = Quaternion.LookRotation(position - controls.targetCamera.transform.position).eulerAngles;
        controls.SetLookAngles(angles.y, Mathf.DeltaAngle(0, angles.x));
        Physics.SyncTransforms();
        typeof(ElevatorDesktopControls).GetMethod("UpdateCenterRay", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controls, null);
    }
    static void PlaceCamera(ElevatorDesktopControls controls, Vector3 position)
    {
        controls.xrOrigin.position += position - controls.targetCamera.transform.position;
        controls.targetCamera.transform.position = position;
        Physics.SyncTransforms();
    }
    public static async Task<object> Main()
    {
        checks.Clear();
        var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
        if (!Application.isPlaying || !lesson || lesson.Flow == null) throw new Exception("Enter Play Mode first.");
        var controls = Object.FindFirstObjectByType<ElevatorDesktopControls>();
        var movement = controls.movement;
        bool controlsEnabled = controls.enabled, movementEnabled = movement.enabled;
        GameObject probe = null, blocker = null;
        string error = null;
        try
        {
            controls.enabled = false;movement.enabled = false;
            lesson.Restart();await Settle(lesson);
            var observations = lesson.CollectObservations();
            var friend = observations.First(o => o.clue == ElevatorClue.Friend);
            var ball = observations.First(o => o.clue == ElevatorClue.Ball);
            var ballRenderers = ball.GetComponentsInChildren<Renderer>(true);
            Check(friend.outlineTarget != ball.outlineTarget, "Friend and ball have separate outline targets");
            Check(!friend.outlineTarget.geometry.Any(g => g.sourceRenderer && ballRenderers.Contains(g.sourceRenderer)), "Friend silhouette excludes every basketball renderer");
            Check(ball.outlineTarget.geometry.Any(g => g.sourceRenderer && ballRenderers.Contains(g.sourceRenderer)), "Ball retains its own silhouette geometry");
            Check(observations.Where(o => o.outlineTarget && o.hasDescription).All(o => o.outlineTarget.geometry.Length > 0), "All described targets retain non-empty outline geometry");
            var friendXR = friend.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var ballXR = ball.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            Check(!friendXR.colliders.Intersect(ballXR.colliders).Any(), "XR colliders are not shared by friend and ball");
            PlaceCamera(controls, friend.transform.position + new Vector3(0,1.1f,-2.5f));
            Aim(controls,ball.transform.position);
            Check(controls.HoverTargetName==ball.name&&!friend.IsHovered&&ball.IsHovered, "Aiming at the held ball selects only basketball, not its parent friend");
            Aim(controls,friend.transform.position+Vector3.up*1.25f);
            Check(controls.HoverTargetName==friend.name&&friend.IsHovered&&!ball.IsHovered, "Moving aim to friend exits basketball hover independently");
            controls.ClearTarget();friend.Hover(false);ball.Hover(false);await Task.Delay(400);
            Check(friend.outlineTarget.Visible && ball.outlineTarget.Visible, "Both objects keep independent idle highlights");
            Check(Vector4.Distance(friend.outlineTarget.Colour, new Color(1,.70f,.28f,1)) < .01f, "Nearby unhovered friend stays orange");
            friend.Hover(true);await Task.Delay(35);
            Check(friend.HoverBlend > 0 && friend.HoverBlend < 1, "Hover enters through intermediate Lerp values");
            await Task.Delay(450);
            Check(Vector4.Distance(friend.outlineTarget.Colour, new Color(.32f,.84f,1,1)) < .015f, "Hovered friend smoothly reaches blue");
            Check(!ball.IsHovered && ball.HoverBlend < .01f, "Hovering friend does not turn basketball blue");
            friend.Hover(false);ball.Hover(true);await Task.Delay(35);
            Check(friend.HoverBlend > 0 && friend.HoverBlend < 1, "Hover exit also interpolates instead of snapping");
            await Task.Delay(450);
            Check(friend.HoverBlend < .01f && ball.HoverBlend > .99f, "Switching hover returns only friend to orange");
            ball.Hover(false);await Task.Delay(400);
            var bubble = lesson.view.thoughtPanel.Find("CurrentThoughtBubble") as RectTransform;
            Check(bubble && lesson.view.spatial.storybook.thoughtBubblePrefab, "Thought HUD instantiates the current ThoughtCloud prefab");
            Check(bubble.Find("Artwork") && bubble.Find("ThoughtRibbon") && bubble.Find("Heading"), "Current bubble retains prefab artwork and title ribbon");
            Check(bubble.GetComponentsInChildren<Image>(true).Length == 2 && lesson.view.thoughtPanel.Find("StorybookArtwork").gameObject.activeSelf == false, "Legacy artwork is hidden instead of overlapping the new bubble");
            lesson.scenario.enabled=false;
            lesson.view.spatial.ShowThought("친구와 농구공을 따로 살펴볼 수 있어요.\n멀리서도 선택할 수 있어요.");
            await Task.Delay(100);
            var body = bubble.Find("Thought").GetComponent<TMP_Text>();
            Check(body.text == lesson.view.thought.text && !body.raycastTarget, "Current Korean description is copied into non-blocking prefab text");
            body.ForceMeshUpdate();
            Check(!body.isTextOverflowing, "Korean description fits inside the current thought bubble");
            lesson.scenario.enabled=true;
            // A real collider beyond both old limits: 2 m activation and 12 m desktop ray.
            probe = GameObject.CreatePrimitive(PrimitiveType.Cube);probe.name = "QA_DistantObservation";
            probe.transform.position = lesson.playerCamera.transform.position + Vector3.up * 60 + Vector3.forward * 40;
            var distant = probe.AddComponent<ElevatorObservation>();distant.lesson=lesson;distant.clue=ElevatorClue.Notice;
            controls.xrOrigin.position = probe.transform.position - Vector3.forward * 20 - Vector3.up * controls.eyeHeight;
            lesson.playerCamera.transform.position = probe.transform.position - Vector3.forward * 20;
            Physics.SyncTransforms();
            Check(distant.DistanceToPlayer > 12 && distant.CanActivate, "Observation is activatable beyond both previous distance limits");
            Check(distant.Process(null,null), "XR select filter permits distant activation");
            Aim(controls, probe.transform.position);
            Check(controls.HoverTargetName == probe.name, "Desktop centre ray selects an object more than 12 m away");
            controls.SelectCenterTarget();
            Check(!string.IsNullOrEmpty(lesson.Thought), "Centre click activates the distant object");
            blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="QA_Occluder";
            blocker.transform.position=Vector3.Lerp(lesson.playerCamera.transform.position,probe.transform.position,.5f);
            blocker.transform.localScale=Vector3.one*2;Aim(controls,probe.transform.position);
            Check(string.IsNullOrEmpty(controls.HoverTargetName), "Removing range limits does not allow selection through walls");
            Object.Destroy(blocker);blocker=null;Object.Destroy(probe);probe=null;controls.ClearTarget();
            lesson.DeveloperJump(1);await Settle(lesson);lesson.SelectFloor();await Settle(lesson);
            Check(!lesson.Flow.PlayerHasBall, "Ball transfer starts unconsumed");
            var camera = lesson.playerCamera.transform;
            controls.xrOrigin.position += Vector3.right * 20;camera.position += Vector3.right * 20;Physics.SyncTransforms();
            Check(ball.DistanceToPlayer > 12 && ball.CanActivate, "Real story basketball is activatable from over 12 m away");
            ball.Activate();
            Check(lesson.Flow.PlayerHasBall, "Distant basketball activation performs the actual story handoff");
            lesson.Restart();await Settle(lesson);
            Check(!lesson.Flow.PlayerHasBall && lesson.Flow.Step == ElevatorStep.Lobby, "Restart restores the original story state");
            PlaceCamera(controls, friend.transform.position + new Vector3(0,1.1f,-2.5f));
            Aim(controls,friend.transform.position+Vector3.up*1.1f);await Task.Delay(500);
            ScreenCapture.CaptureScreenshot(DirectoryPath + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_ThoughtAndHover.png");
            await Task.Delay(300);
            Aim(controls,ball.transform.position);await Task.Delay(500);
            ScreenCapture.CaptureScreenshot(DirectoryPath + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_BallHover.png");
            await Task.Delay(300);
        }
        catch(Exception exception) { error=exception.ToString(); }
        finally
        {
            if(blocker)Object.Destroy(blocker);if(probe)Object.Destroy(probe);
            lesson.scenario.enabled=true;
            controls.ClearTarget();lesson.Restart();movement.enabled=movementEnabled;controls.enabled=controlsEnabled;
        }
        var result = new { success=error==null, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, checks=checks.ToArray(), error, actualHMD=false };
        File.WriteAllText(DirectoryPath + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_Verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}