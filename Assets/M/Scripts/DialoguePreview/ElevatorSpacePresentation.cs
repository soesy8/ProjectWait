// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorSpacePresentation.cs. Original is not modified.
using UnityEngine;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator.DialoguePreview
{
    // A single connected lobby and cabin. The playable position is the cabin's centre.
    public sealed class ElevatorSpacePresentation : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Transform playerRig;
        public Vector3 insidePosition=Vector3.zero, lobbyPosition=new Vector3(0,0,6.8f), exitPosition=new Vector3(0,0,6.3f);
        public Vector3 friendInside=new Vector3(-.90f,0,1.95f), friendOutside=new Vector3(.85f,0,4.7f);
        public float doorZ=2.84f, doorY=1.16f;
        public ElevatorCinematicOverlay overlay;
        public ElevatorSportsCentreMap sportsCentre;
        ElevatorDesktopControls controls;
        int lastRun=-1;
        ElevatorStep previous=(ElevatorStep)(-1);bool placed;
        public float DefaultHeading => sportsCentre?sportsCentre.DefaultHeading:lesson&&lesson.Flow!=null&&(lesson.Flow.Step==ElevatorStep.Lobby||lesson.Flow.Step==ElevatorStep.Complete||lesson.Flow.Step==ElevatorStep.Entering)?180:0;
        public float DoorOpen {get;private set;}
        static float S(float t,float start,float end)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,t));
        void MoveRig(Vector3 position,bool resetHeading=false,float heading=0)
        {
            if(!controls)controls=FindFirstObjectByType<ElevatorDesktopControls>();
            playerRig.position=new Vector3(position.x,playerRig.position.y,position.z);
            if(resetHeading)
            {
                if(XRSettings.isDeviceActive)playerRig.rotation=Quaternion.Euler(0,heading,0);
                else{playerRig.rotation=Quaternion.identity;controls.SetScriptedHeading(heading);}
            }
            lesson.view.Reanchor(lesson.playerCamera.transform.position);
        }
        public void Apply()
        {
            if (sportsCentre) { sportsCentre.Apply(); return; }
            var f=lesson.Flow;var p=lesson.presentation;float t=f.Elapsed;
            bool outside=f.Step==ElevatorStep.Lobby||f.Step==ElevatorStep.Complete;
            bool reset=lastRun!=lesson.RunVersion;
            if(reset||previous!=f.Step)placed=false;
            p.lobby.SetActive(true);lesson.transform.Find("Cabin").gameObject.SetActive(true);
            if(reset)MoveRig(outside?(f.Step==ElevatorStep.Lobby?lobbyPosition:exitPosition):insidePosition,true,outside?180:0);
            float height=lesson.friend.localPosition.y;
            if(outside)lesson.friend.localPosition=friendOutside+Vector3.up*height;
            lesson.friend.localRotation=Quaternion.Euler(0,outside?180:0,0);
            DoorOpen=0;
            if(f.Step==ElevatorStep.Entering)
            {
                if(!placed&&t>=ProjectWait.M.Shared.ComfortBlackout.Cover){MoveRig(insidePosition);controls?.FaceAuthoredHeading(0);placed=true;}
                DoorOpen=placed?0:1;lesson.friend.localPosition=placed?friendInside:friendOutside;
            }
            else if(f.Step==ElevatorStep.Exiting)
            {
                if(!placed&&t>=ProjectWait.M.Shared.ComfortBlackout.Cover){MoveRig(exitPosition);controls?.FaceAuthoredHeading(180);placed=true;}
                DoorOpen=placed?0:1;lesson.friend.localPosition=placed?friendOutside:friendInside;
            }
            else if(f.Step==ElevatorStep.Rescue)DoorOpen=S(t,.35f,2.1f);
            if(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting)lesson.friend.localPosition+=Vector3.up*(Mathf.Abs(Mathf.Sin(t*7))*.012f);
            float keyYaw=outside?150:-30;
            if(f.Step==ElevatorStep.Entering)keyYaw=Mathf.Lerp(150,330,S(t,3.4f,4.7f));
            if(f.Step==ElevatorStep.Exiting)keyYaw=Mathf.Lerp(-30,150,S(t,3.15f,4.25f));
            lesson.cabinLight.transform.rotation=Quaternion.Euler(35,keyYaw,0);
            p.leftDoor.localPosition=new Vector3(-.415f-DoorOpen*.87f,doorY,doorZ);
            p.rightDoor.localPosition=new Vector3(.415f+DoorOpen*.87f,doorY,doorZ);
            p.closedDoorCollider.enabled=DoorOpen<.1f;
            bool rescue=f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Complete;
            p.rescuer.SetActive(rescue);
            p.rescuer.transform.position=f.Step==ElevatorStep.Complete?new Vector3(-.95f,0,4.65f):new Vector3(-.85f,0,4.1f);
            p.rescuer.transform.rotation=Quaternion.Euler(0,f.Step==ElevatorStep.Complete?180:0,0);
            lastRun=lesson.RunVersion;previous=f.Step;
        }
    }
}
