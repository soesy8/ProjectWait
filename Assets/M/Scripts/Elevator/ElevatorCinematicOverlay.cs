using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator
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
            caption.gameObject.SetActive(!lesson.view.spatial||f.IsShotCutscene||f.IsCallCutscene||f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GoingHome);
            caption.text=f.Step==ElevatorStep.Entering?"친구와 함께 엘리베이터에 들어가요.":f.Step==ElevatorStep.Exiting?"안내를 따라 천천히 밖으로 나가요.":f.Step==ElevatorStep.Proposal?"친구  ·  내가 어떻게 넣었냐면…":f.Step==ElevatorStep.Jump?"친구  ·  이렇게 뛰어서!":f.Step==ElevatorStep.Stopped?"친구  ·  어? 왜 멈췄지?":f.Step==ElevatorStep.Connecting?"비상통화 연결 중…":f.Step==ElevatorStep.Rescue?"구조원  ·  문을 열고 안전을 확인할게요.":"";
            if(f.Step==ElevatorStep.Proposal)caption.text="친구  ·  아까 슛 멋있었지? 보여 줄게!";
            if(f.Step==ElevatorStep.PosterPointing)caption.text="나  ·  안에서는 뛰면 안 되는데…\n안전 포스터를 가리켜 알려 주자.";
            if(f.Step==ElevatorStep.PosterWarning)caption.text="나  ·  안에서 뛰면 안 돼. 하지 마!";
            if(f.Step==ElevatorStep.Jump)caption.text=f.Elapsed<lesson.settings.friendReplySeconds?"친구  ·  에이, 한 번만! 잘 봐.":"친구  ·  이렇게 뛰어서!";
            string caller=f.UsedPhone?"119 · 모의 통화":"관리실";
            if(f.Step==ElevatorStep.CallQuestion)caption.text=caller+"  ·  연결됐습니다. 무슨 상황인가요?";
            if(f.Step==ElevatorStep.CallResponse)caption.text="나  ·  "+lesson.settings.reportDialogue;
            if(f.Step==ElevatorStep.CallLiftQuestion)caption.text=caller+"  ·  어느 엘리베이터에 있나요?";
            if(f.Step==ElevatorStep.CallIdentityGuide)caption.text=caller+"  ·  전면 비상벨 아래 식별판에\n건물명과 승강기 번호가 적혀 있어요.";
            float alpha=0;
            if(f.Step==ElevatorStep.Returning){caption.text="직원의 안내를 받아 계단으로 1층에 내려왔어요.";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,2.4f,3.25f));}
            if(f.Step==ElevatorStep.WaitingFade){caption.gameObject.SetActive(true);caption.text="문에서 떨어져 함께 기다렸어요. 잠시 후…";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,3.6f,4.5f));}
            if(f.Step==ElevatorStep.GoingHome)caption.text="1층 주출입구를 통해 친구와 집으로 가요.";
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
            fade.sizeDelta=new Vector2(width+20,740);caption.rectTransform.sizeDelta=new Vector2(Mathf.Min(1100,width-48),74);
        }
    }
}
