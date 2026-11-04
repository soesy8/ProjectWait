using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProjectWait.M.Elevator
{
    [DefaultExecutionOrder(180)]
    public sealed class ElevatorPosterCutscene : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Transform posterAnchor;
        public Collider posterSurface;
        public Canvas screenCanvas,worldCanvas;
        public RectTransform worldRing,arrow,instructionPanel;
        public ElevatorPosterCueGraphic ringGraphic;
        public TMP_Text instruction;
        [Min(.1f)] public float holdSeconds=1.5f,pulseSeconds=1.5f;
        [Range(0,1)] public float pulseGrowth=.20f;
        [Min(.01f)] public float targetRadius=.19f;
        public float HoldElapsed {get;private set;}
        public float PulseScale {get;private set;}=1;
        public bool ArrowClamped {get;private set;}
        public bool Awaiting => lesson&&lesson.Flow!=null&&lesson.Flow.AwaitingPoster;
        public Vector3 TargetPosition => posterAnchor.position;
        ElevatorDesktopControls desktop;
        XRBaseInteractor[] hands;
        int run=-1;
        ElevatorStep step=(ElevatorStep)(-1);
        float pulseElapsed;

        void Awake()
        {
            desktop=FindFirstObjectByType<ElevatorDesktopControls>();
            hands=FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(i=>i is XRPokeInteractor||i is XRBaseInputInteractor&&!(i is XRGazeInteractor)).ToArray();
            Hide();
        }
        void LateUpdate(){Advance(Time.unscaledDeltaTime);}
        void Hide()
        {
            if(screenCanvas)screenCanvas.gameObject.SetActive(false);
            if(worldCanvas)worldCanvas.gameObject.SetActive(false);
        }
        public bool HitsTarget(Ray ray,out float distance)
        {
            distance=0;if(!Awaiting||!posterAnchor)return false;
            var facing=(TargetPosition-lesson.playerCamera.transform.position).normalized;
            if(!new Plane(facing,TargetPosition).Raycast(ray,out distance)||distance<lesson.playerCamera.nearClipPlane)return false;
            if((ray.GetPoint(distance)-TargetPosition).sqrMagnitude>targetRadius*targetRadius)return false;
            // Solid geometry in front of the marker must still occlude pointing.
            if(Physics.Raycast(ray,out var hit,distance-.025f,~0,QueryTriggerInteraction.Ignore)&&hit.collider!=posterSurface)return false;
            return true;
        }
        bool HandAtTarget()
        {
            if(hands==null)return false;
            var normal=(TargetPosition-lesson.playerCamera.transform.position).normalized;
            foreach(var hand in hands)
            {
                if(!hand||!hand.isActiveAndEnabled)continue;
                Vector3 offset=hand.transform.position-TargetPosition;
                if(Mathf.Abs(Vector3.Dot(offset,normal))<.13f&&Vector3.ProjectOnPlane(offset,normal).sqrMagnitude<targetRadius*targetRadius)return true;
            }
            return false;
        }
        public void Advance(float seconds)
        {
            if(!lesson||lesson.Flow==null||!posterAnchor||!screenCanvas||!worldCanvas)return;
            var f=lesson.Flow;
            if(run!=lesson.RunVersion||step!=f.Step)
            {run=lesson.RunVersion;step=f.Step;HoldElapsed=pulseElapsed=0;ringGraphic.SetProgress(0);}
            if(lesson.IsPaused){Hide();return;}
            seconds=Mathf.Max(0,seconds);
            bool pointing=f.AwaitingPoster;
            // A brief cue back to the friend lets the player watch the shot without owning their gaze.
            bool watchFriend=f.Step==ElevatorStep.Jump&&f.Elapsed<lesson.settings.friendReplySeconds;
            screenCanvas.gameObject.SetActive(pointing||watchFriend);
            worldCanvas.gameObject.SetActive(pointing);
            if(!pointing&&!watchFriend)return;
            PositionScreenCanvas();
            if(pointing)
            {
                pulseElapsed+=seconds;
                PulseScale=1+pulseGrowth*(.5f-.5f*Mathf.Cos(pulseElapsed*Mathf.PI*2/pulseSeconds));
                worldCanvas.worldCamera=lesson.playerCamera;
                worldRing.SetPositionAndRotation(TargetPosition,Quaternion.LookRotation(TargetPosition-lesson.playerCamera.transform.position,Vector3.up));
                worldRing.localScale=Vector3.one*(.003125f*PulseScale);
                bool hit=XRSettings.isDeviceActive?HandAtTarget():desktop&&desktop.isActiveAndEnabled&&HitsTarget(lesson.playerCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out _);
                HoldElapsed=hit?HoldElapsed+seconds:0;
                ringGraphic.SetProgress(XRSettings.isDeviceActive?(hit?1:0):HoldElapsed/holdSeconds);
                instruction.text=XRSettings.isDeviceActive?"안전 포스터 앞의 원에 손을 대어 가리켜요.":"마우스로 둘러보고, 포스터의 원을\n중앙 레이저로 1.5초 동안 가리켜요.";
                PlaceArrow(TargetPosition);
                if(hit&&(XRSettings.isDeviceActive||HoldElapsed>=holdSeconds))
                {
                    f.PointAtSafetyPoster();lesson.Advance(0);Hide();HoldElapsed=0;
                }
            }
            else
            {
                instruction.text="친구가 하려는 행동을 살펴봐요.";
                PlaceArrow(lesson.friend.position+Vector3.up*1.05f);
            }
        }
        void PositionScreenCanvas()
        {
            bool xr=XRSettings.isDeviceActive;
            screenCanvas.renderMode=xr?RenderMode.WorldSpace:RenderMode.ScreenSpaceOverlay;
            screenCanvas.worldCamera=lesson.playerCamera;
            if(!xr)return;
            const float distance=.44f;
            var rect=(RectTransform)screenCanvas.transform;
            rect.sizeDelta=new Vector2(720*lesson.playerCamera.aspect,720);
            rect.SetPositionAndRotation(lesson.playerCamera.transform.position+lesson.playerCamera.transform.forward*distance,lesson.playerCamera.transform.rotation);
            rect.localScale=Vector3.one*(2*distance*Mathf.Tan(lesson.playerCamera.fieldOfView*.5f*Mathf.Deg2Rad)/720);
        }
        void PlaceArrow(Vector3 target)
        {
            var root=(RectTransform)screenCanvas.transform;Vector2 size=root.rect.size;
            var view=lesson.playerCamera.WorldToViewportPoint(target);
            Vector2 point=new Vector2((view.x-.5f)*size.x,(view.y-.5f)*size.y);
            Vector2 direction=point;
            if(view.z<=0)
            {
                var local=lesson.playerCamera.transform.InverseTransformPoint(target);
                direction=new Vector2(local.x,local.y);
                if(direction.sqrMagnitude<.001f)direction=Vector2.right;
            }
            if(direction.sqrMagnitude<.001f)direction=Vector2.up;
            direction.Normalize();
            Vector2 half=size*.5f-new Vector2(64,112);
            ArrowClamped=view.z<=0||Mathf.Abs(point.x)>half.x||Mathf.Abs(point.y)>half.y;
            Vector2 placement=point-direction*72;
            if(ArrowClamped)
            {
                float edge=Mathf.Min(half.x/Mathf.Max(.001f,Mathf.Abs(direction.x)),half.y/Mathf.Max(.001f,Mathf.Abs(direction.y)));
                placement=direction*edge;
            }
            placement.x=Mathf.Clamp(placement.x,-half.x,half.x);placement.y=Mathf.Clamp(placement.y,-half.y,half.y);
            arrow.anchoredPosition=placement;
            arrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            instructionPanel.sizeDelta=new Vector2(Mathf.Min(610,size.x-110),90);
        }
        void OnDisable(){HoldElapsed=0;Hide();}
    }
}
