using System.Linq;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    public sealed class ElevatorStoryPresentation : MonoBehaviour
    {
        public GameObject lobby, exitHall, rescuer;
        public Transform leftDoor, rightDoor, friendBall, playerBall;
        public Collider closedDoorCollider;
        public ElevatorLesson lesson;
        public Transform intercomButton, friendShadow;
        public Transform reactionMarks;
        public Renderer intercomLamp;
        public ElevatorSpacePresentation space;
        Transform leftArm,leftForearm,leftHand,rightArm,rightForearm,rightHand,head,pelvis,leftThigh,rightThigh,leftShin,rightShin;
        Quaternion[] rest; Transform[] joints;
        Vector3 home,ballHome;
        SkinnedMeshRenderer face;
        int blinkIndex=-1,surpriseIndex=-1;
        AudioSource sound;
        AudioClip catchSound,whoosh,connected,success,arrival;
        ElevatorStep lastStep=(ElevatorStep)(-1);
        int lastRun=-1,lastEncouragements;
        bool lastLocation,lastReassured,lastIdentity;
        Vector3 lampScale;
        Transform rescueArm,rescueForearm,rescueHand;
        Quaternion rescueUpperRest,rescueLowerRest;
        void Awake()
        {
            if(sound)return;
            home=space?space.friendInside:lesson.friend.localPosition;ballHome=friendBall.localPosition;
            var bones=lesson.friend.GetComponentsInChildren<Transform>(true);
            leftArm=Bone(bones,"upper_arm.L");leftForearm=Bone(bones,"forearm.L");leftHand=Bone(bones,"hand.L");
            rightArm=Bone(bones,"upper_arm.R");rightForearm=Bone(bones,"forearm.R");rightHand=Bone(bones,"hand.R");
            head=Bone(bones,"head");pelvis=Bone(bones,"pelvis");leftThigh=Bone(bones,"thigh.L");rightThigh=Bone(bones,"thigh.R");leftShin=Bone(bones,"shin.L");rightShin=Bone(bones,"shin.R");
            joints=new[]{leftArm,leftForearm,leftHand,rightArm,rightForearm,rightHand,head,pelvis,leftThigh,rightThigh,leftShin,rightShin};rest=joints.Select(t=>t?t.localRotation:Quaternion.identity).ToArray();
            face=lesson.friend.GetComponentInChildren<SkinnedMeshRenderer>();
            if(face){blinkIndex=face.sharedMesh.GetBlendShapeIndex("Blink");surpriseIndex=face.sharedMesh.GetBlendShapeIndex("Surprised");}
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;sound.volume=.16f;
            catchSound=Tone("Soft catch",new[]{220f,165f},.11f);
            whoosh=Tone("Shot swish",new[]{380f,520f,660f},.09f);
            connected=Tone("Connected",new[]{523.25f,659.25f},.17f);
            success=Tone("Team response",new[]{659.25f,783.99f,1046.5f},.12f);
            arrival=Tone("Safe arrival",new[]{523.25f,659.25f,783.99f,1046.5f},.20f);
            if(intercomButton)lampScale=intercomButton.localScale;
            var rescueBones=rescuer.GetComponentsInChildren<Transform>(true);
            rescueArm=Bone(rescueBones,"upper_arm.L");rescueForearm=Bone(rescueBones,"forearm.L");rescueHand=Bone(rescueBones,"hand.L");
            if(rescueArm){rescueUpperRest=rescueArm.localRotation;rescueLowerRest=rescueForearm.localRotation;}
        }
        static Transform Bone(Transform[] bones,string name)=>bones.FirstOrDefault(t=>t.name==name);
        public void Apply(ElevatorLesson value)
        {
            if(!sound)Awake();
            var f=value.Flow;bool outside=f.Step==ElevatorStep.Lobby||f.Step==ElevatorStep.Complete;
            if(!space)
            {
                lobby.SetActive(outside);exitHall.SetActive(f.Step==ElevatorStep.Rescue);rescuer.SetActive(f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.Complete);
                if(f.Step==ElevatorStep.Complete)rescuer.transform.position=new Vector3(.65f,0,1.10f);
                else rescuer.transform.position=new Vector3(.20f,0,Mathf.Lerp(3.7f,2.65f,Mathf.SmoothStep(0,1,(f.Elapsed-.8f)/1.4f)));
            }
            friendBall.gameObject.SetActive(!f.PlayerHasBall);playerBall.gameObject.SetActive(f.PlayerHasBall);
            friendBall.localPosition=ballHome+Vector3.up*(Mathf.Sin(f.Elapsed*2.2f)*.007f);
            float open=f.Step==ElevatorStep.Rescue?Mathf.SmoothStep(0,1,(f.Elapsed-.35f)/1.8f):0;
            if(!space){leftDoor.localPosition=new Vector3(-.41f-open*.80f,1.10f,1.74f);rightDoor.localPosition=new Vector3(.41f+open*.80f,1.10f,1.74f);closedDoorCollider.enabled=f.Step!=ElevatorStep.Rescue;}
            if(f.Step==ElevatorStep.Waiting&&!f.Reassured)value.friend.localPosition=Vector3.Lerp(home,home+new Vector3(.25f,0,.30f),Mathf.SmoothStep(0,1,f.Elapsed/2));
            if(f.Step==ElevatorStep.Proposal&&!(space&&space.sportsCentre))value.friend.localPosition=home+Vector3.down*(Mathf.Sin(f.Elapsed/.9f*Mathf.PI)*.06f);
            if(f.Step!=ElevatorStep.Jump&&f.Step!=ElevatorStep.Proposal)value.friend.localPosition+=Vector3.up*(Mathf.Sin(f.Elapsed*2.5f)*.006f);
            if(space)space.Apply();
            var held=value.playerCamera.transform.TransformPoint(value.playerCamera.aspect>=1.6f?new Vector3(-.62f,-.47f,.95f):new Vector3(-.68f,-.60f,.95f));
            float handoff=f.Step==ElevatorStep.Proposal?Mathf.SmoothStep(0,1,f.Elapsed/.7f):1;
            playerBall.SetPositionAndRotation(Vector3.Lerp(friendBall.position,held,handoff),value.playerCamera.transform.rotation*Quaternion.Euler(0,0,25*handoff));
            if(friendShadow&&!(space&&space.sportsCentre)){friendShadow.position=new Vector3(value.friend.position.x,.012f,value.friend.position.z);friendShadow.localScale=new Vector3(.58f,.008f,.34f)*(1-Mathf.Max(0,value.friend.position.y)*.6f);}
            if(intercomButton)intercomButton.localScale=lampScale*(f.Step==ElevatorStep.Connecting?1+Mathf.Sin(f.Elapsed*7)*.04f:1);
            if(reactionMarks)
            {
                reactionMarks.gameObject.SetActive(f.Step==ElevatorStep.Jump||(f.Step==ElevatorStep.Stopped&&f.Phase==InteractionPhase.Dialogue));
                reactionMarks.localScale=Vector3.one*(1+Mathf.Sin(f.Elapsed*8)*.08f);
            }
            if(lastRun!=value.RunVersion){lastRun=value.RunVersion;lastStep=f.Step;lastEncouragements=f.Encouragements;lastLocation=f.LocationReported;lastReassured=f.Reassured;lastIdentity=f.IdentityFound;sound.Stop();}
            if(lastStep!=f.Step)
            {
                if(f.Step==ElevatorStep.Proposal)Play(catchSound);
                if(f.Step==ElevatorStep.Jump)Play(whoosh);
                if(f.Step==ElevatorStep.Connecting||f.Step==ElevatorStep.Call)Play(connected);
                if(f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.Complete)Play(arrival);
                lastStep=f.Step;
            }
            if(lastIdentity!=f.IdentityFound||lastLocation!=f.LocationReported||lastReassured!=f.Reassured||lastEncouragements!=f.Encouragements)Play(success);
            lastIdentity=f.IdentityFound;lastLocation=f.LocationReported;lastReassured=f.Reassured;lastEncouragements=f.Encouragements;
        }
        void LateUpdate()
        {
            if(lesson.IsPaused)return;
            if(lesson.Flow==null||rest==null)return;
            for(int i=0;i<joints.Length;i++)if(joints[i])joints[i].localRotation=rest[i];
            var f=lesson.Flow;var p=lesson.friend.position;
            bool shooting=f.Step==ElevatorStep.Jump,holding=!f.PlayerHasBall,reaching=f.Step==ElevatorStep.Waiting&&!f.Reassured;
            float lift=f.Step==ElevatorStep.Jump?Mathf.SmoothStep(0,1,(f.Elapsed-lesson.settings.friendReplySeconds+.25f)/.4f):0;
            if(shooting)
            {
                PoseArm(leftArm,leftForearm,leftHand,p+lesson.friend.rotation*new Vector3(.30f,.76f+lift*.22f,-.08f),p+lesson.friend.rotation*new Vector3(.24f,.95f+lift*.22f,-.14f));
                PoseArm(rightArm,rightForearm,rightHand,p+lesson.friend.rotation*new Vector3(-.30f,.76f+lift*.22f,-.08f),p+lesson.friend.rotation*new Vector3(-.24f,.95f+lift*.22f,-.14f));
                if(head)head.localRotation*=Quaternion.Euler(-lift*8,0,0);
                float crouch=f.Step==ElevatorStep.Proposal?Mathf.Sin(f.Elapsed/.9f*Mathf.PI):Mathf.Clamp01(1-lift);
                if(leftThigh)leftThigh.localRotation*=Quaternion.Euler(0,0,crouch*14);
                if(rightThigh)rightThigh.localRotation*=Quaternion.Euler(0,0,crouch*14);
                if(leftShin)leftShin.localRotation*=Quaternion.Euler(0,0,-crouch*22);
                if(rightShin)rightShin.localRotation*=Quaternion.Euler(0,0,-crouch*22);
            }
            else if(holding)
            {
                PoseArm(leftArm,leftForearm,leftHand,p+lesson.friend.rotation*new Vector3(.25f,.69f,-.12f),p+lesson.friend.rotation*new Vector3(.13f,.71f,-.22f));
                PoseArm(rightArm,rightForearm,rightHand,p+lesson.friend.rotation*new Vector3(-.25f,.69f,-.12f),p+lesson.friend.rotation*new Vector3(-.13f,.71f,-.22f));
                if(head)head.localRotation*=Quaternion.Euler(0,Mathf.Sin(f.Elapsed*1.3f)*3,Mathf.Sin(f.Elapsed*1.8f)*3);
            }
            else if(reaching)PoseArm(leftArm,leftForearm,leftHand,p+lesson.friend.rotation*new Vector3(.28f,.84f,.12f),p+lesson.friend.rotation*new Vector3(.40f,.94f,.36f));
            else if(f.Step==ElevatorStep.Stopped||f.Step==ElevatorStep.Connecting)
            {
                PoseArm(leftArm,leftForearm,leftHand,p+lesson.friend.rotation*new Vector3(.30f,.76f,-.10f),p+lesson.friend.rotation*new Vector3(.32f,.90f,-.15f));
                if(head)head.localRotation*=Quaternion.Euler(0,Mathf.Sin(f.Elapsed*.9f)*6,-5);
            }
            else if(f.Step==ElevatorStep.Waiting&&f.Reassured)
            {
                PoseArm(leftArm,leftForearm,leftHand,p+lesson.friend.rotation*new Vector3(.30f,.79f,-.09f),p+lesson.friend.rotation*new Vector3(.36f,.95f+Mathf.Sin(f.Elapsed*3)*.025f,-.15f));
                if(head)head.localRotation*=Quaternion.Euler(Mathf.Sin(f.Elapsed*3)*3,0,3);
            }
            if(!lesson.GetComponent<ElevatorCharacterPerformance>()&&(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting))
            {
                float walk=Mathf.Sin(f.Elapsed*7)*16;
                if(leftThigh)leftThigh.localRotation*=Quaternion.Euler(0,0,walk);if(rightThigh)rightThigh.localRotation*=Quaternion.Euler(0,0,-walk);
            }
            if(face)
            {
                float blink=Mathf.Abs((Time.unscaledTime%4.2f)-4.05f);if(blinkIndex>=0)face.SetBlendShapeWeight(blinkIndex,Mathf.Clamp01(1-blink/.12f)*100);
                if(surpriseIndex>=0)face.SetBlendShapeWeight(surpriseIndex,(f.Step==ElevatorStep.Stopped||f.Step==ElevatorStep.Connecting)?100:0);
            }
            if(rescueArm&&rescuer.activeInHierarchy)
            {
                rescueArm.localRotation=rescueUpperRest;rescueForearm.localRotation=rescueLowerRest;
                var rp=rescuer.transform.position;
                PoseArm(rescueArm,rescueForearm,rescueHand,rp+rescuer.transform.rotation*new Vector3(.37f,1.09f,-.10f),rp+rescuer.transform.rotation*new Vector3(.45f,1.29f+Mathf.Sin(f.Elapsed*3)*.025f,-.20f));
            }
        }
        static void PoseArm(Transform upper,Transform lower,Transform hand,Vector3 elbow,Vector3 target)
        {
            if(!upper||!lower||!hand)return;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
        }
        void Play(AudioClip clip){if(sound&&clip)sound.PlayOneShot(clip);}
        static AudioClip Tone(string name,float[] notes,float duration)
        {
            const int rate=22050;int count=(int)(rate*duration);var samples=new float[count*notes.Length];
            for(int n=0;n<notes.Length;n++)for(int i=0;i<count;i++){float t=i/(float)rate,env=Mathf.Sin(Mathf.PI*i/count);samples[n*count+i]=(Mathf.Sin(2*Mathf.PI*notes[n]*t)+.18f*Mathf.Sin(4*Mathf.PI*notes[n]*t))*env*env*.42f;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){foreach(var clip in new[]{catchSound,whoosh,connected,success,arrival})if(clip)Destroy(clip);}
    }
}
