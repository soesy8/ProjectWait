using UnityEngine;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator
{
    // Authored at real floor heights. Rescue takes place at the checked 3F landing;
    // the subsequent escorted stair journey is an explicit, faded scene transition.
    public sealed class ElevatorSportsCentreMap : MonoBehaviour
    {
        public const float ThirdFloorHeight = 7.92f;
        public ElevatorSpacePresentation space;
        public Transform geometry;
        public Transform autoDoorLeft, autoDoorRight;
        public Transform sideIntercom, frontIntercom, identity, facilityGuide;
        public Renderer[] firstFloorRings, thirdFloorRings;
        public Material normalButton, selectedButton;
        public Vector3 lobby = new Vector3(-1.8f, ThirdFloorHeight, -1.8f);
        public Vector3 inside = new Vector3(-1.8f, ThirdFloorHeight, 5.8f);
        public Vector3 friendInside = new Vector3(-1.1f, ThirdFloorHeight, 4.12f);
        public Vector3 friendLobby = new Vector3(-3.2f, ThirdFloorHeight, 1.4f);
        public Vector3 groundLobby = new Vector3(-1.4f, 0, -.4f);
        public Vector3 groundExit = new Vector3(-.8f, 0, -7.2f);
        Vector3 leftClosed, rightClosed;
        ElevatorStep previous = (ElevatorStep)(-1);
        ElevatorDesktopControls controls;
        int run = -1;
        float lastFloor;
        bool initialized;
        bool teleported;
        public float DoorOpen { get; private set; }
        ElevatorLesson Lesson => space.lesson;
        public bool OnGround => Lesson.Flow.Step==ElevatorStep.GroundFloor || Lesson.Flow.Step==ElevatorStep.GoingHome || Lesson.Flow.Step==ElevatorStep.Complete || Lesson.Flow.Step==ElevatorStep.Returning&&Lesson.Flow.Elapsed>=1.75f;
        public float CurrentFloorHeight => OnGround ? 0 : ThirdFloorHeight;
        public float DefaultHeading => Lesson.Flow!=null&&(Lesson.Flow.Step==ElevatorStep.Lobby || Lesson.Flow.Step==ElevatorStep.Entering || Lesson.Flow.Step==ElevatorStep.ReturnLobby)?0:180;
        public Quaternion DialogueRotation => Quaternion.Euler(0, DefaultHeading, 0);
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void MoveRig(Vector3 p,float heading)
        {
            if (!controls) controls=FindFirstObjectByType<ElevatorDesktopControls>();
            float offset=XRSettings.isDeviceActive ? space.playerRig.position.y-lastFloor : 0;
            var controller=space.playerRig.GetComponent<CharacterController>();
            bool enabledController=controller&&controller.enabled;
            if(enabledController)controller.enabled=false;
            space.playerRig.position=p+Vector3.up*offset;lastFloor=p.y;
            if(enabledController)controller.enabled=true;
            if(XRSettings.isDeviceActive)space.playerRig.rotation=Quaternion.Euler(0,heading,0);
            else {space.playerRig.rotation=Quaternion.identity;if(controls)controls.SetScriptedHeading(heading);}
            Lesson.view.Reanchor(Lesson.playerCamera.transform.position);
        }
        public void Apply()
        {
            var f=Lesson.Flow;var p=Lesson.presentation;float t=f.Elapsed;
            if(!initialized){leftClosed=p.leftDoor.localPosition;rightClosed=p.rightDoor.localPosition;lastFloor=space.playerRig.position.y;initialized=true;}
            bool reset=run!=Lesson.RunVersion,changed=previous!=f.Step;
            bool outside=f.Step==ElevatorStep.Lobby || f.Step==ElevatorStep.ReturnLobby || OnGround;
            if(reset||changed)
            {
                teleported=false;
                if(f.Step==ElevatorStep.Lobby)MoveRig(lobby,0);
                else if(f.Step==ElevatorStep.Boarding || f.Step==ElevatorStep.Proposal || f.Step==ElevatorStep.Stopped || f.Step==ElevatorStep.Call || f.Step==ElevatorStep.Waiting || f.Step==ElevatorStep.Rescue)
                {if(reset||previous==ElevatorStep.Entering)MoveRig(inside,180);}
                else if(f.Step==ElevatorStep.ReturnLobby)MoveRig(lobby,0);
                else if(f.Step==ElevatorStep.GroundFloor)MoveRig(groundLobby,180);
                else if(f.Step==ElevatorStep.Complete)MoveRig(groundExit,180);
            }
            Lesson.friend.position=outside?(OnGround?new Vector3(-.2f,0,-2.4f):friendLobby):friendInside;
            Lesson.friend.rotation=Quaternion.Euler(0,outside?DefaultHeading:180,0);
            DoorOpen=0;
            if(f.Step==ElevatorStep.Entering)
            {
                DoorOpen=S(t,.25f,1.15f)*(1-S(t,4.8f,5.75f));
                Vector3 mid=new Vector3(-1.8f,ThirdFloorHeight,3.6f);
                Lesson.friend.position=t<2.4f?Vector3.Lerp(friendLobby,mid,S(t,.9f,2.4f)):Vector3.Lerp(mid,friendInside,S(t,2.4f,3.65f));
                Lesson.friend.rotation=Quaternion.Euler(0,Mathf.Lerp(0,180,S(t,3.2f,4.1f)),0);
                if(XRSettings.isDeviceActive){if(t>=3.1f&&!teleported){MoveRig(inside,180);teleported=true;}}
                else MoveRig(Vector3.Lerp(lobby,inside,S(t,1.2f,4.5f)),Mathf.Lerp(0,180,S(t,3.4f,4.7f)));
            }
            else if(f.Step==ElevatorStep.Rescue)DoorOpen=S(t,.4f,2.1f);
            else if(f.Step==ElevatorStep.Exiting)
            {
                DoorOpen=1-S(t,3.9f,4.6f);
                var mid=new Vector3(-1.8f,ThirdFloorHeight,3.5f);
                Lesson.friend.position=t<1.7f?Vector3.Lerp(friendInside,mid,S(t,.15f,1.7f)):Vector3.Lerp(mid,friendLobby,S(t,1.7f,3.1f));
                Lesson.friend.rotation=Quaternion.identity;
                if(XRSettings.isDeviceActive){if(t>=2.3f&&!teleported){MoveRig(lobby,0);teleported=true;}}
                else MoveRig(Vector3.Lerp(inside,lobby,S(t,.65f,3.95f)),Mathf.Lerp(180,0,S(t,3.15f,4.25f)));
            }
            else if(f.Step==ElevatorStep.Returning)
            {
                if(t>=1.75f&&lastFloor>1)MoveRig(groundLobby,180);
            }
            else if(f.Step==ElevatorStep.GoingHome)
            {
                if(XRSettings.isDeviceActive){if(t>=3&&!teleported){MoveRig(groundExit,180);teleported=true;}}
                else MoveRig(Vector3.Lerp(groundLobby,groundExit,S(t,.7f,5.3f)),180);
                Lesson.friend.position=Vector3.Lerp(new Vector3(-.2f,0,-2.4f),new Vector3(.1f,0,-8.4f),S(t,.2f,4.9f));
                Lesson.friend.rotation=Quaternion.Euler(0,Mathf.Lerp(0,180,S(t,4.6f,5.8f)),0);
            }
            else if(f.Step==ElevatorStep.Complete)Lesson.friend.position=new Vector3(.1f,0,-8.4f);
            p.leftDoor.localPosition=leftClosed+Vector3.left*(DoorOpen*.9f);
            p.rightDoor.localPosition=rightClosed+Vector3.right*(DoorOpen*.9f);
            p.closedDoorCollider.enabled=DoorOpen<.95f;
            float autoOpen=f.Step==ElevatorStep.GoingHome?S(t,.1f,.85f):f.Step==ElevatorStep.Complete?1:0;
            if(autoDoorLeft)autoDoorLeft.localPosition=Vector3.left*(autoOpen*1.3f);
            if(autoDoorRight)autoDoorRight.localPosition=Vector3.right*(autoOpen*1.3f);
            if(firstFloorRings!=null)foreach(var r in firstFloorRings)if(r)r.sharedMaterial=f.DestinationSelected?selectedButton:normalButton;
            if(thirdFloorRings!=null)foreach(var r in thirdFloorRings)if(r)r.sharedMaterial=f.DestinationSelected?normalButton:selectedButton;
            bool rescue=f.Step==ElevatorStep.Rescue || f.Step==ElevatorStep.Exiting || f.Step==ElevatorStep.ReturnLobby || f.Step==ElevatorStep.Returning || f.Step==ElevatorStep.GroundFloor;
            p.rescuer.SetActive(rescue);p.rescuer.transform.position=OnGround?new Vector3(-2.8f,0,-2.5f):new Vector3(-2.3f,ThirdFloorHeight,1.25f);
            p.rescuer.transform.rotation=Quaternion.Euler(0,f.Step==ElevatorStep.ReturnLobby?0:180,0);
            if(p.friendShadow){p.friendShadow.position=new Vector3(Lesson.friend.position.x,CurrentFloorHeight+.012f,Lesson.friend.position.z);p.friendShadow.localScale=new Vector3(.58f,.008f,.34f);}
            previous=f.Step;run=Lesson.RunVersion;
        }
        public Vector3 DialogueAnchor()
        {
            var f=Lesson.Flow;
            if(f.Step==ElevatorStep.Call || f.Step==ElevatorStep.Connecting || f.Step==ElevatorStep.Unanswered)return new Vector3(-1.6f,ThirdFloorHeight+1.55f,4.25f);
            if(f.Step==ElevatorStep.Retry)return new Vector3(-1.8f,ThirdFloorHeight+1.55f,4.05f);
            if(f.Step==ElevatorStep.Rescue)return new Vector3(-1.65f,ThirdFloorHeight+1.65f,2.3f);
            if(f.Step==ElevatorStep.ReturnLobby)return new Vector3(-1.1f,ThirdFloorHeight+1.6f,1.3f);
            return Lesson.friend.position+DialogueRotation*new Vector3(1.05f,1.50f,-.18f);
        }
    }
}
