using UnityEngine;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator
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
            p.lobby.SetActive(true);lesson.transform.Find("Cabin").gameObject.SetActive(true);
            if(reset)MoveRig(outside?(f.Step==ElevatorStep.Lobby?lobbyPosition:exitPosition):insidePosition,true,outside?180:0);
            float height=lesson.friend.localPosition.y;
            if(outside)lesson.friend.localPosition=friendOutside+Vector3.up*height;
            lesson.friend.localRotation=Quaternion.Euler(0,outside?180:0,0);
            DoorOpen=0;
            if(f.Step==ElevatorStep.Entering)
            {
                DoorOpen=S(t,.25f,1.15f)*(1-S(t,4.8f,5.75f));
                var midpoint=new Vector3(.12f,0,3.0f);
                lesson.friend.localPosition=t<2.2f?Vector3.Lerp(friendOutside,midpoint,S(t,.9f,2.2f)):Vector3.Lerp(midpoint,friendInside,S(t,2.2f,3.5f));
                lesson.friend.localRotation=Quaternion.Euler(0,180*(1-S(t,.65f,1.15f)),0);
                if(XRSettings.isDeviceActive)MoveRig(t<3.1f?lobbyPosition:insidePosition);
                else MoveRig(Vector3.Lerp(lobbyPosition,insidePosition,S(t,1.2f,4.5f)));
            }
            else if(f.Step==ElevatorStep.Exiting)
            {
                DoorOpen=1-S(t,3.4f,4.5f);
                var midpoint=new Vector3(.05f,0,3.05f);
                lesson.friend.localPosition=t<1.65f?Vector3.Lerp(friendInside,midpoint,S(t,.15f,1.65f)):Vector3.Lerp(midpoint,friendOutside,S(t,1.65f,3.1f));
                lesson.friend.localRotation=Quaternion.Euler(0,180,0);
                if(XRSettings.isDeviceActive)MoveRig(t<2.3f?insidePosition:exitPosition);
                else MoveRig(Vector3.Lerp(insidePosition,exitPosition,S(t,.65f,3.95f)));
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
            lastRun=lesson.RunVersion;
        }
    }
}
