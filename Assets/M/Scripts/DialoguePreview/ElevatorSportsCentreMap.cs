// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorSportsCentreMap.cs. Original is not modified.
using UnityEngine;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Authored at real floor heights. Rescue takes place at the checked 3F landing;
    // the subsequent escorted stair journey is an explicit, faded scene transition.
    public sealed class ElevatorSportsCentreMap : MonoBehaviour
    {
        public const float ThirdFloorHeight = 7.92f;
        public ElevatorSpacePresentation space;
        public Transform geometry;
        public GameObject firstFloorGeometry;
        public Transform autoDoorLeft, autoDoorRight;
        public Transform thirdFloorAutoDoorLeft,thirdFloorAutoDoorRight;
        public Collider gymBoundary;
        public Transform sideIntercom, frontIntercom, identity, facilityGuide;
        public Renderer[] firstFloorRings, thirdFloorRings;
        public Material normalButton, selectedButton;
        public Vector3 lobby = new Vector3(-1.8f, ThirdFloorHeight, -1.8f);
        public Vector3 inside = new Vector3(-1.8f, ThirdFloorHeight, 5.8f);
        public Vector3 friendInside = new Vector3(-1.1f, ThirdFloorHeight, 4.12f);
        public Vector3 friendLobby = new Vector3(-3.2f, ThirdFloorHeight, 1.4f);
        public Vector3 gymStart = new Vector3(0, ThirdFloorHeight, -13);
        public Vector3 friendGymStart = new Vector3(-1.15f, ThirdFloorHeight, -10.5f);
        public Vector3 groundLobby = new Vector3(-1.4f, 0, -.4f);
        public Vector3 groundExit = new Vector3(-.8f, 0, -7.2f);
        Vector3 leftClosed, rightClosed;
        ElevatorStep previous = (ElevatorStep)(-1);
        ElevatorDesktopControls controls;
        int run = -1;
        float lastFloor;
        bool initialized;
        bool teleported;
        bool ringsInitialized,previousDestination;
        public void ConfigureTravel()
        {
            float duration=ProjectWait.M.Shared.ComfortBlackout.Duration;
            Lesson.Flow.ConfigureTravel(duration,duration,duration,duration);
        }
        public bool IsInteriorBeat=>InteriorBeat(Lesson.Flow.Step)||Lesson.Flow.IsDoorSafetyCutscene||Lesson.Flow.IsCallCutscene||
            Lesson.Flow.AwaitingCallContinue||Lesson.Flow.Step==ElevatorStep.DoorPosterPointing;
        static bool InteriorBeat(ElevatorStep step)=>step==ElevatorStep.Boarding||step==ElevatorStep.Proposal||step==ElevatorStep.PosterPointing||step==ElevatorStep.PosterWarning||step==ElevatorStep.Jump||step==ElevatorStep.Travel||step==ElevatorStep.Stopped||step==ElevatorStep.Connecting||step==ElevatorStep.Call||step==ElevatorStep.CallQuestion||step==ElevatorStep.CallResponse||step==ElevatorStep.CallLiftQuestion||step==ElevatorStep.CallIdentityGuide||step==ElevatorStep.Unanswered||step==ElevatorStep.Retry||step==ElevatorStep.Waiting||step==ElevatorStep.WaitingFade||step==ElevatorStep.Rescue;
        float thirdFloorDoorOpen;
        public float DoorOpen { get; private set; }
        ElevatorLesson Lesson => space.lesson;
        public bool OnGround => Lesson.Flow.Step==ElevatorStep.GroundFloor || Lesson.Flow.Step==ElevatorStep.GoingHome || Lesson.Flow.Step==ElevatorStep.Complete || Lesson.Flow.Step==ElevatorStep.Returning&&Lesson.Flow.Elapsed>=ProjectWait.M.Shared.ComfortBlackout.Cover;
        public float CurrentFloorHeight => OnGround ? 0 : ThirdFloorHeight;
        public float DefaultHeading => Lesson.Flow!=null&&(Lesson.Flow.IsOpening || Lesson.Flow.Step==ElevatorStep.Lobby || Lesson.Flow.Step==ElevatorStep.Entering || Lesson.Flow.Step==ElevatorStep.ReturnLobby)?0:180;
        public Quaternion DialogueRotation => Quaternion.Euler(0, DefaultHeading, 0);
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void MoveRig(Vector3 p,bool resetHeading=false,float heading=0)
        {
            if (!controls) controls=FindFirstObjectByType<ElevatorDesktopControls>();
            var movement=space.playerRig.GetComponent<ElevatorPlayerMovement>();
            // Scene handoff can offset either rig to preserve the incoming eye height.
            // Keep that feet offset during authored walks as well as XR teleports.
            float offset=movement?-movement.FeetOffset:XRSettings.isDeviceActive?space.playerRig.position.y-lastFloor:0;
            var controller=space.playerRig.GetComponent<CharacterController>();
            bool enabledController=controller&&controller.enabled;
            if(enabledController)controller.enabled=false;
            space.playerRig.position=p+Vector3.up*offset;lastFloor=p.y;
            // New runs reset heading; cinematic turns are applied separately, additive to live look.
            if(resetHeading){if(XRSettings.isDeviceActive)space.playerRig.rotation=Quaternion.Euler(0,heading,0);
            else {space.playerRig.rotation=Quaternion.identity;if(controls)controls.SetScriptedHeading(heading);}}
            // Destinations describe the head/capsule footprint, not the tracking origin.
            // Apply this after heading changes, which also rotate room-scale offsets.
            var horizontal=Lesson.playerCamera.transform.position-space.playerRig.position;
            horizontal.y=0;space.playerRig.position-=horizontal;
            if(enabledController)controller.enabled=true;
            if(movement)movement.ResetVerticalMotion();
            if(resetHeading&&controls)controls.RequestAuthoredSpawn(p,heading);
            Lesson.view.Reanchor(Lesson.playerCamera.transform.position);
        }
        public void Apply()
        {
            var f=Lesson.Flow;var p=Lesson.presentation;float t=f.Elapsed;
            if(!initialized){leftClosed=p.leftDoor.localPosition;rightClosed=p.rightDoor.localPosition;lastFloor=space.playerRig.position.y;initialized=true;}
            bool reset=run!=Lesson.RunVersion,changed=previous!=f.Step;
            if(reset)ConfigureTravel();
            bool outside=f.IsOpening || f.Step==ElevatorStep.Lobby || f.Step==ElevatorStep.ReturnLobby || OnGround;
            if(reset||changed)
            {
                teleported=false;
                if(reset)
                {
                    if(f.IsOpening)MoveRig(gymStart,true,0);
                    else if(f.Step==ElevatorStep.Lobby)MoveRig(lobby,true,0);
                    else if(IsInteriorBeat)MoveRig(inside,true,180);
                    else if(f.Step==ElevatorStep.ReturnLobby)MoveRig(lobby,true,0);
                    else if(f.Step==ElevatorStep.GroundFloor)MoveRig(groundLobby,true,180);
                    else if(f.Step==ElevatorStep.Complete)MoveRig(groundExit,true,180);
                }
            }
            if(ElevatorTravelTiming.IsRelocation(f.Step)&&t>=ProjectWait.M.Shared.ComfortBlackout.Cover&&!teleported)
            {
                var blackout=ProjectWait.M.Shared.ComfortScreenFade.For(Lesson.playerCamera);if(blackout)blackout.SetOpacity(1);
                Vector3 destination=f.Step==ElevatorStep.Entering?inside:f.Step==ElevatorStep.Returning?groundLobby:f.Step==ElevatorStep.GoingHome?groundExit:lobby;
                MoveRig(destination);
                if(controls)controls.FaceAuthoredHeading(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GoingHome?180:0);
                teleported=true;
            }
            Lesson.friend.position=outside?(OnGround?new Vector3(-.2f,0,-2.4f):friendLobby):friendInside;
            Lesson.friend.rotation=Quaternion.Euler(0,outside?DefaultHeading:180,0);
            if(gymBoundary)gymBoundary.enabled=!f.IsOpening;
            DoorOpen=0;
            if(f.Step==ElevatorStep.StartupFade)
            {
                // Hold the establishing pose until the entire fade has finished.
                Lesson.friend.position=friendGymStart;
                Lesson.friend.rotation=Quaternion.identity;
            }
            else if(f.Step==ElevatorStep.GymDeparture)
            {
                Lesson.friend.position=teleported?friendLobby:friendGymStart;
                Lesson.friend.rotation=Quaternion.identity;
            }
            else if(f.Step==ElevatorStep.Entering)
            {
                DoorOpen=teleported?0:1;
                Lesson.friend.position=teleported?friendInside:friendLobby;
                Lesson.friend.rotation=Quaternion.Euler(0,teleported?180:0,0);
            }
            else if(f.Step==ElevatorStep.Rescue)DoorOpen=S(t,.4f,2.1f);
            else if(f.Step==ElevatorStep.Exiting)
            {
                DoorOpen=teleported?0:1;
                Lesson.friend.position=teleported?friendLobby:friendInside;
                Lesson.friend.rotation=Quaternion.Euler(0,teleported?0:180,0);
            }
            else if(f.Step==ElevatorStep.GoingHome)
            {
                Lesson.friend.position=teleported?new Vector3(.1f,0,-8.4f):new Vector3(-.2f,0,-2.4f);
                Lesson.friend.rotation=Quaternion.Euler(0,teleported?180:0,0);
            }
            else if(f.Step==ElevatorStep.Complete)Lesson.friend.position=new Vector3(.1f,0,-8.4f);
            if(Lesson.doorSafetyCutscene)Lesson.doorSafetyCutscene.ApplyPosition();
            p.leftDoor.localPosition=leftClosed+Vector3.left*(DoorOpen*.9f);
            p.rightDoor.localPosition=rightClosed+Vector3.right*(DoorOpen*.9f);
            p.closedDoorCollider.enabled=DoorOpen<.95f;
            // A closed 3F cabin cannot see or reach 1F. Restore it before any door opens.
            if(firstFloorGeometry)
            {
                bool show=OnGround||!IsInteriorBeat||DoorOpen>.001f;
                if(firstFloorGeometry.activeSelf!=show)firstFloorGeometry.SetActive(show);
            }
            float autoOpen=f.Step==ElevatorStep.GoingHome?S(t,.1f,.85f):f.Step==ElevatorStep.Complete?1:0;
            if(autoDoorLeft)autoDoorLeft.localPosition=Vector3.left*(autoOpen*1.3f);
            if(autoDoorRight)autoDoorRight.localPosition=Vector3.right*(autoOpen*1.3f);
            var walk=space.playerRig.GetComponent<ElevatorPlayerMovement>();
            var feet=walk?walk.FeetPosition:space.playerRig.position;
            bool nearThirdDoor=Mathf.Abs(feet.y-ThirdFloorHeight)<1.4f&&Vector2.Distance(new Vector2(feet.x,feet.z),new Vector2(-.6f,-5.4f))<2.4f;
            // Open ahead of both walkers, then close only after they clear the threshold.
            if(f.Step==ElevatorStep.StartupFade)thirdFloorDoorOpen=0;
            else if(f.Step==ElevatorStep.GymDeparture)
            {
                thirdFloorDoorOpen=0;
            }
            else if(reset)thirdFloorDoorOpen=nearThirdDoor?1:0;
            else thirdFloorDoorOpen=Mathf.MoveTowards(thirdFloorDoorOpen,nearThirdDoor?1:0,Time.deltaTime*2.4f);
            if(thirdFloorAutoDoorLeft)thirdFloorAutoDoorLeft.localPosition=Vector3.left*(thirdFloorDoorOpen*1.3f);
            if(thirdFloorAutoDoorRight)thirdFloorAutoDoorRight.localPosition=Vector3.right*(thirdFloorDoorOpen*1.3f);
            if(!ringsInitialized||previousDestination!=f.DestinationSelected)
            {
                if(firstFloorRings!=null)foreach(var r in firstFloorRings)if(r)r.sharedMaterial=f.DestinationSelected?selectedButton:normalButton;
                if(thirdFloorRings!=null)foreach(var r in thirdFloorRings)if(r)r.sharedMaterial=f.DestinationSelected?normalButton:selectedButton;
                ringsInitialized=true;previousDestination=f.DestinationSelected;
            }
            bool rescue=f.Step==ElevatorStep.Rescue || f.Step==ElevatorStep.Exiting || f.Step==ElevatorStep.ReturnLobby || f.Step==ElevatorStep.Returning || f.Step==ElevatorStep.GroundFloor;
            p.rescuer.SetActive(rescue);p.rescuer.transform.position=OnGround?new Vector3(-2.8f,0,-2.5f):new Vector3(-2.3f,ThirdFloorHeight,1.25f);
            p.rescuer.transform.rotation=Quaternion.Euler(0,f.Step==ElevatorStep.ReturnLobby?0:180,0);
            if(p.friendShadow){p.friendShadow.position=new Vector3(Lesson.friend.position.x,CurrentFloorHeight+.012f,Lesson.friend.position.z);p.friendShadow.localScale=new Vector3(.58f,.008f,.34f);}
            // All subsequent interior interaction/phase changes preserve position and gaze.
            previous=f.Step;run=Lesson.RunVersion;
        }
        public Vector3 DialogueAnchor()
        {
            var f=Lesson.Flow;
            if(Lesson.presentation.rescueTeam&&(f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.ReturnLobby||f.Step==ElevatorStep.GroundFloor))return Lesson.presentation.rescueTeam.DialogueAnchor;
            if(f.Step==ElevatorStep.Call || f.IsCallCutscene || f.Step==ElevatorStep.Unanswered || f.Step==ElevatorStep.Retry)
            {
                if(!f.UsedPhone&&sideIntercom)return sideIntercom.position+new Vector3(-.38f,.46f,.08f);
                return Lesson.friend.position+DialogueRotation*new Vector3(.58f,1.12f,-.20f);
            }
            if(f.Step==ElevatorStep.Rescue)return new Vector3(-1.65f,ThirdFloorHeight+1.65f,2.3f);
            if(f.Step==ElevatorStep.ReturnLobby)return new Vector3(-1.1f,ThirdFloorHeight+1.6f,1.3f);
            return Lesson.friend.position+DialogueRotation*new Vector3(.72f,1.50f,-.18f);
        }
    }
}
