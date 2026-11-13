// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorCinematicOverlay.cs. Original is not modified.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator.DialoguePreview
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
        public float FadeAlpha {get;private set;}
        public bool LetterboxVisible => isActiveAndEnabled && Amount > .001f;
        public bool Blocking => lesson&&lesson.Flow!=null&&(lesson.Flow.IsCinematic||Amount>.015f);
        int run=-1;
        bool enteredThroughStoryTransition;
        bool stereoReady;
        StereoUISafeArea.PlaneCache planeCache;
        // Compatibility for existing HUD/preview callers: the bands no longer own text.
        public int SubtitleLayoutVersion => 0;
        public bool SubtitleFits => false;
        public bool HasVisibleSubtitle => false;
        public static bool UsesRescueSubtitle(bool vr,ElevatorStep step,bool readyToExit) => false;
        public void Build()
        {
            var canvas=GetComponent<Canvas>();if(!canvas)canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=playerCamera;canvas.sortingOrder=100;
            top=Panel("TopLetterbox");bottom=Panel("BottomLetterbox");fade=Panel("XRComfortFade");
            HideLegacyCaption();
            top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);fade.gameObject.SetActive(false);
        }
        RectTransform Panel(string name)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(transform,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
            var image=r.gameObject.AddComponent<Image>();image.color=Color.black;image.raycastTarget=false;image.material=overlayMaterial;return r;
        }
        void OnEnable(){Application.onBeforeRender+=Position;if(Application.isPlaying)Advance(0);}
        void OnDisable(){Application.onBeforeRender-=Position;}
        void LateUpdate(){Advance(Time.unscaledDeltaTime);}
        public void Advance(float seconds)
        {
            if(lesson&&lesson.IsPaused)return;
            if(seconds<0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            if(!lesson||lesson.Flow==null)return;if(!top)Build();
            if(run!=lesson.RunVersion){run=lesson.RunVersion;Amount=lesson.Flow.IsOpening?1:0;enteredThroughStoryTransition=ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy;}
            if(lesson.Flow.IsOpening&&ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)enteredThroughStoryTransition=true;
            Amount=Mathf.MoveTowards(Amount,lesson.Flow.IsCinematic?1:0,seconds/.38f);
            Position();
            top.gameObject.SetActive(Amount>0&&(!XRSettings.isDeviceActive||stereoReady));
            bottom.gameObject.SetActive(Amount>0&&(!XRSettings.isDeviceActive||stereoReady));
            var f=lesson.Flow;
            float alpha=0;
            if(f.Step==ElevatorStep.StartupFade)alpha=enteredThroughStoryTransition?0:ElevatorOpeningSequence.FadeAlpha(f.Elapsed);
            if(f.Step==ElevatorStep.GymDeparture)
            {
                float startupAlpha=enteredThroughStoryTransition?0:ElevatorOpeningSequence.FadeAlpha(f.Elapsed);
                alpha=Mathf.Max(startupAlpha,ElevatorOpeningSequence.ComfortFadeAlpha(f.Elapsed,XRSettings.isDeviceActive));
            }
            if(f.Step==ElevatorStep.Returning)alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,2.4f,3.25f));
            if(f.Step==ElevatorStep.WaitingFade)alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,3.6f,4.5f));
            if(XRSettings.isDeviceActive&&(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting))
            {float middle=f.Step==ElevatorStep.Entering?3.1f:2.3f;alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.60f,Mathf.Abs(f.Elapsed-middle)));}
            if(XRSettings.isDeviceActive&&f.Step==ElevatorStep.GoingHome)alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.65f,Mathf.Abs(f.Elapsed-3)));
            if(XRSettings.isDeviceActive&&!lesson.SpawnReady)alpha=1;
            FadeAlpha=alpha;fade.gameObject.SetActive(alpha>0);fade.GetComponent<Image>().color=new Color(0,0,0,alpha);
            lesson.view.SetCinematicVisibility();
        }
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void Position()
        {
            if(!playerCamera||!top)return;
            HideLegacyCaption();
            if(XRSettings.isDeviceActive){PositionStereo();return;}
            const float distance=.45f;float height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.SetPositionAndRotation(playerCamera.transform.position+playerCamera.transform.forward*distance,playerCamera.transform.rotation);transform.localScale=Vector3.one*(height/720);
            var root=(RectTransform)transform;float width=720*playerCamera.aspect;root.sizeDelta=new Vector2(width,720);
            float band=VRUIReadingLayout.LetterboxHeight;
            top.sizeDelta=bottom.sizeDelta=new Vector2(width+20,band);float slide=Mathf.SmoothStep(0,1,Amount)*band;
            top.anchoredPosition=new Vector2(0,360+band*.5f-slide);bottom.anchoredPosition=new Vector2(0,-360-band*.5f+slide);
            fade.sizeDelta=new Vector2(width+20,740);
            fade.anchoredPosition=Vector2.zero;
            top.gameObject.SetActive(Amount>0);bottom.gameObject.SetActive(Amount>0);
        }

        void PositionStereo()
        {
            VRUIReadingLayout.SetPlane(transform,playerCamera.transform);
            // Preserve a fully overscanned comfort blackout while XR projection is starting/reconnecting.
            fade.sizeDelta=new Vector2(2880,2880);fade.anchoredPosition=Vector2.zero;
            if(!StereoUISafeArea.TryCapture(playerCamera,out var frame))
            {stereoReady=false;top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);return;}
            LayoutVR(frame);
        }

        // Also used by recorded-frustum validation. This does not change the camera/projection.
        public bool LayoutVR(StereoUISafeArea.Frame frame)
        {
            if(!playerCamera||!top)return false;
            HideLegacyCaption();
            VRUIReadingLayout.SetPlane(transform,playerCamera.transform);
            stereoReady=planeCache.Get(frame,transform,out _,out var coverage,out _,out _);
            if(!stereoReady){top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);return false;}
            ((RectTransform)transform).sizeDelta=coverage.size;
            float overscan=coverage.height*.02f;
            var lv=planeCache.LeftVertical;var rv=planeCache.RightVertical;
            float band=Mathf.Min(lv.y-lv.x,rv.y-rv.x)*(VRUIReadingLayout.LetterboxHeight/720);
            float outer=coverage.yMin-overscan;
            float inner=LowerBandEdge(lv,rv,Amount);
            float topOuter=coverage.yMax+overscan;
            float topInner=Mathf.Min(lv.y,rv.y)-band*Mathf.SmoothStep(0,1,Amount);
            // Equal-sized backgrounds; any canted/asymmetric excess stays offscreen.
            float thickness=Mathf.Max(inner-outer,topOuter-topInner);
            top.sizeDelta=bottom.sizeDelta=new Vector2(coverage.width+overscan*2,thickness);
            bottom.anchoredPosition=new Vector2(coverage.center.x,inner-thickness*.5f);
            top.anchoredPosition=new Vector2(coverage.center.x,topInner+thickness*.5f);
            bottom.gameObject.SetActive(Amount>0);
            top.gameObject.SetActive(Amount>0);
            fade.sizeDelta=coverage.size+Vector2.one*(overscan*2);
            fade.anchoredPosition=coverage.center;
            return true;
        }

        public static float LowerBandEdge(Vector2 left,Vector2 right,float amount) =>
            Mathf.Max(left.x,right.x)+Mathf.Min(left.y-left.x,right.y-right.x)*
            (VRUIReadingLayout.LetterboxHeight/720)*Mathf.SmoothStep(0,1,amount);

        void HideLegacyCaption()
        {
            // Retain serialized references/GUIDs but never render old scene captions.
            if(caption){caption.text="";caption.enabled=false;caption.gameObject.SetActive(false);}
            var oldBackdrop=bottom?bottom.Find("SubtitleReadabilityBackdrop"):null;
            if(oldBackdrop)oldBackdrop.gameObject.SetActive(false);
        }

        public bool TryGetSubtitleViewport(StereoUISafeArea.Frame frame,out Rect region)
        {
            region=default;
            return false;
        }

        public bool TryGetSubtitleLocalRect(Transform plane,out Rect region)
        {
            region=default;
            return false;
        }
    }
}
