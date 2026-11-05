using System;
using System.Linq;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    // Stage positions are world anchors. This component never changes player position or gaze.
    public sealed class ElevatorRescueTeam : MonoBehaviour
    {
        public Transform firefighter, specialist;
        public Vector3 firefighterPoseScale=Vector3.one, specialistPoseScale=Vector3.one;
        public Vector3 firefighterAtDoor=new Vector3(-2.30f,0,2.60f);
        public Vector3 specialistAtDoor=new Vector3(-1.25f,0,2.40f);
        public Vector3 firefighterInLobby=new Vector3(-2.85f,0,-4.60f);
        public Vector3 specialistInLobby=new Vector3(-.75f,0,-4.40f);
        public Vector3 friendInLobby=new Vector3(-1.80f,0,-3.40f);
        [Min(1)] public float dialogueHeight=2.95f;
        public Vector3 DialogueAnchor {get;private set;}
        Actor firePose, specialistPose;
        CharacterController collisionPlayer;
        Collider fireCollider,specialistCollider;
        void OnEnable(){collisionPlayer=null;}
        int dialogueRun=-1,dialogueBeat=-1;
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        public void Apply(ElevatorLesson lesson)
        {
            if(!lesson||lesson.Flow==null||!firefighter||!specialist)return;
            var f=lesson.Flow;var map=lesson.presentation.space?lesson.presentation.space.sportsCentre:null;
            bool visible=f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GroundFloor;
            if(gameObject.activeSelf!=visible)gameObject.SetActive(visible);
            if(!visible)return;
            if(firePose==null){firePose=new Actor(firefighter,firefighterPoseScale);specialistPose=new Actor(specialist,specialistPoseScale);}
            // Keep the ray-selectable actor colliders from blocking the escorted player.
            var player=lesson.presentation.space?lesson.presentation.space.playerRig.GetComponent<CharacterController>():null;
            if(player&&collisionPlayer!=player)
            {
                if(!fireCollider)fireCollider=firefighter.GetComponent<Collider>();
                if(!specialistCollider)specialistCollider=specialist.GetComponent<Collider>();
                if(fireCollider)Physics.IgnoreCollision(fireCollider,player,true);
                if(specialistCollider)Physics.IgnoreCollision(specialistCollider,player,true);
                collisionPlayer=player;
            }
            float floor=map?map.CurrentFloorHeight:0,t=f.Elapsed;
            Vector3 a=firefighterAtDoor,b=specialistAtDoor;float faceA=8,faceB=-8,guide=f.ReadyToExit?1:0,checking=f.ReadyToExit?0:1;
            if(f.Step==ElevatorStep.Rescue)
            {
                a=Vector3.Lerp(firefighterAtDoor+new Vector3(-.12f,0,-.9f),firefighterAtDoor,S(t,.25f,1.8f));
                b=Vector3.Lerp(specialistAtDoor+new Vector3(.10f,0,-.9f),specialistAtDoor,S(t,.45f,2.0f));
                guide=f.ReadyToExit?1:S(t,.6f,1.4f)*.40f;
                checking=(1-S(t,lesson.settings.rescueSubtitleSeconds-.7f,lesson.settings.rescueSubtitleSeconds))*S(t,.8f,1.7f);
                DialogueAnchor=new Vector3(-1.8f,floor+dialogueHeight,2.30f);
            }
            else if(f.Step==ElevatorStep.Exiting)
            {
                float walk=S(t,.25f,3.9f);
                a=Vector3.Lerp(firefighterAtDoor,firefighterInLobby,walk);
                b=Vector3.Lerp(specialistAtDoor,specialistInLobby,S(t,1.4f,4.05f));
                var start=f.SafeWallReached&&lesson.doorSafetyCutscene?lesson.doorSafetyCutscene.SafeWallPosition:
                    map?map.friendInside:new Vector3(-1.1f,0,4.12f);
                if(!f.SafeWallReached)start.y=floor;
                var threshold=new Vector3(-1.25f,floor,3.55f);
                lesson.friend.position=t<1.3f?Vector3.Lerp(start,threshold,S(t,.15f,1.3f)):Vector3.Lerp(threshold,friendInLobby+Vector3.up*floor,S(t,1.3f,3.85f));
                lesson.friend.rotation=Quaternion.Euler(0,t<3.5f?(f.SafeWallReached?90*(1-S(t,0,.4f)):0):Mathf.Lerp(0,180,S(t,3.5f,4.2f)),0);
                faceA=Mathf.Lerp(160,0,S(t,3.25f,4.05f));faceB=Mathf.Lerp(175,0,S(t,3.4f,4.2f));
                guide=.6f;checking=0;
                DialogueAnchor=new Vector3(-1.8f,floor+dialogueHeight,-4.3f);
            }
            else if(f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.Returning&&floor>1)
            {
                a=firefighterInLobby;b=specialistInLobby;faceA=8;faceB=-8;guide=1;checking=0;
                lesson.friend.position=friendInLobby+Vector3.up*floor;lesson.friend.rotation=Quaternion.Euler(0,180,0);
                DialogueAnchor=new Vector3(-1.8f,floor+dialogueHeight,-4.30f);
            }
            else
            {
                a=new Vector3(-2.55f,0,-3.8f);b=new Vector3(1.45f,0,-3.6f);faceA=12;faceB=-18;guide=.55f;checking=0;
                if(f.Step==ElevatorStep.GroundFloor){lesson.friend.position=new Vector3(-.2f,floor,-2.4f);lesson.friend.rotation=Quaternion.Euler(0,180,0);}
                DialogueAnchor=new Vector3(-1.4f,floor+dialogueHeight,-3.6f);
            }
            a.y=b.y=floor;
            float walking=f.Step==ElevatorStep.Exiting?S(t,.15f,.45f)*(1-S(t,3.5f,4.1f)):f.Step==ElevatorStep.Rescue?S(t,.15f,.35f)*(1-S(t,1.45f,1.95f)):0;
            firePose.Present(a,faceA,.035f,guide,false,t,0,walking);
            specialistPose.Present(b,faceB,.075f*checking,guide,true,t,checking,walking);
            if(dialogueRun!=lesson.RunVersion){dialogueRun=lesson.RunVersion;dialogueBeat=-1;}
            int beat=f.Step==ElevatorStep.Rescue?(f.ReadyToExit?2:f.Elapsed>=1.6f?1:0):-1;
            bool speakerChanged=beat==1&&dialogueBeat==0;dialogueBeat=beat;
            if(speakerChanged)lesson.Refresh();
        }
        public void Describe(ElevatorLesson lesson,ref string who,ref string line)
        {
            var f=lesson.Flow;
            if(f.Step==ElevatorStep.Rescue)
            {
                if(f.ReadyToExit){who="소방 구조대원";line="승강장과 나갈 길을 확인했어요.\n두 구조요원의 안내를 따라\n천천히 나와 주세요.";}
                else if(f.Elapsed<1.6f){who="소방 구조대원";line="구조대가 도착했어요.\n문에서 떨어져 안에서 기다려 주세요.";}
                else {who="승강기 전문 구조요원";line="승강장과 바닥 높이를 확인하고 있어요.\n확인이 끝나면 함께 안내할게요.";}
            }
            else if(f.Step==ElevatorStep.ReturnLobby){who="소방 구조대원";line="승강기는 점검이 필요해요.\n저희를 선택하면 계단으로\n1층까지 함께 내려갈게요.";}
        }
        sealed class Actor
        {
            readonly Transform root,pelvis,spine,head;
            readonly Vector3 poseScale;
            readonly Transform[] bones,upper,lower,hand,thigh,shin,foot;
            readonly Vector3[] positions,feet;
            readonly Quaternion[] rotations,footRotations;
            static readonly string[] Names={"root","pelvis","spine","chest","neck","head","upper_arm.L","upper_arm.R","forearm.L","forearm.R","hand.L","hand.R","thigh.L","thigh.R","shin.L","shin.R","foot.L","foot.R"};
            public Actor(Transform actor,Vector3 proportions)
            {
                poseScale=proportions;
                root=actor;bones=actor.GetComponentsInChildren<Transform>(true).Where(x=>Names.Contains(x.name)).ToArray();
                positions=bones.Select(x=>x.localPosition).ToArray();rotations=bones.Select(x=>x.localRotation).ToArray();
                pelvis=Bone("pelvis");spine=Bone("spine");head=Bone("head");
                upper=Sides("upper_arm.");lower=Sides("forearm.");hand=Sides("hand.");thigh=Sides("thigh.");shin=Sides("shin.");foot=Sides("foot.");
                feet=foot.Select(x=>root.InverseTransformPoint(x.position)).ToArray();footRotations=foot.Select(x=>Quaternion.Inverse(root.rotation)*x.rotation).ToArray();
            }
            Transform Bone(string name)=>bones.First(x=>x.name==name);
            Transform[] Sides(string name)=>new[]{Bone(name+"L"),Bone(name+"R")};
            public void Present(Vector3 position,float heading,float crouch,float guide,bool technical,float time,float checking=0,float walking=0)
            {
                root.SetPositionAndRotation(position,Quaternion.Euler(0,heading,0));
                for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
                pelvis.position-=Vector3.up*(crouch*poseScale.y*root.lossyScale.y);
                spine.rotation=Quaternion.AngleAxis(checking*12+(technical?2:4),root.right)*spine.rotation;
                head.rotation=Quaternion.AngleAxis(checking*10+Mathf.Sin(time*1.7f)*1.2f,root.right)*head.rotation;
                for(int i=0;i<2;i++)
                {
                    var step=feet[i];float wave=Mathf.Sin(time*7+i*Mathf.PI);
                    step.z+=wave*.075f*walking*poseScale.z;step.y+=Mathf.Max(0,wave)*.045f*walking*poseScale.y;
                    IK(thigh[i],shin[i],foot[i],root.TransformPoint(step),root.forward);
                    foot[i].rotation=root.rotation*footRotations[i];
                    float side=Mathf.Sign(root.InverseTransformPoint(upper[i].position).x);
                    bool gesture=technical?side<0:side>0;
                    if(gesture||checking>.01f)
                    {
                        var relaxed=new Vector3(side*.25f,.66f,.05f);
                        var target=checking>.01f?new Vector3(side*.29f,.61f,.32f):new Vector3(side*.29f,1.01f+Mathf.Sin(time*2.2f)*.015f,.31f);
                        target=Vector3.Lerp(relaxed,target,Mathf.Max(guide,checking));
                        target=Vector3.Scale(target,poseScale);
                        // Keep the elbow behind the shoulder-to-hand line, matching
                        // the corrected rear poles in the Blender source rigs.
                        IK(upper[i],lower[i],hand[i],root.TransformPoint(target),root.right*(side*.25f)-root.forward);
                    }
                }
            }
            static void IK(Transform a,Transform b,Transform c,Vector3 target,Vector3 pole)
            {
                var line=target-a.position;float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
                if(l1<.001f||l2<.001f||line.sqrMagnitude<.000001f)return;
                float d=Mathf.Clamp(line.magnitude,Mathf.Abs(l1-l2)+.0001f,(l1+l2)*.999f);var direction=line.normalized;
                float along=(l1*l1-l2*l2+d*d)/(2*d);var perpendicular=Vector3.ProjectOnPlane(pole,direction).normalized;
                if(perpendicular.sqrMagnitude<.1f)perpendicular=Vector3.Cross(direction,Vector3.up).normalized;
                var elbow=a.position+direction*along+perpendicular*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
                a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
                b.rotation=Quaternion.FromToRotation(c.position-b.position,a.position+direction*d-b.position)*b.rotation;
            }
        }
    }
}
