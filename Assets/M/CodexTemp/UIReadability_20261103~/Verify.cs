using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class VerifyElevatorUIReadability
{
    const string Dir = "Assets/M/CodexTemp/UIReadability_20261103~/";
    static readonly List<object> checks = new List<object>();
    static readonly List<object> samples = new List<object>();
    static void Check(bool pass, string name) { checks.Add(new { name, passed = pass }); }
    static void Settle(ElevatorLesson l)
    {
        l.presentation.space.overlay.Advance(10);
        l.view.spatial.Tick();
        Canvas.ForceUpdateCanvases();
    }
    static void Step(ElevatorLesson l,ElevatorStep step)
    {
        typeof(ElevatorFlow).GetMethod("Enter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(l.Flow,new object[]{step,InteractionPhase.Dialogue});
        l.Advance(0);
    }
    static void Text(TMP_Text t, float size, string name)
    {
        t.ForceMeshUpdate(true, true);
        Check(Mathf.Abs(t.fontSize-size)<.01f && !t.enableAutoSizing,name+": fixed readable size");
        Check((t.fontStyle & FontStyles.Bold)!=0 && t.fontWeight==FontWeight.Bold,name+": bold weight");
        Check(!t.isTextOverflowing,name+": no text overflow");
        var preferred=t.GetPreferredValues(t.text,t.rectTransform.rect.width,0);
        Check(preferred.y <= t.rectTransform.rect.height+2,name+": measured height fits ("+preferred.y+"/"+t.rectTransform.rect.height+")");
        Check(!t.raycastTarget,name+": label does not intercept input");
    }
    static void Inspect(ElevatorLesson l,string name)
    {
        Settle(l);
        foreach(var b in l.view.ChoiceButtons)
        {
            var t=b.GetComponentInChildren<TMP_Text>();
            Text(t,l.Flow.Step==ElevatorStep.Complete?ElevatorTypography.NotebookButtonSize:b.name=="DialogueNext"?ElevatorTypography.NextSize:ElevatorTypography.ChoiceSize,name+" "+b.name);
            Check(((RectTransform)b.transform).rect.height>=82,name+": expanded button height");
            samples.Add(new{name,button=b.name,text=t.text,size=t.fontSize,height=((RectTransform)b.transform).rect.height,lines=t.textInfo.lineCount,overflow=t.isTextOverflowing});
        }
        Text(l.view.spatial.speaker,ElevatorTypography.SpeakerSize,name+" speaker");
        var status=l.view.dialoguePanel.Find("ReadingStatus")?.GetComponent<TMP_Text>();
        if(status&&status.gameObject.activeSelf)Text(status,ElevatorTypography.HintSize,name+" reading status");
        Check(l.view.dialogueText.fontSize==35&&!l.view.dialogueText.isTextOverflowing,name+": existing dialogue readability preserved");
    }
    static async Task Capture(string name)
    {
        await Task.Delay(500);
        ScreenCapture.CaptureScreenshot(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_"+name+".png");
        await Task.Delay(150);
    }
    public static async Task<object> Main()
    {
        checks.Clear();samples.Clear();
        var l=Object.FindFirstObjectByType<ElevatorLesson>();
        if(!Application.isPlaying||!l||l.Flow==null)throw new Exception("Play an M elevator scene first.");
        var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();
        var m=Object.FindFirstObjectByType<ElevatorPlayerMovement>();
        var ui=l.view.spatial;var camera=l.playerCamera;
        bool le=l.enabled,ce=c.enabled,me=m.enabled,se=l.scenario.enabled,originalGuided=l.scenario.objectGuidedScene;
        float aspect=camera.aspect;string error=null;
        l.enabled=c.enabled=m.enabled=l.scenario.enabled=false;
        try
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var signs=l.presentation.space.sportsCentre.geometry.Find("ReadableSignage");
            Check(signs,"Physical button and signage prefab is installed");
            if(signs)
            {
                Check(!signs.GetComponentInChildren<Collider>(true),"Typography surfaces introduce no colliders");
                foreach(var t in signs.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.ForceMeshUpdate(true,true);
                    Check(!t.isTextOverflowing,"Sign "+t.transform.parent.name+": no overflow");
                    Check((t.fontStyle&FontStyles.Bold)!=0&&!t.enableAutoSizing,"Sign "+t.transform.parent.name+": bold without auto-shrink");
                    var p=t.GetPreferredValues(t.text,t.rectTransform.rect.width,0);
                    Check(p.x<=t.rectTransform.rect.width+.002f&&p.y<=t.rectTransform.rect.height+.002f,"Sign "+t.transform.parent.name+": measured fit ("+p+"/"+t.rectTransform.rect.size+")");
                }
            }
            l.Restart();Inspect(l,"Lobby");
            await Capture("01_Buttons");
            for(int i=0;i<7;i++){l.DeveloperJump(i);Inspect(l,"Stage "+i);}
            l.DeveloperJump(1);l.SelectFloor();Inspect(l,"Floor selected");
            l.DeveloperJump(4);l.ReportSituation();Inspect(l,"Identity report");
            Step(l,ElevatorStep.Complete);Inspect(l,"Complete");
            foreach(var t in ui.notebook.GetComponentsInChildren<TMP_Text>(true))
            {
                if(t.name=="Label")continue;
                t.ForceMeshUpdate(true,true);Check(!t.isTextOverflowing,"Notebook "+t.name+": no overflow");
                var p=t.GetPreferredValues(t.text,t.rectTransform.rect.width,0);
                Check(p.y<=t.rectTransform.rect.height+2,"Notebook "+t.name+": height fits ("+p.y+"/"+t.rectTransform.rect.height+")");
            }
            await Capture("02_Notebook");
            l.Restart();Settle(l);l.pauseMenu.Pause();
            foreach(var b in l.pauseMenu.Buttons)Text(b.GetComponentInChildren<TMP_Text>(true),ElevatorTypography.PauseButtonSize,"Pause "+b.name);
            Text(l.pauseMenu.canvas.transform.Find("PauseCard/Heading").GetComponent<TMP_Text>(),40,"Pause heading");
            await Capture("03_Pause");l.pauseMenu.Resume();
            l.view.ShowThought("휴대폰이에요. 비상통화에 응답이 없을 때 119 모의 신고에 사용할 수 있어요.");ui.Tick();
            var bubble=l.view.thoughtPanel.Find("CurrentThoughtBubble");var body=bubble.Find("Thought").GetComponent<TMP_Text>();
            Text(body,ElevatorTypography.ThoughtSize/bubble.localScale.x,"Observation body");
            Check(body.font==l.view.font,"Observation uses M font rather than mutating shared fallback atlas");
            Check(!bubble.Find("Heading").gameObject.activeSelf&&!bubble.Find("ThoughtRibbon").gameObject.activeSelf,"Observation headings remain hidden");
            Check(Mathf.Abs(body.fontSize*bubble.localScale.x-ElevatorTypography.ThoughtSize)<.01f,"Observation retains 27pt after prefab scaling");
            await Capture("04_Hint");
            foreach(float a in new[]{16f/9,4f/3,9f/16})
            {
                camera.aspect=a;ui.Layout();Inspect(l,"Aspect "+a);
                Text(l.view.title,ElevatorTypography.HeadingSize,"HUD title "+a);
                Text(l.view.subtitle,ElevatorTypography.ObjectiveSize,"HUD objective "+a);
                Text(ui.controls,ElevatorTypography.ControlsSize,"HUD controls "+a);
            }
            camera.aspect=aspect;ui.Layout();
            Step(l,ElevatorStep.Entering);Settle(l);
            var caption=l.presentation.space.overlay.GetComponentInChildren<TMP_Text>(true);
            Text(caption,ElevatorTypography.CaptionSize,"Cinematic caption");
            // Newly created long choices must grow, not shrink or truncate.
            l.Restart();bool objectGuided=l.scenario.objectGuidedScene;
            l.scenario.objectGuidedScene=false;l.Refresh();var label=l.view.ChoiceButtons[0].GetComponentInChildren<TMP_Text>();
            label.text="문을 억지로 열지 않고 친구와 함께 안전한 곳에서 직원의 안내를 기다릴래요";
            ui.Layout();Text(label,ElevatorTypography.ChoiceSize,"Long choice");
            Check(((RectTransform)label.transform.parent).rect.height>104,"Long choice grows button height");
            l.scenario.objectGuidedScene=objectGuided;
        }
        catch(Exception e){error=e.ToString();}
        finally
        {
            camera.aspect=aspect;l.scenario.objectGuidedScene=originalGuided;if(l.IsPaused)l.pauseMenu.Resume();l.Restart();Settle(l);
            l.enabled=le;c.enabled=ce;m.enabled=me;l.scenario.enabled=se;
        }
        int failed=checks.Count(v=>!(bool)v.GetType().GetProperty("passed").GetValue(v));
        var result=new{success=error==null&&failed==0,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,count=checks.Count,failed,checks,samples,error,HMDTested=false};
        File.WriteAllText(Dir+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"_Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return new{result.success,result.count,result.failed,result.error};
    }
}