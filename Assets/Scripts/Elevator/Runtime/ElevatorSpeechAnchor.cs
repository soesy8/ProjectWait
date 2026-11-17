using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // One existing bubble/choice column, connected to the current actor or emitter.
    // Only source translation moves it. Turning the head cannot drag it into view.
    public sealed class ElevatorSpeechAnchor
    {
        public enum Voice { Friend, Player, Intercom, Phone, Firefighter, Specialist, Object }
        readonly Dictionary<ElevatorClue,Transform> objects=new Dictionary<ElevatorClue,Transform>();
        ElevatorLesson owner;
        Transform source;
        Vector3 voiceOffset,side;
        float lift;
        int run=-1;
        ElevatorStep step=(ElevatorStep)(-1);
        ElevatorClue? clue;
        string speaker;
        bool phone;
        public Voice CurrentVoice {get;private set;}
        public Transform Source => source;
        public Vector3 SourcePoint {get;private set;}

        public static string VisibleLine(ElevatorLesson lesson,string who,string line)
        {
            if(who=="안내"||lesson.Flow.Step==ElevatorStep.StartupFade)return "";
            if(lesson.Flow.Step==ElevatorStep.GymDeparture&&lesson.Flow.Elapsed>=5)return "";
            if(lesson.Flow.Step==ElevatorStep.Jump&&lesson.Flow.Elapsed>=lesson.settings.friendReplySeconds)return "";
            return line??"";
        }

        public void Reset(){owner=null;source=null;objects.Clear();run=-1;step=(ElevatorStep)(-1);speaker=null;}

        public Pose Get(ElevatorLesson lesson,string who,Bounds column,float scale)
        {
            var f=lesson.Flow;
            if(owner!=lesson||run!=lesson.RunVersion||step!=f.Step||speaker!=who||phone!=f.UsedPhone||clue!=f.PreviewClue||!source)
            {
                if(owner!=lesson||run!=lesson.RunVersion)
                {
                    objects.Clear();
                    foreach(var item in lesson.CollectObservations())if(item&&!objects.ContainsKey(item.clue))objects.Add(item.clue,item.transform);
                }
                owner=lesson;run=lesson.RunVersion;step=f.Step;speaker=who;phone=f.UsedPhone;clue=f.PreviewClue;
                Resolve(lesson,who);
                var eye=lesson.playerCamera.transform;
                var forward=Vector3.ProjectOnPlane(SourcePoint-eye.position,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up).normalized;
                var right=Vector3.Cross(Vector3.up,forward);
                side=right*(Vector3.Dot(SourcePoint-eye.position,eye.right)>.1f?-1:1);
            }
            SourcePoint=source?source.position+voiceOffset:SourcePoint;
            // Tail direction, reflow and choice count must not feed back into placement.
            var position=SourcePoint+side*(ElevatorDialogueText.PanelWidth*.5f*scale+.24f)+Vector3.up*lift;
            var facing=position-lesson.playerCamera.transform.position;
            return new Pose(position,Quaternion.LookRotation(facing.sqrMagnitude>.001f?facing:Vector3.forward,Vector3.up));
        }

        Transform Object(ElevatorClue key)=>objects.TryGetValue(key,out var value)?value:null;

        void Resolve(ElevatorLesson lesson,string who)
        {
            var f=lesson.Flow;var p=lesson.presentation;var space=p?p.space:null;var map=space?space.sportsCentre:null;var team=p?p.rescueTeam:null;
            CurrentVoice=Voice.Friend;source=lesson.friend;lift=.14f;voiceOffset=Vector3.up*1.15f;
            bool remote=(who??"").Contains("관리실")||(who??"").Contains("119");
            if(f.HasObjectPreview&&f.PreviewClue!=ElevatorClue.Friend)
            {CurrentVoice=Voice.Object;source=Object(f.PreviewClue.Value);voiceOffset=Vector3.zero;lift=.32f;}
            else if(f.ReviewingIdentity||who=="엘리베이터 안내판"||who=="위치 알리기")
            {CurrentVoice=Voice.Object;source=map&&map.identity?map.identity:Object(ElevatorClue.Identity);voiceOffset=Vector3.zero;lift=.32f;}
            else if(remote||f.Step==ElevatorStep.Unanswered||f.Step==ElevatorStep.Connecting)
            {
                bool usePhone=f.UsedPhone||f.Step==ElevatorStep.Unanswered;
                CurrentVoice=usePhone?Voice.Phone:Voice.Intercom;
                source=usePhone?Object(ElevatorClue.Phone):map&&map.sideIntercom?map.sideIntercom:Object(ElevatorClue.Intercom);
                if(!source&&lesson.view&&lesson.view.spatial&&!usePhone)source=lesson.view.spatial.intercomAnchor;
                voiceOffset=Vector3.zero;lift=.35f;
            }
            else if(who=="나")
            {
                CurrentVoice=Voice.Player;source=space?space.playerRig:null;
                if(!source)source=lesson.playerCamera.transform.parent;
                var eye=lesson.playerCamera.transform;var forward=Vector3.ProjectOnPlane(eye.forward,Vector3.up).normalized;
                // A one-off first-person speaking position, not a head-following caption.
                SourcePoint=eye.position+forward*1.6f-Vector3.Cross(Vector3.up,forward)*.65f-Vector3.up*.20f;
                voiceOffset=source?SourcePoint-source.position:Vector3.zero;lift=0;
                return;
            }
            else if((who??"").Contains("승강기 담당"))
            {CurrentVoice=Voice.Specialist;source=team?team.specialist:null;}
            else if((who??"").Contains("구조"))
            {CurrentVoice=Voice.Firefighter;source=team?team.firefighter:p&&p.rescuer?p.rescuer.transform:null;}
            else if(who=="도움 요청")
            {CurrentVoice=Voice.Intercom;source=map&&map.sideIntercom?map.sideIntercom:Object(ElevatorClue.Intercom);voiceOffset=Vector3.zero;lift=.35f;}
            if(!source){source=lesson.friend?lesson.friend:lesson.playerCamera.transform.parent;voiceOffset=Vector3.up*1.15f;lift=.14f;}
            if(CurrentVoice==Voice.Friend||CurrentVoice==Voice.Firefighter||CurrentVoice==Voice.Specialist)
            {
                if(source)foreach(var bone in source.GetComponentsInChildren<Transform>(true))if(bone.name=="head")
                {voiceOffset=Vector3.up*(bone.position.y-source.position.y+.10f);break;}
            }
            SourcePoint=source?source.position+voiceOffset:lesson.playerCamera.transform.position+lesson.playerCamera.transform.forward*1.4f;
        }
    }
}
