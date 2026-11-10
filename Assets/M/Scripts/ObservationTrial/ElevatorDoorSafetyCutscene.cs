// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorDoorSafetyCutscene.cs. Original is not modified.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    // Authored child performance. The player's gaze and tracked pose stay under input control.
    [DefaultExecutionOrder(150)]
    public sealed class ElevatorDoorSafetyCutscene : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Vector3 friendByDoor=new Vector3(-1.10f,ElevatorSportsCentreMap.ThirdFloorHeight,3.85f);
        public Vector3 friendAtSafeWall=new Vector3(-.86f,ElevatorSportsCentreMap.ThirdFloorHeight,4.75f);
        public Transform doorPosterAnchor;
        Transform spine,head,leftShoulder,rightShoulder,leftThigh,rightThigh,leftShin,rightShin;
        Vector3 leftShoulderRest,rightShoulderRest;
        Quaternion spineRest;
        float soleOffset;
        Transform[] feet;
        Vector3[][] shoePoints;
        bool initialized;
        public Vector3 SafeWallPosition {get{Initialize();return friendAtSafeWall-Vector3.up*soleOffset;}}
        public bool AtSafeWall => lesson&&lesson.Flow!=null&&lesson.Flow.SafeWallReached&&
            (lesson.Flow.Step==ElevatorStep.SafeWallWaiting||lesson.Flow.Step==ElevatorStep.WaitingFade||lesson.Flow.Step==ElevatorStep.Rescue);
        public static bool IsRelieved(ElevatorFlow flow) => flow.Step==ElevatorStep.CallSafetyAdvice||flow.Step==ElevatorStep.DoorLean||
            flow.Step==ElevatorStep.DoorPosterPointing||flow.Step==ElevatorStep.DoorPosterWarning||flow.DoorRetry||
            flow.Step==ElevatorStep.SafeWallMove||flow.Step==ElevatorStep.SafeWallWaiting||
            flow.Step==ElevatorStep.Waiting&&flow.HelpRequested&&flow.LocationReported||
            flow.SafeWallReached&&(flow.Step==ElevatorStep.WaitingFade||flow.Step==ElevatorStep.Rescue);
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public static bool Describe(ElevatorLesson value,out string who,out string line)
        {
            who="친구";line="";
            switch(value.Flow.Step)
            {
                case ElevatorStep.CallLocationResponse:who="나";line=ElevatorDialogue.LocationReport(value.settings);return true;
                case ElevatorStep.CallSafetyAdvice:who=ElevatorDialogue.Caller(value.Flow.UsedPhone);line=ElevatorDialogue.SafetyAdvice;return true;
                case ElevatorStep.DoorLean:case ElevatorStep.DoorPosterPointing:line=ElevatorDialogue.FriendQuestion;return true;
                case ElevatorStep.DoorPosterWarning:who="나";line=ElevatorDialogue.DoorWarning;return true;
                case ElevatorStep.SafeWallMove:line=ElevatorDialogue.FriendMoving;return true;
                case ElevatorStep.SafeWallWaiting:line=ElevatorDialogue.FriendSettled;return true;
                default:return false;
            }
        }
        void Awake(){Initialize();}
        void Initialize()
        {
            if(initialized||!lesson||!lesson.friend)return;
            var bones=lesson.friend.GetComponentsInChildren<Transform>(true);
            spine=bones.FirstOrDefault(t=>t.name=="spine");head=bones.FirstOrDefault(t=>t.name=="head");
            leftShoulder=bones.FirstOrDefault(t=>t.name=="shoulder.L");rightShoulder=bones.FirstOrDefault(t=>t.name=="shoulder.R");
            leftThigh=bones.FirstOrDefault(t=>t.name=="thigh.L");rightThigh=bones.FirstOrDefault(t=>t.name=="thigh.R");
            leftShin=bones.FirstOrDefault(t=>t.name=="shin.L");rightShin=bones.FirstOrDefault(t=>t.name=="shin.R");
            if(leftShoulder)leftShoulderRest=leftShoulder.localPosition;if(rightShoulder)rightShoulderRest=rightShoulder.localPosition;
            if(spine)spineRest=spine.localRotation;
            // The imported child has a model-space vertical offset. Ground its
            // shoes at the authored floor anchors instead of floating by that offset.
            feet=new[]{bones.FirstOrDefault(t=>t.name=="foot.L"),bones.FirstOrDefault(t=>t.name=="foot.R")};
            var samples=new[]{new List<Vector3>(),new List<Vector3>()};
            float lowest=float.PositiveInfinity;var mesh=new Mesh();
            var expressions=lesson.friend.GetComponentInChildren<ProjectWait.M.Elevator.ObservationTrial.FriendImprovedExpressions>(true);
            var skins=expressions?new[]{expressions.face}:lesson.friend.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var skin in skins)
            {
                if(!skin.enabled)continue;skin.BakeMesh(mesh);
                foreach(var vertex in mesh.vertices)
                {
                    var point=skin.transform.TransformPoint(vertex);var local=lesson.friend.InverseTransformPoint(point);
                    lowest=Mathf.Min(lowest,point.y-lesson.friend.position.y);
                    if(local.y>.30f||!feet[0]||!feet[1])continue;
                    int side=Mathf.Abs(local.x-lesson.friend.InverseTransformPoint(feet[0].position).x)<Mathf.Abs(local.x-lesson.friend.InverseTransformPoint(feet[1].position).x)?0:1;
                    samples[side].Add(feet[side].InverseTransformPoint(point));
                }
            }
            shoePoints=samples.Select(p=>p.ToArray()).ToArray();
            Destroy(mesh);soleOffset=float.IsInfinity(lowest)?0:lowest;
            initialized=true;
        }
        public void ApplyPosition()
        {
            if(!lesson||lesson.Flow==null)return;
            Initialize();
            var f=lesson.Flow;var root=lesson.friend;
            var doorPosition=friendByDoor-Vector3.up*soleOffset;
            if(f.Step==ElevatorStep.DoorLean)
            {
                var map=lesson.presentation.space.sportsCentre;
                // First exhale in place, then take a small step and begin to lean.
                float progress=f.Elapsed/Mathf.Max(.1f,lesson.settings.doorLeanSeconds);
                root.position=Vector3.Lerp(map.friendInside,doorPosition,S(progress,.28f,.62f));
                root.rotation=Quaternion.Euler(0,180,0);
            }
            else if(f.Step==ElevatorStep.DoorPosterPointing||f.Step==ElevatorStep.DoorPosterWarning)
            {root.position=doorPosition;root.rotation=Quaternion.Euler(0,180,0);}
            else if(f.DoorRetry)
            {
                // A small aborted movement, never contact, a fall or a door opening.
                float reaction=Mathf.Sin(Mathf.Clamp01(f.Elapsed/.8f)*Mathf.PI);
                root.position=doorPosition+Vector3.back*(.06f*reaction);
                root.rotation=Quaternion.Euler(0,180,0);
            }
            else if(f.Step==ElevatorStep.SafeWallMove)
            {
                float duration=lesson.settings.safeWallMoveSeconds;
                float travel=S(f.Elapsed,.25f,duration-.55f);
                root.position=Vector3.Lerp(doorPosition,SafeWallPosition,travel);
                var walking=Quaternion.LookRotation(-(friendAtSafeWall-friendByDoor).normalized,Vector3.up);
                var rotation=Quaternion.Slerp(Quaternion.Euler(0,180,0),walking,S(f.Elapsed,0,.45f));
                root.rotation=Quaternion.Slerp(rotation,Quaternion.Euler(0,90,0),S(f.Elapsed,duration-1.05f,duration-.15f));
            }
            else if(AtSafeWall)
            {root.position=SafeWallPosition;root.rotation=Quaternion.Euler(0,90,0);}
        }
        void LateUpdate()
        {
            if(!lesson||lesson.Flow==null||lesson.IsPaused)return;
            Initialize();if(!spine)return;
            spine.localRotation=spineRest;
            var f=lesson.Flow;
            if(leftShoulder)leftShoulder.localPosition=leftShoulderRest;if(rightShoulder)rightShoulder.localPosition=rightShoulderRest;
            float progress=f.Elapsed/Mathf.Max(.1f,lesson.settings.doorLeanSeconds);
            float release=f.Step==ElevatorStep.DoorLean?S(progress,0,.24f):f.Step==ElevatorStep.DoorPosterPointing?1:
                f.Step==ElevatorStep.DoorPosterWarning?1-S(f.Elapsed,.1f,.8f):0;
            float knees=f.Step==ElevatorStep.DoorLean?S(progress,.18f,.42f):f.Step==ElevatorStep.DoorPosterPointing?1:
                f.Step==ElevatorStep.DoorPosterWarning?1-S(f.Elapsed,.1f,.8f):0;
            if(release>0)
            {
                // A small sigh and lowered shoulders show relief, not shaking or distress.
                if(leftShoulder)leftShoulder.position+=Vector3.down*(.018f*release);
                if(rightShoulder)rightShoulder.position+=Vector3.down*(.018f*release);
                spine.rotation=Quaternion.AngleAxis(3*release,lesson.friend.right)*spine.rotation;
                if(head)head.rotation=Quaternion.AngleAxis(4*release,lesson.friend.right)*head.rotation;
            }
            if(knees>0)
            {
                // Soften both knees without buckling, falling or touching the door.
                if(leftThigh)leftThigh.localRotation*=Quaternion.Euler(4*knees,0,0);
                if(rightThigh)rightThigh.localRotation*=Quaternion.Euler(4*knees,0,0);
                if(leftShin)leftShin.localRotation*=Quaternion.Euler(-8*knees,0,0);
                if(rightShin)rightShin.localRotation*=Quaternion.Euler(-8*knees,0,0);
            }
            float lean=f.Step==ElevatorStep.DoorLean?S(progress,.62f,.92f):
                f.Step==ElevatorStep.DoorPosterPointing?1:
                f.Step==ElevatorStep.DoorPosterWarning?1-S(f.Elapsed,.25f,1.0f):f.DoorRetry?.15f*Mathf.Sin(Mathf.Clamp01(f.Elapsed/.8f)*Mathf.PI):0;
            if(lean>0)
            {
                spine.rotation=Quaternion.AngleAxis(-12*lean,Vector3.right)*spine.rotation;
                if(head)head.rotation=Quaternion.AngleAxis(5*lean,lesson.friend.right)*head.rotation;
            }
            if(AtSafeWall)
            {
                float settle=f.Step==ElevatorStep.SafeWallWaiting?S(f.Elapsed,0,.8f):1;
                spine.rotation=Quaternion.AngleAxis(-8*settle,Vector3.forward)*spine.rotation;
                // Relaxed head tilt and breathing make the safe waiting pose legible.
                if(head)head.rotation=Quaternion.AngleAxis(Mathf.Sin(f.Elapsed*1.7f)*1.2f,lesson.friend.right)*head.rotation;
            }
            if(f.IsDoorSafetyCutscene||f.SafeWallReached)GroundShoes();
        }
        void GroundShoes()
        {
            // Probe the actual shoe envelope after Animator and walking IK have
            // rotated the feet. A one-time bind-pose offset is insufficient here.
            float lowest=float.PositiveInfinity;
            for(int i=0;i<2;i++)if(feet[i])foreach(var p in shoePoints[i])lowest=Mathf.Min(lowest,feet[i].TransformPoint(p).y);
            if(float.IsInfinity(lowest))return;
            soleOffset=lowest-lesson.friend.position.y;
            float floor=lesson.presentation.space.sportsCentre.CurrentFloorHeight;
            lesson.friend.position+=Vector3.up*(floor-lowest);
        }
        void OnDisable()
        {
            if(spine)spine.localRotation=spineRest;
            if(leftShoulder)leftShoulder.localPosition=leftShoulderRest;if(rightShoulder)rightShoulder.localPosition=rightShoulderRest;
        }
    }
}
