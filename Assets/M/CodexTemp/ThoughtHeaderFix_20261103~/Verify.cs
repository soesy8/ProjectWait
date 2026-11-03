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
using Object=UnityEngine.Object;

public static class VerifyThoughtHeaderFix
{
    const string Dir="Assets/M/CodexTemp/ThoughtHeaderFix_20261103~/";
    static readonly List<object> checks=new List<object>();
    static readonly List<object> samples=new List<object>();
    static ElevatorLesson lesson;
    static ElevatorSpatialUI ui;
    static void Check(bool passed,string name){checks.Add(new{name,passed});if(!passed)throw new Exception(name);}
    static void Settle(){lesson.presentation.space.overlay.Advance(10);ui.Tick();ui.Transitions.Advance(2);Canvas.ForceUpdateCanvases();}
    static void BodyOnly(string name)
    {
        var panel=lesson.view.thoughtPanel;
        Check(panel.GetComponentsInChildren<TMP_Text>(true).Where(ElevatorThoughtHeaderPolicy.IsHeader).All(t=>!t.gameObject.activeSelf),name+": no active original, legacy or ghost title");
        Check(panel.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ThoughtRibbon").All(t=>!t.gameObject.activeSelf),name+": no active title decoration");
        var bubble=panel.Find("CurrentThoughtBubble");var body=bubble?bubble.Find("Thought").GetComponent<TMP_Text>():lesson.view.thought;
        if(!string.IsNullOrEmpty(lesson.view.thought.text))
        {
            Check(body.gameObject.activeSelf&&body.text==lesson.view.thought.text,name+": actual instruction/description is preserved");
            body.ForceMeshUpdate();Check(!body.isTextOverflowing,name+": body still fits");
        }
        samples.Add(new{name,step=lesson.Flow.Step.ToString(),body=body.text,panelActive=panel.gameObject.activeSelf});
    }
    static void SetSpeakerVisible(){while(ui.TryReadDialogue()){}ui.Transitions.Advance(2);}
    public static async Task<object> Main()
    {
        checks.Clear();samples.Clear();lesson=Object.FindFirstObjectByType<ElevatorLesson>();
        if(!Application.isPlaying||!lesson||lesson.Flow==null)throw new Exception("Play Mode required.");
        ui=lesson.view.spatial;var story=ui.storybook;var c=Object.FindFirstObjectByType<ElevatorDesktopControls>();var movement=c.movement;var scenario=lesson.scenario;
        bool le=lesson.enabled,ce=c.enabled,me=movement.enabled,se=scenario.enabled,ve=lesson.view.enabled,guided=scenario.objectGuidedScene;
        var prefab=story.thoughtBubblePrefab;var thoughtRoot=lesson.view.thoughtPanel;var rotation=c.LookAngles;var rigPosition=c.xrOrigin.position;
        GameObject probe=null,ghost=null;string error=null;
        try
        {
            lesson.enabled=c.enabled=movement.enabled=scenario.enabled=lesson.view.enabled=false;
            lesson.Restart();Settle();BodyOnly("Startup");
            lesson.DeveloperJump(1);Settle();BodyOnly("Cabin boarding");lesson.SelectFloor();Settle();BodyOnly("Cabin floor selected");
            lesson.DeveloperJump(3);lesson.Advance(2);Settle();BodyOnly("Stopped investigation");
            lesson.DeveloperJump(4);Settle();lesson.ReportSituation();Settle();
            Check(lesson.Flow.SituationReported&&!lesson.Flow.IdentityFound&&thoughtRoot.gameObject.activeSelf,"Reproduced cabin identity-search state is reached");
            BodyOnly("Identity-search base hint");
            var actualHint=lesson.view.thought.text;
            lesson.view.ShowThought("전면 식별판을 살펴봐요.");Settle();BodyOnly("Search to observation");
            lesson.view.ShowThought("");Settle();Check(lesson.view.thought.text==actualHint,"Hover exit restores the actual mission hint, not a title");BodyOnly("Observation exit to base hint");
            lesson.Advance(lesson.settings.secondHintSeconds+1);Settle();BodyOnly("Delayed search hint revision");
            // Same-content and resize paths must not resurrect any heading or ghost layer.
            var bubble=thoughtRoot.Find("CurrentThoughtBubble");var heading=bubble.Find("Heading").GetComponent<TMP_Text>();var ribbon=bubble.Find("ThoughtRibbon");
            heading.text="나의 생각";heading.gameObject.SetActive(true);ribbon.gameObject.SetActive(true);
            ui.ShowThought("");BodyOnly("Identical empty observation refresh");
            heading.text="나의 생각 · 힌트";heading.gameObject.SetActive(true);ribbon.gameObject.SetActive(true);
            ui.Layout();BodyOnly("Layout after a stale title was re-enabled");
            heading.text="살펴보니…";heading.gameObject.SetActive(true);ribbon.gameObject.SetActive(true);
            ui.Tick();BodyOnly("Tick clears reactivated heading");
            // A previous outgoing copy may have a generic name, not Heading.
            ghost=new GameObject("TransitionText",typeof(RectTransform));ghost.transform.SetParent(bubble,false);
            var ghostText=ghost.AddComponent<TextMeshProUGUI>();ghostText.font=lesson.view.font;ghostText.text="나의 생각";
            ui.ShowThought("같은 설명을 반복해 확인해요.");ui.Transitions.Advance(.025f);BodyOnly("Generic old title copy is hidden during a content fade");
            Object.Destroy(ghost);ghost=null;
            foreach(var clue in new[]{ElevatorClue.Friend,ElevatorClue.Intercom,ElevatorClue.FrontIntercom,ElevatorClue.Identity,ElevatorClue.Phone,ElevatorClue.Notice})
            {
                lesson.Inspect(clue);Settle();BodyOnly("Inspect "+clue);
                lesson.ClearThought(clue);Settle();BodyOnly("Clear "+clue);
            }
            lesson.FindIdentity();Settle();BodyOnly("Identity found");lesson.Report();Settle();BodyOnly("Location reported");
            lesson.Advice();Settle();BodyOnly("Waiting");lesson.Reassure();Settle();BodyOnly("Waiting fade");
            lesson.Advance(4.9f);Settle();BodyOnly("Rescue arrival");lesson.Advance(lesson.settings.rescueSubtitleSeconds+.1f);Settle();BodyOnly("Rescue ready");
            // Exercise both dialogue scenes' content configurations without opening/saving another scene.
            foreach(bool mode in new[]{false,true})
            {
                scenario.objectGuidedScene=mode;lesson.DeveloperJump(4);Settle();lesson.ReportSituation();Settle();
                lesson.view.ShowThought("식별판을 찾아보자.");ui.Transitions.Advance(.025f);BodyOnly("Guided="+mode+" observation fade");
                lesson.view.ShowThought("");Settle();BodyOnly("Guided="+mode+" mission return");
            }
            // The no-prefab fallback must also be body-only, not just the main prefab route.
            story.thoughtBubblePrefab=null;story.LayoutThought(false);
            Check(!ui.thoughtHeading.gameObject.activeSelf,"Fallback mission layout does not enable legacy ThoughtHeading");
            Check(!thoughtRoot.Find("ThoughtRibbon").gameObject.activeSelf,"Fallback mission layout does not create/show a ribbon");
            story.LayoutThought(true);Check(!ui.thoughtHeading.gameObject.activeSelf,"Fallback observation is also title-free");story.thoughtBubblePrefab=prefab;ui.Layout();
            // The no-storybook route is governed by the same spatial policy.
            ui.storybook=null;ui.ShowThought("본문만 남는 기본 설명이에요.");
            Check(!ui.thoughtHeading.gameObject.activeSelf,"No-storybook update cannot re-enable legacy title");ui.storybook=story;ui.Layout();Settle();BodyOnly("Restored prefab");
            // A tracked body that turns into a title must not spawn a generic ghost later.
            probe=new GameObject("LegacyDescription",typeof(RectTransform));probe.transform.SetParent(thoughtRoot,false);
            var probeText=probe.AddComponent<TextMeshProUGUI>();probeText.font=lesson.view.font;probeText.text="이전 내용";
            ui.Transitions.Begin();ui.Transitions.End();ui.Transitions.Advance(2);
            probeText.text="나의 생각 · 힌트";ui.Transitions.Begin();ui.Transitions.End();ui.Transitions.Advance(.025f);
            Check(!probe.gameObject.activeSelf,"Transition tracker retires a previously tracked title instead of cloning it");BodyOnly("Tracked-title cleanup");Object.Destroy(probe);probe=null;
            Check(ElevatorThoughtHeaderPolicy.IsTitle("나의 생각")&&ElevatorThoughtHeaderPolicy.IsTitle("나의 생각 · 힌트")&&ElevatorThoughtHeaderPolicy.IsTitle("살펴보니…"),"Known exact legacy title variants are recognized");
            Check(!ElevatorThoughtHeaderPolicy.IsTitle("친구의 생각을 들어 보자.")&&!ElevatorThoughtHeaderPolicy.IsTitle("나의 생각을 말해 보자."),"Ordinary body sentences mentioning thoughts are not mistaken for headings");
            // Cleanup is scoped to PlayerThought, never all Heading objects in the scene.
            lesson.pauseMenu.Pause();ui.Tick();var pauseHeading=lesson.pauseMenu.canvas.transform.Find("PauseCard/Heading").GetComponent<TMP_Text>();
            Check(pauseHeading.gameObject.activeInHierarchy&&pauseHeading.text=="일시 정지","Pause heading remains present and untouched");lesson.pauseMenu.Resume();
            lesson.DeveloperJump(4);Settle();SetSpeakerVisible();
            Check(ui.speaker.gameObject.activeSelf&&ui.speaker.text.Contains("관리실"),"Manager speaker name is preserved");
            Check(ui.speakerTab.gameObject.activeSelf,"Manager speaker ribbon is not treated as a thought ribbon");
            var tabArt=ui.speakerTab.Find("StorybookArtwork").GetComponent<Image>();Check(tabArt.sprite==story.thoughtRibbon,"Shared thoughtRibbon sprite still renders the manager speaker tab");
            BodyOnly("Speaker unaffected");lesson.ReportSituation();Settle();BodyOnly("Final cabin search hint");
            var anchor=thoughtRoot.anchoredPosition;var scale=thoughtRoot.localScale;ui.Tick();ui.Transitions.Advance(.03f);
            Check(thoughtRoot.anchoredPosition==anchor&&thoughtRoot.localScale==scale,"Header removal does not add UI motion or zoom");
            if(lesson.view.ChoiceButtons.Count>0){int revision=lesson.Flow.Revision;lesson.pauseMenu.Pause();lesson.view.InvokeChoice(0);Check(lesson.Flow.Revision==revision,"Existing pause interaction lock is preserved");lesson.pauseMenu.Resume();}
            await Task.Delay(40);ScreenCapture.CaptureScreenshot(Dir+"Cabin_ThoughtBodyOnly.png");await Task.Delay(120);
        }
        catch(Exception e){error=e.ToString();}
        finally
        {
            if(lesson.IsPaused)lesson.pauseMenu.Resume();if(probe)Object.Destroy(probe);if(ghost)Object.Destroy(ghost);
            ui.storybook=story;story.thoughtBubblePrefab=prefab;scenario.objectGuidedScene=guided;c.ClearTarget();lesson.Restart();Settle();
            lesson.enabled=le;c.enabled=ce;movement.enabled=me;scenario.enabled=se;lesson.view.enabled=ve;
        }
        var result=new {success=error==null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,checks=checks.ToArray(),samples=samples.ToArray(),error,actualHMD=false};
        File.WriteAllText(Dir+"ThoughtHeaderVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}