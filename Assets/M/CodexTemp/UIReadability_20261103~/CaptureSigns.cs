using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

public static class CaptureReadableElevatorSigns
{
    const string Dir="Assets/M/CodexTemp/UIReadability_20261103~/";
    static ElevatorLesson l;static ElevatorDesktopControls c;static ElevatorPlayerMovement m;
    static void Place(Vector3 p,Vector3 target)
    {
        bool enabled=m.Controller.enabled;m.Controller.enabled=false;
        c.xrOrigin.position+=p-l.playerCamera.transform.position;l.playerCamera.transform.position=p;
        m.Controller.enabled=enabled;
        var angle=Quaternion.LookRotation(target-p).eulerAngles;c.SetLookAngles(angle.y,Mathf.DeltaAngle(0,angle.x));
        Physics.SyncTransforms();l.view.spatial.Tick();
    }
    static async Task Shot(string name){await Task.Delay(550);ScreenCapture.CaptureScreenshot(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_"+name+".png");await Task.Delay(150);}
    public static async Task<object> Main()
    {
        l=Object.FindFirstObjectByType<ElevatorLesson>();c=Object.FindFirstObjectByType<ElevatorDesktopControls>();m=c.movement;
        bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled;
        try
        {
            l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;EditorApplication.ExecuteMenuItem("Window/General/Game");
            l.DeveloperJump(1);l.presentation.space.overlay.Advance(10);
            Place(new Vector3(-1.8f,9.2f,5.8f),new Vector3(-.43f,9.30f,4.70f));await Shot("05_SideButtons");
            Place(new Vector3(-2.10f,9.85f,5.0f),new Vector3(-3.01f,9.80f,3.73f));await Shot("06_FrontButtons");
            Place(new Vector3(-1.65f,9.80f,5.25f),new Vector3(-3.18f,9.80f,5.2f));await Shot("07_FacilityGuide");
            l.Restart();l.presentation.space.overlay.Advance(10);
            Place(new Vector3(-4.99f,9.15f,1.2f),new Vector3(-4.99f,9.07f,3.08f));await Shot("08_LobbyGuide");
        }
        finally{l.Restart();l.presentation.space.overlay.Advance(10);l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;}
        return new{success=true};
    }
}