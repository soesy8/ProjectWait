// Isolated test snapshot of Assets/M/Scripts/Elevator/ElevatorCinematicOverlay.cs. Original is not modified.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator.ObservationTrial
{
    [DefaultExecutionOrder(200)]
    public sealed class ElevatorCinematicOverlay : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Camera playerCamera;
        public Material overlayMaterial;
        public Material captionMaterial;
        public TMP_FontAsset font;
        [SerializeField] RectTransform top,bottom,fade;
        [SerializeField] TMP_Text caption;
        public float Amount {get;private set;}
        public bool LetterboxVisible => isActiveAndEnabled && Amount > .001f;
        public bool Blocking => lesson&&lesson.Flow!=null&&(lesson.Flow.IsCinematic||Amount>.015f);
        int run=-1;
        public void Build()
        {
            var canvas=GetComponent<Canvas>();if(!canvas)canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=playerCamera;canvas.sortingOrder=100;
            top=Panel("TopLetterbox");bottom=Panel("BottomLetterbox");fade=Panel("XRComfortFade");
            var rect=(RectTransform)new GameObject("CinematicSubtitle",typeof(RectTransform)).transform;rect.SetParent(bottom,false);rect.sizeDelta=new Vector2(1000,64);rect.localPosition=Vector3.back;
            caption=rect.gameObject.AddComponent<TextMeshProUGUI>();caption.font=font;ElevatorTypography.Apply(caption,ElevatorTypography.CaptionSize);caption.color=Color.white;caption.alignment=TextAlignmentOptions.Center;
            if(captionMaterial)caption.fontSharedMaterial=captionMaterial;
            top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);fade.gameObject.SetActive(false);
        }
        RectTransform Panel(string name)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(transform,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
            var image=r.gameObject.AddComponent<Image>();image.color=Color.black;image.raycastTarget=false;image.material=overlayMaterial;return r;
        }
        void OnEnable(){Application.onBeforeRender+=Position;}
        void OnDisable(){Application.onBeforeRender-=Position;}
        void LateUpdate(){Advance(Time.unscaledDeltaTime);}
        public void Advance(float seconds)
        {
            if(lesson&&lesson.IsPaused)return;
            if(!lesson||lesson.Flow==null)return;if(!top)Build();
            if(run!=lesson.RunVersion){run=lesson.RunVersion;Amount=0;}
            Amount=Mathf.MoveTowards(Amount,lesson.Flow.IsCinematic?1:0,seconds/.38f);
            Position();
            top.gameObject.SetActive(Amount>0);bottom.gameObject.SetActive(Amount>0);
            var f=lesson.Flow;
            caption.gameObject.SetActive(!lesson.view.spatial||f.IsShotCutscene||f.IsCallCutscene||f.IsDoorSafetyCutscene||f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GoingHome);
            caption.text=f.Step==ElevatorStep.Entering?"친구와 함께 엘리베이터에 타고 있어요.":f.Step==ElevatorStep.Exiting?"구조요원과 함께 밖으로 나가고 있어요.":f.Step==ElevatorStep.Connecting?"비상통화 연결 중…":"";
            if(f.Step==ElevatorStep.Proposal)caption.text=ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotStory);
            if(f.Step==ElevatorStep.PosterPointing)caption.text="친구가 뛰려고 해요. 안전 안내를 살펴보세요.";
            if(f.Step==ElevatorStep.PosterWarning)caption.text="나  ·  여기서는 뛰면 위험해. 슛은 밖에서 하자.";
            if(f.Step==ElevatorStep.Jump)caption.text=f.Elapsed<lesson.settings.friendReplySeconds?ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotGesture):"엘리베이터 안에서는 뛰지 마세요.";
            if(f.Step==ElevatorStep.Stopped&&f.Phase==InteractionPhase.Dialogue)caption.text=ElevatorDialogue.Caption("친구",ElevatorDialogue.Stopped);
            string caller=ElevatorDialogue.Caller(f.UsedPhone);
            if(f.Step==ElevatorStep.CallQuestion)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.Greeting(f.UsedPhone));
            if(f.Step==ElevatorStep.CallResponse)caption.text=ElevatorDialogue.Caption("나",lesson.settings.reportDialogue);
            if(f.Step==ElevatorStep.CallLiftQuestion)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.LocationQuestion);
            if(f.Step==ElevatorStep.CallIdentityGuide)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.IdentityGuide);
            if(ElevatorDoorSafetyCutscene.Describe(lesson,out var speaker,out var line))caption.text=ElevatorDialogue.Caption(speaker,line);
            float alpha=0;
            if(f.Step==ElevatorStep.Returning){caption.text="구조요원과 함께 계단으로 내려가고 있어요.";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,2.4f,3.25f));}
            if(f.Step==ElevatorStep.WaitingFade){caption.gameObject.SetActive(true);caption.text="친구와 함께 기다리던 중…";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,3.6f,4.5f));}
            if(f.Step==ElevatorStep.GoingHome)caption.text="친구와 함께 집으로 돌아가요.";
            if(XRSettings.isDeviceActive&&(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting))
            {float middle=f.Step==ElevatorStep.Entering?3.1f:2.3f;alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.60f,Mathf.Abs(f.Elapsed-middle)));}
            if(XRSettings.isDeviceActive&&f.Step==ElevatorStep.GoingHome)alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.65f,Mathf.Abs(f.Elapsed-3)));
            fade.gameObject.SetActive(alpha>0);fade.GetComponent<Image>().color=new Color(0,0,0,alpha);
            lesson.view.SetCinematicVisibility();
        }
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void Position()
        {
            if(!playerCamera||!top)return;
            const float distance=.45f;float height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.SetPositionAndRotation(playerCamera.transform.position+playerCamera.transform.forward*distance,playerCamera.transform.rotation);transform.localScale=Vector3.one*(height/720);
            var root=(RectTransform)transform;float width=720*playerCamera.aspect;root.sizeDelta=new Vector2(width,720);
            top.sizeDelta=bottom.sizeDelta=new Vector2(width+20,82);float slide=Mathf.SmoothStep(0,1,Amount)*82;
            top.anchoredPosition=new Vector2(0,401-slide);bottom.anchoredPosition=new Vector2(0,-401+slide);
            ElevatorTypography.Apply(caption,ElevatorTypography.CaptionSize);
            fade.sizeDelta=new Vector2(width+20,740);caption.rectTransform.sizeDelta=new Vector2(Mathf.Min(1100,width-48),82);
        }
    }
}
