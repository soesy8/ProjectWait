using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator
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
        public bool CanMove=>lesson&&lesson.Flow!=null&&!lesson.IsPaused&&!lesson.Flow.IsCinematic;
        public string VRBinding=>"<XRController>{LeftHand}/primary2DAxis";
        void OnEnable()
        {
            Controller=GetComponent<CharacterController>();
            keyboard=new InputAction("ElevatorWalk",InputActionType.Value);
            keyboard.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            leftStick=new InputAction("ElevatorLeftStick",InputActionType.Value,VRBinding,processors:"stickDeadzone(min=0.18,max=0.95)");
            keyboard.Enable();leftStick.Enable();
        }
        public void CalibrateFeet(float floorHeight)
        {
            FeetOffset=floorHeight-transform.position.y;verticalSpeed=0;
            if(Controller)Controller.center=new Vector3(0,Controller.height*.5f+FeetOffset,0);
        }
        public void ResetVerticalMotion(){verticalSpeed=0;}
        void Update(){Move(XRSettings.isDeviceActive?leftStick.ReadValue<Vector2>():keyboard.ReadValue<Vector2>(),Time.deltaTime,XRSettings.isDeviceActive);}
        public void Move(Vector2 input,float seconds,bool vr)
        {
            if(!CanMove||!Controller||!Controller.enabled||seconds<=0){LastInput=Vector2.zero;verticalSpeed=0;return;}
            LastInput=Vector2.ClampMagnitude(input,1);
            Vector3 forward=Vector3.ProjectOnPlane(playerCamera.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.1f)forward=transform.forward;
            Vector3 right=Vector3.Cross(Vector3.up,forward).normalized;
            if(Controller.isGrounded&&verticalSpeed<0)verticalSpeed=-2;
            else verticalSpeed=Mathf.Max(-20,verticalSpeed+gravity*seconds);
            Vector3 horizontal=(forward*LastInput.y+right*LastInput.x)*(vr?vrSpeed:desktopSpeed);
            Controller.Move((horizontal+Vector3.up*verticalSpeed)*seconds);
        }
        void OnDisable(){keyboard?.Dispose();leftStick?.Dispose();verticalSpeed=0;LastInput=Vector2.zero;}
    }
}
