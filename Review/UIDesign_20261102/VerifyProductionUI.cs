using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class VerifyProductionUI
{
    const string Root="Review/UIDesign_20261102/";
    static List<object> checks=new List<object>();
    static ElevatorLesson L=>Object.FindFirstObjectByType<ElevatorLesson>();
    public static async Task<object> FinalPreview()
    {
        checks.Clear();L.DeveloperJump(2);
        await Wait(()=>L.Flow.Step==ElevatorStep.Proposal&&!L.CinematicBlocking,"Final proposal view ready");await Settle(1200);TextFits();
        var heading=L.view.spatial.thoughtHeading;
        heading.ForceMeshUpdate();Check("Observation heading fits nameplate",heading.preferredWidth<=heading.rectTransform.rect.width+1);
        await Shot("Production_Choices");
        L.pauseMenu.Toggle();await Settle(150);await Shot("Production_Pause");await Click(L.pauseMenu.resumeButton);
        File.WriteAllText(Root+"cartoon-final-preview.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
        return checks.ToArray();
    }
    public static async Task<object> Gallery()
    {
        Canvas.ForceUpdateCanvases();
        var texts=Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        foreach(var t in texts)t.ForceMeshUpdate();
        var issues=texts.Where(t=>t.preferredHeight>t.rectTransform.rect.height+2).Select(t=>new {t.name,t.text,needed=t.preferredHeight,available=t.rectTransform.rect.height}).ToArray();
        var report=new {textCount=texts.Length,issues};
        File.WriteAllText(Root+"cartoon-gallery-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        await Shot("Production_Gallery");return report;
    }
    static void Check(string name,bool pass,object detail=null)
    { checks.Add(new {name,pass,detail}); if(!pass)throw new Exception(name); }
    static async Task Wait(Func<bool> condition,string name,int seconds=20)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(!condition()&&Time.realtimeSinceStartup<end)await Task.Delay(40);
        Check(name,condition());
    }
    static async Task Settle(int ms=900){await Task.Delay(ms);}
    static async Task Shot(string name)
    {
        Canvas.ForceUpdateCanvases();await Awaitable.EndOfFrameAsync();
        ScreenCapture.CaptureScreenshot(Root+name.Replace("Production_","Cartoon_")+".png");await Task.Delay(120);
    }
    static void TextFits()
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in new[]{L.view.dialogueText}.Concat(L.view.ChoiceButtons.Select(b=>b.GetComponentInChildren<TMP_Text>())))
        {
            t.ForceMeshUpdate();
            Check("Text fits: "+t.name,t.preferredHeight<=t.rectTransform.rect.height+2,new {text=t.text,needed=t.preferredHeight,available=t.rectTransform.rect.height});
        }
    }
    static async Task Click(UnityEngine.UI.Button button)
    {
        Check("Button is active and enabled: "+button.name,button.IsActive()&&button.IsInteractable());
        Canvas.ForceUpdateCanvases();
        var r=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:L.playerCamera;
        var pos=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=pos,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        var hit=hits.FirstOrDefault(x=>x.gameObject.GetComponentInParent<UnityEngine.UI.Button>()==button);
        Check("Image raycast reaches button: "+button.name,hit.gameObject!=null,new {x=pos.x,y=pos.y,hits=hits.Take(5).Select(x=>x.gameObject.name).ToArray()});
        ExecuteEvents.ExecuteHierarchy(hit.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        var feedback=button.GetComponent<StorybookButtonFeedback>();
        Check("Button has tactile feedback",feedback!=null);
        ExecuteEvents.ExecuteHierarchy(hit.gameObject,pointer,ExecuteEvents.pointerDownHandler);
        var image=(UnityEngine.UI.Image)button.targetGraphic;
        Check("Press visibly depresses artwork",image.rectTransform.anchoredPosition.y<0);
        ExecuteEvents.ExecuteHierarchy(hit.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hit.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        await Settle(150);
    }
    static async Task Choice(int index)
    {
        await Wait(()=>!L.CinematicBlocking&&L.view.ChoiceButtons.Count>index&&L.view.ChoiceButtons[index].IsActive(),"Choice ready: "+index);
        await Click(L.view.ChoiceButtons[index]);
    }
    public static async Task<object> Main(string part)
    {
        checks.Clear();
        try
        {
            Check("Play Mode active",Application.isPlaying);
            int frame=Time.frameCount;await Settle(180);Check("Player loop advances",Time.frameCount>frame);
            Check("Generated image theme is connected",L.view.spatial.storybook!=null);
            if(part=="choices")
            {
                await Choice(0);await Wait(()=>L.Flow.Step==ElevatorStep.Boarding,"Boarding reached");await Settle();
                L.Activate(ElevatorClue.FloorButtons);await Settle(200);
                await Choice(0);await Wait(()=>L.Flow.Step==ElevatorStep.Proposal,"Proposal reached");await Settle(1200);
                TextFits();await Shot("Production_Choices");
                L.pauseMenu.Toggle();await Settle(150);
                float elapsed=L.Flow.Elapsed;await Settle(180);Check("Pause freezes scenario",L.IsPaused&&Mathf.Abs(L.Flow.Elapsed-elapsed)<.001f);
                await Shot("Production_Pause");await Click(L.pauseMenu.resumeButton);Check("Resume button continues game",!L.IsPaused);
            }
            else if(part=="call")
            {
                await Choice(0);await Wait(()=>L.Flow.Step==ElevatorStep.Stopped&&L.Flow.CanObserve&&!L.CinematicBlocking,"Investigation ready");
                L.Activate(ElevatorClue.Intercom);await Wait(()=>L.Flow.Step==ElevatorStep.Call,"Call connected");await Settle(1100);
                TextFits();await Shot("Production_Call");await Choice(0);Check("Next reports situation",L.Flow.SituationReported);
                // Arrange the discovered clue; this review exercises UI, not the 2m world-object range.
                L.FindIdentity();await Settle(1000);
                Check("Identity discovered",L.Flow.IdentityFound);await Choice(0);Check("Next reports location",L.Flow.LocationReported);
                await Choice(0);await Wait(()=>L.Flow.Step==ElevatorStep.Waiting,"Waiting reached");await Settle(1000);
                TextFits();await Shot("Production_Waiting");
            }
            else if(part=="complete")
            {
                L.Reassure();await Wait(()=>L.Flow.Step==ElevatorStep.Rescue&&L.Flow.ReadyToExit,"Rescue ready",25);
                await Choice(0);await Wait(()=>L.Flow.Step==ElevatorStep.ReturnLobby,"Return lobby reached");await Choice(0);
                await Wait(()=>L.Flow.Step==ElevatorStep.GroundFloor,"Ground floor reached");L.Continue();
                await Wait(()=>L.Flow.Step==ElevatorStep.Complete,"Completed route");await Settle(1000);
                Check("Narrative outcomes preserved",L.Flow.HelpRequested&&L.Flow.LocationReported&&L.Flow.Reassured);
                TextFits();await Shot("Production_Complete");
            }
            else if(part=="guided")
            {
                Check("Object-guided scene preserved",L.scenario.objectGuidedScene);
                // Arrange UI states through the project's editor stage controls.
                L.DeveloperJump(2);
                await Wait(()=>L.Flow.Step==ElevatorStep.Proposal,"Guided proposal reached");await Settle(1000);TextFits();
                Check("Guided scene keeps object selection",L.view.ChoiceButtons.Count==0);
                await Shot("Production_Guided");L.DeveloperJump(4);
                await Wait(()=>L.Flow.Step==ElevatorStep.Call,"Guided call view ready");
                await Settle(1100);TextFits();await Choice(0);Check("Guided Next reports situation",L.Flow.SituationReported);
            }
            else if(part=="final")
            {
                Check("Completion view active",L.Flow.Step==ElevatorStep.Complete);
                TextFits();await Shot("Production_Complete");await Choice(0);
                var row=L.view.spatial.completionRows[0].Find("StorybookArtwork").GetComponent<UnityEngine.UI.Image>();
                Check("History selection updates artwork",row.color!=Color.white);
            }
            return new {part,checks=checks.ToArray()};
        }
        finally
        {
            File.WriteAllText(Root+"verify-cartoon-"+part+".json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
        }
    }
}
