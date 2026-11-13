#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using ProjectWait.M.TitleScene;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;

namespace ProjectWait.M.EditorTools
{
    // Opt-in, read-only runtime inspection. Remove enabled.txt to stop; never saves a scene or settings.
    [InitializeOnLoad]
    public static class LobbyVRInputDiagnostics
    {
        const string Folder="Assets/M/CodexTemp/LobbyVRInput_20261113~/";
        static double next;
        static LobbyVRInputDiagnostics(){Application.onBeforeRender+=Tick;}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||InputState.currentUpdateType==InputUpdateType.Editor||EditorApplication.timeSinceStartup<next||!File.Exists(Folder+"enabled.txt"))return;
            next=EditorApplication.timeSinceStartup+1;
            try{Capture();}catch(Exception ex){File.WriteAllText(Folder+"snapshot.txt",ex.ToString());}
        }
        static void Capture()
        {
            var text=new StringBuilder();
            text.AppendLine(DateTime.Now.ToString("O"));
            text.AppendLine("XR active="+XRSettings.isDeviceActive+" device="+XRSettings.loadedDeviceName+" appFocus="+Application.isFocused+" update="+InputState.currentUpdateType);
            var player=UnityEngine.Object.FindFirstObjectByType<TitlePlazaPlayer>();
            if(player)
            {
                text.AppendLine("scene="+player.gameObject.scene.path+" VR="+player.IsVR+" frames="+player.TickedFrames+" hover="+player.HoveredExperience);
                text.AppendLine("transition="+ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy+" aimValid="+player.TryGetAimRay(out var ray)+" ray="+ray);
                var line=player.controllerRay;
                text.AppendLine("line="+(line?line.name:"missing")+" active="+(line&&line.gameObject.activeInHierarchy)+" enabled="+(line&&line.enabled)+" material="+(line&&line.sharedMaterial?line.sharedMaterial.name:"missing"));
                if(line&&line.positionCount>=2)text.AppendLine("line start="+line.GetPosition(0)+" end="+line.GetPosition(1));
                Action(text,player,"aimPose");Action(text,player,"selectTrigger");
            }
            foreach(var device in InputSystem.devices)
            {
                if(!(device is TrackedDevice))continue;
                text.AppendLine("DEVICE "+device.path+" layout="+device.layout+" usages="+string.Join(",",device.usages)+" added="+device.added+" enabled="+device.enabled);
                var pointer=device.TryGetChildControl<PoseControl>("pointer");
                if(pointer!=null)
                {
                    var pose=pointer.ReadValue();
                    text.AppendLine(" pointer="+pointer.path+" tracked="+pose.isTracked+" flags="+pose.trackingState+" p="+pose.position+" q="+pose.rotation);
                    text.AppendLine(" pointer child tracked="+pointer.isTracked.ReadValue()+" pressed="+pointer.isTracked.isPressed+" flags="+pointer.trackingState.ReadValue()+" format="+pointer.isTracked.stateBlock.format+" bits="+pointer.isTracked.stateBlock.sizeInBits);
                }
                foreach(var name in new[]{"isTracked","trackingState","devicePosition","deviceRotation","pointerPosition","pointerRotation","triggerPressed","trigger"})
                {
                    var control=device.TryGetChildControl<InputControl>(name);
                    if(control!=null)text.AppendLine(" "+name+"="+control.ReadValueAsObject());
                }
            }
            File.WriteAllText(Folder+"snapshot.txt",text.ToString());
        }
        static void Action(StringBuilder text,TitlePlazaPlayer player,string name)
        {
            var action=typeof(TitlePlazaPlayer).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(player) as InputAction;
            if(action==null){text.AppendLine(name+" missing");return;}
            text.AppendLine(name+" enabled="+action.enabled+" phase="+action.phase+" active="+action.activeControl?.path+" controls="+action.controls.Count);
            foreach(var control in action.controls)text.AppendLine(" bound="+control.path+" type="+control.valueType+" value="+control.ReadValueAsObject());
        }
    }
}
#endif
