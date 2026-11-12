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
        bool entryTurnComplete;
        bool ringsInitialized,previousDestination;
        float entryTurnApplied;
        float openingYawApplied;
        public float OpeningViewYaw => openingYawApplied;
        public bool IsInteriorBeat=>InteriorBeat(Lesson.Flow.Step)||Lesson.Flow.IsDoorSafetyCutscene||Lesson.Flow.IsCallCutscene||
            Lesson.Flow.AwaitingCallContinue||Lesson.Flow.Step==ElevatorStep.DoorPosterPointing;
        static bool InteriorBeat(ElevatorStep step)=>step==ElevatorStep.Boarding||step==ElevatorStep.Proposal||step==ElevatorStep.PosterPointing||step==ElevatorStep.PosterWarning||step==ElevatorStep.Jump||step==ElevatorStep.Travel||step==ElevatorStep.Stopped||step==ElevatorStep.Connecting||step==ElevatorStep.Call||step==ElevatorStep.CallQuestion||step==ElevatorStep.CallResponse||step==ElevatorStep.CallLiftQuestion||step==ElevatorStep.CallIdentityGuide||step==ElevatorStep.Unanswered||step==ElevatorStep.Retry||step==ElevatorStep.Waiting||step==ElevatorStep.WaitingFade||step==ElevatorStep.Rescue;
        Vector3 transitionStart;
        float thirdFloorDoorOpen;
        public float DoorOpen { get; private set; }
        ElevatorLesson Lesson => space.lesson;
        public bool OnGround => Lesson.Flow.Step==ElevatorStep.GroundFloor || Lesson.Flow.Step==ElevatorStep.GoingHome || Lesson.Flow.Step==ElevatorStep.Complete || Lesson.Flow.Step==ElevatorStep.Returning&&Lesson.Flow.Elapsed>=1.75f;
        public float CurrentFloorHeight => OnGround ? 0 : ThirdFloorHeight;
        public float DefaultHeading => Lesson.Flow!=null&&(Lesson.Flow.IsOpening || Lesson.Flow.Step==ElevatorStep.Lobby || Lesson.Flow.Step==ElevatorStep.Entering || Lesson.Flow.Step==ElevatorStep.ReturnLobby)?0:180;
        public Quaternion DialogueRotation => Quaternion.Euler(0, DefaultHeading, 0);
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void ApplyEntryTurn(float progress)
        {
            // Add each authored rotation increment to the live gaze. Mouse/head
            // movement never changes the remaining 180-degree cinematic turn.
            float total=180*Mathf.Clamp01(progress);
            controls.AddCinematicYaw(total-entryTurnApplied);
            entryTurnApplied=total;entryTurnComplete=progress>=1;
        }
        void ApplyOpeningView(float yaw)
        {
            if(XRSettings.isDeviceActive)return;
            if(!controls)controls=FindFirstObjectByType<ElevatorDesktopControls>();
            if(!controls)return;
            float delta=Mathf.DeltaAngle(openingYawApplied,yaw);
            if(Mathf.Abs(delta)>.00001f)controls.AddCinematicYaw(delta);
            openingYawApplied=yaw;
        }
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
            Lesson.view.Reanchor(Lesson.playerCamera.transform.position);
        }
        public void Apply()
        {
            var f=Lesson.Flow;var p=Lesson.presentation;float t=f.Elapsed;
            if(!initialized){leftClosed=p.leftDoor.localPosition;rightClosed=p.rightDoor.localPosition;lastFloor=space.playerRig.position.y;initialized=true;}
            bool reset=run!=Lesson.RunVersion,changed=previous!=f.Step;
            if(reset||changed&&f.Step==ElevatorStep.GymDeparture)openingYawApplied=0;
            if(reset||changed&&f.Step==ElevatorStep.Entering){entryTurnComplete=false;entryTurnApplied=0;}
            bool outside=f.IsOpening || f.Step==ElevatorStep.Lobby || f.Step==ElevatorStep.ReturnLobby || OnGround;
            if(reset||changed)
            {
                // Flow can cross a whole movement interval before presentation runs.
                // Commit a missed arrival without repeating an already completed teleport.
                if(changed&&!reset&&!teleported)
                {
                    if(previous==ElevatorStep.Entering&&f.Step==ElevatorStep.Boarding)MoveRig(inside);
                    else if(previous==ElevatorStep.Exiting&&f.Step==ElevatorStep.ReturnLobby)MoveRig(lobby);
                    else if(previous==ElevatorStep.GoingHome&&f.Step==ElevatorStep.Complete)MoveRig(groundExit);
                }
                if(changed&&previous==ElevatorStep.GymDeparture&&f.Step==ElevatorStep.Lobby)
                {ApplyOpeningView(0);if(!teleported)MoveRig(lobby);}
                teleported=false;
                if(changed&&f.Step==ElevatorStep.GroundFloor&&!reset&&lastFloor>1)MoveRig(groundLobby);
                var walking=space.playerRig.GetComponent<ElevatorPlayerMovement>();
                transitionStart=walking?walking.FeetPosition:space.playerRig.position;
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
                // Walk through the real gym entrance and sliding doorway, not through a wall.
                var friendThreshold=new Vector3(-1.15f,ThirdFloorHeight,-4.2f);
                Lesson.friend.position=ElevatorOpeningSequence.WalkPath(friendGymStart,friendThreshold,friendLobby,t,0,6.7f,out var direction);
                var walkingRotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
                Lesson.friend.rotation=t<6.1f?Quaternion.Slerp(Quaternion.identity,walkingRotation,S(t,0,.6f)):
                    Quaternion.Slerp(walkingRotation,Quaternion.identity,S(t,6.1f,7.0f));
                if(XRSettings.isDeviceActive)
                {
                    if(t>=ElevatorOpeningSequence.TeleportAt&&!teleported){MoveRig(lobby);teleported=true;}
                }
                else
                {
                    var threshold=new Vector3(0,ThirdFloorHeight,-4.2f);
                    MoveRig(ElevatorOpeningSequence.WalkPath(gymStart,threshold,lobby,t,.4f,7.1f));
                    ApplyOpeningView(ElevatorOpeningSequence.ViewYaw(gymStart,threshold,lobby,t,.4f,7.1f,false));
                }
            }
            else if(f.Step==ElevatorStep.Entering)
            {
                DoorOpen=S(t,.25f,1.15f)*(1-S(t,4.8f,5.75f));
                Vector3 mid=new Vector3(-1.8f,ThirdFloorHeight,3.6f);
                Lesson.friend.position=t<2.4f?Vector3.Lerp(friendLobby,mid,S(t,.9f,2.4f)):Vector3.Lerp(mid,friendInside,S(t,2.4f,3.65f));
                Vector3 direction=t<2.4f?mid-friendLobby:friendInside-mid;
                var walkingRotation=Quaternion.LookRotation(-direction.normalized,Vector3.up);
                Lesson.friend.rotation=t<3.65f?Quaternion.Slerp(Quaternion.identity,walkingRotation,S(t,.65f,1.0f)):Quaternion.Slerp(walkingRotation,Quaternion.Euler(0,180,0),S(t,3.65f,4.15f));
                if(XRSettings.isDeviceActive){if(t>=3.1f&&!teleported){MoveRig(inside);ApplyEntryTurn(1);teleported=true;}}
                else
                {
                    var approach=new Vector3(-1.8f,ThirdFloorHeight,2.2f);
                    var position=t<2.5f?Vector3.Lerp(transitionStart,approach,S(t,1.2f,2.5f)):Vector3.Lerp(approach,inside,S(t,2.5f,4.5f));
                    MoveRig(position);
                    teleported=t>=4.5f;
                    // Boarding contributes a fixed turn while the player's look stays additive.
                    if(t>=3.4f&&!entryTurnComplete)
                    {
                        ApplyEntryTurn(S(t,3.4f,4.7f));
                    }
                }
            }
            else if(f.Step==ElevatorStep.Rescue)DoorOpen=S(t,.4f,2.1f);
            else if(f.Step==ElevatorStep.Exiting)
            {
                DoorOpen=1-S(t,3.9f,4.6f);
                var mid=new Vector3(-1.8f,ThirdFloorHeight,3.5f);
                Lesson.friend.position=t<1.7f?Vector3.Lerp(friendInside,mid,S(t,.15f,1.7f)):Vector3.Lerp(mid,friendLobby,S(t,1.7f,3.1f));
                var exitDirection=t<1.7f?mid-friendInside:friendLobby-mid;
                Lesson.friend.rotation=Quaternion.Slerp(Quaternion.Euler(0,180,0),Quaternion.LookRotation(-exitDirection.normalized,Vector3.up),S(t,0,.25f));
                if(XRSettings.isDeviceActive){if(t>=2.3f&&!teleported){MoveRig(lobby);teleported=true;}}
                else
                {
                    var threshold=new Vector3(-1.8f,ThirdFloorHeight,3.65f);
                    var position=t<1.8f?Vector3.Lerp(transitionStart,threshold,S(t,.65f,1.8f)):Vector3.Lerp(threshold,lobby,S(t,1.8f,3.95f));
                    MoveRig(position);
                    teleported=t>=3.95f;
                }
            }
            else if(f.Step==ElevatorStep.Returning)
            {
                if(t>=1.75f&&lastFloor>1)MoveRig(groundLobby);
            }
            else if(f.Step==ElevatorStep.GoingHome)
            {
                if(XRSettings.isDeviceActive){if(t>=3&&!teleported){MoveRig(groundExit);teleported=true;}}
                else {MoveRig(Vector3.Lerp(transitionStart,groundExit,S(t,.7f,5.3f)));teleported=t>=5.3f;}
                Lesson.friend.position=Vector3.Lerp(new Vector3(-.2f,0,-2.4f),new Vector3(.1f,0,-8.4f),S(t,.2f,4.9f));
                Lesson.friend.rotation=Quaternion.Euler(0,Mathf.Lerp(0,180,S(t,4.6f,5.8f)),0);
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
            else if(f.Step==ElevatorStep.GymDeparture)thirdFloorDoorOpen=S(t,0,.9f)*(1-S(t,5.9f,6.9f));
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
            if(previous==ElevatorStep.Entering&&f.Step==ElevatorStep.Boarding&&!entryTurnComplete)
            {ApplyEntryTurn(1);}
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
