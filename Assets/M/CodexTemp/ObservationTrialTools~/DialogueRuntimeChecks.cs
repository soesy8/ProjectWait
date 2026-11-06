using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProjectWait.M.Elevator.ObservationTrial;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DialogueRuntimeChecks
{
    const string Evidence="Assets/M/Documentation/ObservationTrial_Dialogue_20261106/";
    static readonly List<string> checks=new List<string>();
    static ElevatorLesson lesson;
    static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks.Add(text);}
    static string Plain(string text)=>string.Concat((text??"").Where(c=>!char.IsWhiteSpace(c)));
    static async Task Frames(int count=8){for(int i=0;i<count;i++){EditorApplication.QueuePlayerLoopUpdate();await Task.Delay(30);}}
    static void Save(string name,object value){Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+name+".json",JsonConvert.SerializeObject(value,Formatting.Indented));}
    static void Card(string expected,string stage)
    {
        Check(Plain(lesson.view.dialogueText.text)==Plain(expected),stage+": actual card uses the revised line");
        lesson.view.dialogueText.ForceMeshUpdate();
        Check(!lesson.view.dialogueText.isTextOverflowing,stage+": card text fits without auto-shrinking");
    }
    public static async Task<object> Run()
    {
        lesson=Object.FindFirstObjectByType<ElevatorLesson>();
        if(!Application.isPlaying||!lesson||lesson.gameObject.scene.path!=ProjectWait.M.Editor.ElevatorObservationTrialSetup.ScenePath)throw new Exception("Play the isolated trial scene first.");
        checks.Clear();lesson.enabled=false;TMP_Text measure=null;
        try
        {
            var overlay=lesson.presentation.space.overlay;
            var caption=overlay.GetComponentInChildren<TMP_Text>(true);
            measure=Object.Instantiate(caption,caption.transform.parent,false);measure.name="DialogueCopyMeasure";measure.gameObject.SetActive(false);
            measure.fontSize=ElevatorTypography.CaptionSize;measure.rectTransform.sizeDelta=new Vector2(912,82);
            var entries=new List<object>();
            foreach(var field in typeof(ElevatorDialogue).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.IsLiteral&&f.FieldType==typeof(string)))
            {
                string line=(string)field.GetRawConstantValue();
                Check(!new[]{"선택","클릭","카드"}.Any(line.Contains),field.Name+": no interface instructions in spoken/lesson copy");
                measure.text=ElevatorDialogue.Caption("소방 구조대원",line);measure.ForceMeshUpdate(true);
                float height=measure.GetPreferredValues(measure.text,912,0).y;
                // Retry/notice text is instructional and never a cinematic subtitle.
                bool spoken=field.Name!="RetryDoor"&&field.Name!="RetryExit"&&field.Name!="SafetyNotice";
                if(spoken)Check(height<=82.1f,field.Name+": caption fits the existing two-line strip at 4:3");
                entries.Add(new{name=field.Name,text=line,captionHeight=height,spoken});
            }
            Save("DialogueCopy",entries);
            Check(ElevatorDialogue.WithoutShortcut("1   1층 누르기")=="1층 누르기"&&
                ElevatorDialogue.WithoutShortcut("1   119에 연락하기 · 체험")=="119에 연락하기 · 체험","action labels preserve floor and emergency numbers when removing keyboard shortcuts");
            Check(lesson.settings.reportDialogue==ElevatorDialogue.SituationReport&&lesson.settings.handoffDialogue==ElevatorDialogue.Handoff&&
                lesson.settings.waitDialogue==ElevatorDialogue.Reassure,"saved isolated settings use the revised speech");

            foreach(bool phone in new[]{false,true})
            {
                lesson.simulateNoResponse=phone;lesson.DeveloperJump(4);lesson.Flow.NoResponse=phone;
                // Developer stage 4 represents a connected intercom; start the phone
                // route through its actual unanswered state when testing caller tags.
                if(phone)
                {
                    lesson.DeveloperJump(3);lesson.Advance(2);lesson.Intercom();lesson.Advance(lesson.settings.connectionSeconds+.01f);lesson.Phone();
                }
                await Frames();Card(ElevatorDialogue.Greeting(phone),phone?"119 greeting":"intercom greeting");
                overlay.Advance(.5f);
                Check(caption.text.Contains(ElevatorDialogue.Caller(phone)),"caller tag matches the selected call route");
                lesson.Advance(lesson.settings.callQuestionSeconds+.01f);await Frames();Card(ElevatorDialogue.SituationReport,"situation report");
                overlay.Advance(.5f);Check(Plain(caption.text).Contains(Plain(ElevatorDialogue.SituationReport)),"caption and situation card agree");
                lesson.Advance(lesson.settings.callReplySeconds+.01f);await Frames();Card(ElevatorDialogue.LocationQuestion,"location question");
                lesson.Advance(lesson.settings.callQuestionSeconds+.01f);await Frames();Card(ElevatorDialogue.IdentityGuide,"identity directions");
                lesson.Continue();await Frames();lesson.FindIdentity();await Frames();
                Check(lesson.view.ChoiceButtons[0].GetComponentInChildren<TMP_Text>().text=="위치 알려주기","identity action uses a plain, specific label");
                lesson.Report();await Frames();Card(ElevatorDialogue.LocationReport(lesson.settings),"spoken building and identifier");
                Check(lesson.view.dialogueText.text.Contains("HB-EL-01")&&!lesson.view.dialogueText.text.Contains("01에 있어요"),"identifier is reported rather than described as a location");
                lesson.Advance(lesson.settings.locationReportSeconds+.01f);await Frames();Card(ElevatorDialogue.SafetyAdvice,"call safety advice");
            }
            lesson.simulateNoResponse=false;lesson.DeveloperJump(5);await Frames();
            lesson.CollectObservations().First(o=>o.clue==ElevatorClue.Friend&&o.gameObject.activeInHierarchy).Activate();await Frames();
            Card(ElevatorDialogue.FriendQuestion,"friend question");
            Check(lesson.view.ChoiceButtons[0].GetComponentInChildren<TMP_Text>().text==ElevatorDialogue.SafeReply&&
                lesson.view.ChoiceButtons[1].GetComponentInChildren<TMP_Text>().text==ElevatorDialogue.UnsafeReply,"replies read as things to say, not procedural instructions");
            lesson.ChooseSafeWall();await Frames();Card(ElevatorDialogue.DoorWarning,"player's safe reply performance");
            lesson.Advance(lesson.settings.doorWarningSeconds+.01f);await Frames();Card(ElevatorDialogue.FriendMoving,"friend acknowledgement");
            lesson.Advance(lesson.settings.safeWallMoveSeconds+.01f);await Frames();Card(ElevatorDialogue.FriendSettled,"friend settles");
            lesson.DeveloperJump(6);await Frames();Card(ElevatorDialogue.RescueArrival,"rescue arrival");
            lesson.Advance(2);await Frames();Card(ElevatorDialogue.RescueChecking,"rescue check");
            lesson.Advance(lesson.settings.rescueSubtitleSeconds);await Frames();Card(ElevatorDialogue.RescueReady,"rescue clearance");
            lesson.Continue();lesson.Advance(lesson.settings.exitSeconds+.01f);await Frames();Card(ElevatorDialogue.Stairs,"escorted stairs");
            var result=new{status="PASS",count=checks.Count,checks,limits="Automated wording consistency and layout checks; naturalness is editorial judgment, not a human usability result."};
            Save("DialogueValidation",result);return result;
        }
        catch(Exception e){Save("DialogueValidation",new{status="FAIL",error=e.ToString(),checks,step=lesson.Flow.Step.ToString()});throw;}
        finally{if(measure)Object.DestroyImmediate(measure.gameObject);lesson.simulateNoResponse=false;lesson.Restart();lesson.enabled=true;}
    }
}
