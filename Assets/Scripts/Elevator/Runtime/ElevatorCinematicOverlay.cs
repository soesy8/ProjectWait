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
        public float RetryFade {get;set;}
        public bool LetterboxVisible => isActiveAndEnabled && Amount > .001f;
        public bool Blocking => lesson&&lesson.Flow!=null&&(lesson.Flow.IsCinematic||FadeAlpha>.015f||lesson.RestoringObservation);
        int run=-1;
        bool enteredThroughStoryTransition;
        bool stereoReady;
        Vector3 levelRight=Vector3.right;
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
        void OnDisable(){Application.onBeforeRender-=Position;var blackout=ProjectWait.M.Shared.ComfortScreenFade.For(playerCamera);if(blackout)blackout.SetOpacity(0);}
        void LateUpdate(){Advance(Time.unscaledDeltaTime);}
        public void Advance(float seconds)
        {
            if(lesson&&lesson.IsPaused)return;
            if(seconds<0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            if(!lesson||lesson.Flow==null)return;if(!top)Build();
            if(run!=lesson.RunVersion){run=lesson.RunVersion;Amount=lesson.Flow.IsOpening?1:0;enteredThroughStoryTransition=ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy;}
            if(lesson.Flow.IsOpening&&ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy)enteredThroughStoryTransition=true;
            // Searching the cabin and reading the ID plate need the entire field of view.
            bool framed=lesson.Flow.IsCinematic||lesson.Flow.Step==ElevatorStep.CallSafetyAdvice;
            Amount=Mathf.MoveTowards(Amount,framed?1:0,seconds/.38f);
            Position();
            top.gameObject.SetActive(Amount>0&&(!XRSettings.isDeviceActive||stereoReady));
            bottom.gameObject.SetActive(Amount>0&&(!XRSettings.isDeviceActive||stereoReady));
            var f=lesson.Flow;
            float alpha=0;
            if(f.Step==ElevatorStep.StartupFade)alpha=enteredThroughStoryTransition?0:ElevatorOpeningSequence.FadeAlpha(f.Elapsed);
            if(ElevatorTravelTiming.IsRelocation(f.Step))alpha=ProjectWait.M.Shared.ComfortBlackout.Alpha(ElevatorTravelTiming.TravelElapsed(f));
            if(f.Step==ElevatorStep.WaitingFade)alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,3.6f,4.5f));
            if(XRSettings.isDeviceActive&&!lesson.SpawnReady)alpha=1;
            alpha=Mathf.Max(alpha,RetryFade);FadeAlpha=alpha;
            var blackout=ProjectWait.M.Shared.ComfortScreenFade.For(playerCamera);
            if(blackout)blackout.SetOpacity(alpha);
            fade.gameObject.SetActive(!blackout&&alpha>0);fade.GetComponent<Image>().color=new Color(0,0,0,alpha);
            lesson.view.SetCinematicVisibility();
        }
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void Position()
        {
            if(!playerCamera||!top)return;
            HideLegacyCaption();
            if(XRSettings.isDeviceActive){PositionStereo();return;}
            const float distance=.45f;float height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.SetPositionAndRotation(playerCamera.transform.position+playerCamera.transform.forward*distance,ProjectWait.M.Shared.LevelLetterboxLayout.LevelRotation(playerCamera.transform.forward,ref levelRight));transform.localScale=Vector3.one*(height/720);
            var root=(RectTransform)transform;
            ProjectWait.M.Shared.LevelLetterboxLayout.LayoutDesktop(playerCamera,root,top,bottom,Amount);
            fade.sizeDelta=root.sizeDelta*1.05f;fade.anchoredPosition=(top.anchoredPosition+bottom.anchoredPosition)*.5f;
            top.gameObject.SetActive(Amount>0);bottom.gameObject.SetActive(Amount>0);
        }

        void PositionStereo()
        {
            VRUIReadingLayout.SetPlane(transform,playerCamera.transform);
            transform.rotation=ProjectWait.M.Shared.LevelLetterboxLayout.LevelRotation(playerCamera.transform.forward,ref levelRight);
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
            transform.rotation=ProjectWait.M.Shared.LevelLetterboxLayout.LevelRotation(playerCamera.transform.forward,ref levelRight);
            stereoReady=ProjectWait.M.Shared.LevelLetterboxLayout.LayoutStereo(playerCamera,frame,(RectTransform)transform,top,bottom,Amount,out var coverage);
            if(!stereoReady){top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);return false;}
            float overscan=coverage.height*.02f;
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
