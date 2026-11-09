// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorPlayerMovement.cs. Original is not modified.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ElevatorPlayerMovement : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Camera playerCamera;
        public float desktopSpeed=2.2f, vrSpeed=1.65f, gravity=-15;
        public float FeetOffset {get;private set;}
        public Vector3 FeetPosition=>transform.position+Vector3.up*FeetOffset;
        public Vector2 LastInput {get;private set;}
        public CharacterController Controller {get;private set;}
        InputAction keyboard, leftStick;
        float verticalSpeed;
        Vector3 safeHeadPosition;
        bool hasSafeHead;
        readonly RaycastHit[] headHits=new RaycastHit[32];
        public bool CanMove=>lesson&&lesson.Flow!=null&&!lesson.CinematicBlocking&&!lesson.Flow.IsCinematic;
        public string VRBinding=>"<XRController>{LeftHand}/primary2DAxis";
        void OnEnable()
        {
            Controller=GetComponent<CharacterController>();
            keyboard=new InputAction("ElevatorWalk",InputActionType.Value);
            keyboard.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            leftStick=new InputAction("ElevatorLeftStick",InputActionType.Value,VRBinding,processors:"stickDeadzone(min=0.18,max=0.95)");
            keyboard.Enable();leftStick.Enable();
            hasSafeHead=false;Application.onBeforeRender+=ProtectTrackedHead;
        }
        public void CalibrateFeet(float floorHeight)
        {
            FeetOffset=floorHeight-transform.position.y;verticalSpeed=0;hasSafeHead=false;
            if(Controller)Controller.center=new Vector3(0,Controller.height*.5f+FeetOffset,0);
        }
        public void ResetVerticalMotion(){verticalSpeed=0;hasSafeHead=false;}
        void Update(){Move(XRSettings.isDeviceActive?leftStick.ReadValue<Vector2>():keyboard.ReadValue<Vector2>(),Time.deltaTime,XRSettings.isDeviceActive);}
        void LateUpdate(){ProtectTrackedHead();}
        [BeforeRenderOrder(100)]
        void ProtectTrackedHead()
        {
            if(!XRSettings.isDeviceActive||!Controller||!Controller.enabled||!playerCamera)return;
            ConstrainHead();UpdateCapsule(true);
        }
        void UpdateCapsule(bool vr)
        {
            var local=vr?transform.InverseTransformPoint(playerCamera.transform.position):Vector3.zero;
            Controller.center=new Vector3(local.x,Controller.height*.5f+FeetOffset,local.z);
        }
        void ConstrainHead()
        {
            var current=playerCamera.transform.position;
            if(hasSafeHead)
            {
                var delta=current-safeHeadPosition;float length=delta.magnitude;
                float radius=Mathf.Max(.08f,playerCamera.nearClipPlane+.035f);
                if(length>.00001f)
                {
                    int count=Physics.SphereCastNonAlloc(safeHeadPosition,radius,delta/length,headHits,length,~0,QueryTriggerInteraction.Ignore);
                    float allowed=length;
                    for(int i=0;i<count;i++)
                    {
                        var collider=headHits[i].collider;
                        if(!collider||collider.transform.IsChildOf(transform))continue;
                        allowed=Mathf.Min(allowed,Mathf.Max(0,headHits[i].distance-.005f));
                    }
                    if(allowed<length)
                    {
                        // Offset the origin, never the tracked camera; tracking remains live.
                        bool wasEnabled=Controller.enabled;Controller.enabled=false;
                        transform.position+=safeHeadPosition+delta/length*allowed-current;
                        Controller.enabled=wasEnabled;
                    }
                }
            }
            safeHeadPosition=playerCamera.transform.position;hasSafeHead=true;
        }
        public void Move(Vector2 input,float seconds,bool vr)
        {
            if(!Controller||!Controller.enabled||!playerCamera){LastInput=Vector2.zero;verticalSpeed=0;return;}
            if(vr)ConstrainHead();
            UpdateCapsule(vr);
            if(!CanMove||seconds<=0){LastInput=Vector2.zero;verticalSpeed=0;return;}
            LastInput=Vector2.ClampMagnitude(input,1);
            Vector3 forward=Vector3.ProjectOnPlane(playerCamera.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=transform.forward;
            Vector3 right=Vector3.Cross(Vector3.up,forward).normalized;
            if(Controller.isGrounded&&verticalSpeed<0)verticalSpeed=-2;
            else verticalSpeed=Mathf.Max(-20,verticalSpeed+gravity*seconds);
            Vector3 horizontal=(forward*LastInput.y+right*LastInput.x)*(vr?vrSpeed:desktopSpeed);
            Controller.Move((horizontal+Vector3.up*verticalSpeed)*seconds);
            if(vr)ConstrainHead();
        }
        void OnDisable(){Application.onBeforeRender-=ProtectTrackedHead;hasSafeHead=false;keyboard?.Dispose();leftStick?.Dispose();verticalSpeed=0;LastInput=Vector2.zero;}
    }
}
