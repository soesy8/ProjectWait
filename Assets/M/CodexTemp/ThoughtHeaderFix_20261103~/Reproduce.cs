using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
public static class ReproduceThoughtHeader
{
    public static object Main()
    {
        var l=Object.FindFirstObjectByType<ElevatorLesson>();
        if(!Application.isPlaying||!l||l.Flow==null)throw new Exception("Play Mode required; never alter Edit Mode state.");
        bool enabled=l.enabled,scenarioEnabled=l.scenario.enabled;var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();bool controlsEnabled=c.enabled;
        try
        {
            l.enabled=l.scenario.enabled=c.enabled=false;l.DeveloperJump(4);l.presentation.space.overlay.Advance(10);l.ReportSituation();l.view.spatial.Tick();l.view.spatial.Transitions.Advance(2);
            l.view.ShowThought("식별판에서 승강기 번호를 확인해요.");l.view.spatial.Tick();
            l.view.ShowThought("");l.view.spatial.Tick();l.view.spatial.Transitions.Advance(2);
            var root=l.view.thoughtPanel;var bubble=root.Find("CurrentThoughtBubble");
            var result=new {success=true,step=l.Flow.Step.ToString(),l.Flow.SituationReported,l.Flow.IdentityFound,
                root.gameObject.activeInHierarchy,body=bubble.Find("Thought").GetComponent<TMP_Text>().text,
                heading=bubble.Find("Heading").GetComponent<TMP_Text>().text,
                headingActive=bubble.Find("Heading").gameObject.activeInHierarchy,
                ribbonActive=bubble.Find("ThoughtRibbon").gameObject.activeInHierarchy};
            File.WriteAllText("Assets/M/CodexTemp/ThoughtHeaderFix_20261103~/ReproducedBefore.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally {l.Restart();l.presentation.space.overlay.Advance(10);l.enabled=enabled;l.scenario.enabled=scenarioEnabled;c.enabled=controlsEnabled;}
    }
}