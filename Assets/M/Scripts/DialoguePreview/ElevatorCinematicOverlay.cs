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
        RectTransform subtitleBackdrop;
        bool stereoLayout, lastSubtitleVisible;
        string lastSubtitleText;
        Vector2 lastSubtitleSize;
        StereoUISafeArea.PlaneCache planeCache;
        string measuredText;
        TMP_FontAsset measuredFont;
        FontStyles measuredStyle;
        float measuredWidth=-1,measuredSize;
        Vector2 measuredPreferred;
        int captionMeasurements;
        public int SubtitleLayoutVersion { get; private set; }
        public bool SubtitleFits { get; private set; }
        public bool HasVisibleSubtitle => SubtitleVisible;
        // VR rescue dialogue belongs to the fixed world card, not a duplicate subtitle.
        public static bool UsesRescueSubtitle(bool vr,ElevatorStep step,bool readyToExit) => !vr&&step==ElevatorStep.Rescue&&!readyToExit;
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
            caption.color=Color.white;
            caption.gameObject.SetActive(!lesson.view.spatial||f.IsShotCutscene||f.IsCallCutscene||f.IsDoorSafetyCutscene||f.Step==ElevatorStep.GymDeparture||f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Returning||f.Step==ElevatorStep.GoingHome||UsesRescueSubtitle(XRSettings.isDeviceActive,f.Step,f.ReadyToExit));
            caption.text=f.Step==ElevatorStep.Connecting?"비상통화 연결 중…":"";
            if(f.Step==ElevatorStep.Proposal)caption.text=ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotStory);
            if(f.Step==ElevatorStep.PosterPointing)caption.text="친구가 뛰려고 해요. 안전 안내를 살펴보세요.";
            if(f.Step==ElevatorStep.PosterWarning)caption.text="나  ·  여기서는 뛰면 위험해. 슛은 밖에서 하자.";
            if(f.Step==ElevatorStep.Jump)caption.text=f.Elapsed<lesson.settings.friendReplySeconds?ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotGesture):"";
            if(f.Step==ElevatorStep.Stopped&&f.Phase==InteractionPhase.Dialogue)caption.text=ElevatorDialogue.Caption("친구",ElevatorDialogue.Stopped);
            string caller=ElevatorDialogue.Caller(f.UsedPhone);
            if(f.Step==ElevatorStep.CallQuestion)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.Greeting(f.UsedPhone));
            if(f.Step==ElevatorStep.CallResponse)caption.text=ElevatorDialogue.Caption("나",lesson.settings.reportDialogue);
            if(f.Step==ElevatorStep.CallLiftQuestion)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.LocationQuestion);
            if(f.Step==ElevatorStep.CallIdentityGuide)caption.text=ElevatorDialogue.Caption(caller,ElevatorDialogue.IdentityGuide);
            if(ElevatorDoorSafetyCutscene.Describe(lesson,out var speaker,out var line))caption.text=ElevatorDialogue.Caption(speaker,line);
            if(f.Step==ElevatorStep.Rescue&&!f.ReadyToExit)
            {
                speaker="구조요원";line=ElevatorDialogue.RescueChecking;
                if(lesson.presentation&&lesson.presentation.rescueTeam)lesson.presentation.rescueTeam.Describe(lesson,ref speaker,ref line);
                caption.text=ElevatorDialogue.Caption(speaker,line);
            }
            float alpha=0;
            if(f.Step==ElevatorStep.StartupFade){caption.gameObject.SetActive(false);alpha=enteredThroughStoryTransition?0:ElevatorOpeningSequence.FadeAlpha(f.Elapsed);}
            if(f.Step==ElevatorStep.GymDeparture)
            {
                caption.text=ElevatorOpeningSequence.Caption(f.Elapsed);
                float startupAlpha=enteredThroughStoryTransition?0:ElevatorOpeningSequence.FadeAlpha(f.Elapsed);
                caption.color=new Color(1,1,1,1-startupAlpha);
                alpha=Mathf.Max(startupAlpha,ElevatorOpeningSequence.ComfortFadeAlpha(f.Elapsed,XRSettings.isDeviceActive));
            }
            if(f.Step==ElevatorStep.Returning){caption.text="계단으로 1층에 내려가요.";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,2.4f,3.25f));}
            if(f.Step==ElevatorStep.WaitingFade){caption.gameObject.SetActive(true);caption.text="잠시 후…";alpha=S(f.Elapsed,.35f,1.1f)*(1-S(f.Elapsed,3.6f,4.5f));}
            if(XRSettings.isDeviceActive&&(f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting))
            {float middle=f.Step==ElevatorStep.Entering?3.1f:2.3f;alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.60f,Mathf.Abs(f.Elapsed-middle)));}
            if(XRSettings.isDeviceActive&&f.Step==ElevatorStep.GoingHome)alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.65f,Mathf.Abs(f.Elapsed-3)));
            // Content may have changed/wrapped above; size the subtitle without resizing the band.
            if(XRSettings.isDeviceActive)Position();
            if(XRSettings.isDeviceActive&&!lesson.SpawnReady)alpha=1;
            FadeAlpha=alpha;fade.gameObject.SetActive(alpha>0);fade.GetComponent<Image>().color=new Color(0,0,0,alpha);
            lesson.view.SetCinematicVisibility();
        }
        static float S(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        void Position()
        {
            if(!playerCamera||!top)return;
            if(XRSettings.isDeviceActive){PositionStereo();return;}
            stereoLayout=false;
            caption.enabled=true;
            const float distance=.45f;float height=2*distance*Mathf.Tan(playerCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            transform.SetPositionAndRotation(playerCamera.transform.position+playerCamera.transform.forward*distance,playerCamera.transform.rotation);transform.localScale=Vector3.one*(height/720);
            var root=(RectTransform)transform;float width=720*playerCamera.aspect;root.sizeDelta=new Vector2(width,720);
            top.sizeDelta=bottom.sizeDelta=new Vector2(width+20,82);float slide=Mathf.SmoothStep(0,1,Amount)*82;
            top.anchoredPosition=new Vector2(0,401-slide);bottom.anchoredPosition=new Vector2(0,-401+slide);
            ElevatorTypography.Apply(caption,ElevatorTypography.CaptionSize);
            fade.sizeDelta=new Vector2(width+20,740);caption.rectTransform.sizeDelta=new Vector2(Mathf.Min(1100,width-48),82);
            caption.rectTransform.localScale=Vector3.one;caption.rectTransform.anchoredPosition=Vector2.zero;
            fade.anchoredPosition=Vector2.zero;
            top.gameObject.SetActive(Amount>0);bottom.gameObject.SetActive(Amount>0);
            if(subtitleBackdrop)subtitleBackdrop.gameObject.SetActive(false);
            UpdateSubtitleVersion(false);
        }

        void PositionStereo()
        {
            stereoLayout=true;
            VRUIReadingLayout.SetPlane(transform,playerCamera.transform);
            // Preserve a fully overscanned comfort blackout while XR projection is starting/reconnecting.
            fade.sizeDelta=new Vector2(2880,2880);fade.anchoredPosition=Vector2.zero;
            if(!StereoUISafeArea.TryCapture(playerCamera,out var frame))
            {stereoReady=false;top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);UpdateSubtitleVersion(false);return;}
            LayoutVR(frame);
        }

        // Also used by recorded-frustum validation. This does not change the camera/projection.
        public bool LayoutVR(StereoUISafeArea.Frame frame)
        {
            if(!playerCamera||!top)return false;
            stereoLayout=true;
            VRUIReadingLayout.SetPlane(transform,playerCamera.transform);
            stereoReady=planeCache.Get(frame,transform,out var safe,out var coverage,out var left,out var right);
            if(!stereoReady){top.gameObject.SetActive(false);bottom.gameObject.SetActive(false);SubtitleFits=false;UpdateSubtitleVersion(false);return false;}
            ((RectTransform)transform).sizeDelta=coverage.size;
            float overscan=coverage.height*.02f;
            var lv=planeCache.LeftVertical;var rv=planeCache.RightVertical;
            float band=Mathf.Min(lv.y-lv.x,rv.y-rv.x)*(VRUIReadingLayout.LetterboxHeight/720);
            float outer=coverage.yMin-overscan;
            float inner=Mathf.Max(lv.x,rv.x)+band*Mathf.SmoothStep(0,1,Amount);
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
            ElevatorTypography.Apply(caption,ElevatorTypography.CaptionSize);
            var rect=caption.rectTransform;
            rect.localScale=Vector3.one*StereoUISafeArea.VisualScale;
            float horizontal=VRUIReadingLayout.AnglePoint(VRUIReadingLayout.HorizontalLimit,0).x;
            var padded=Rect.MinMaxRect(Mathf.Max(Mathf.Max(left.xMin,right.xMin),-horizontal)+16,
                Mathf.Max(left.yMin,right.yMin)+12,Mathf.Min(Mathf.Min(left.xMax,right.xMax),horizontal)-16,inner-12);
            float width=Mathf.Max(1,Mathf.Min(VRUIReadingLayout.CaptionWidth,
                Mathf.Min(safe.width-32,padded.width-24*StereoUISafeArea.VisualScale)/StereoUISafeArea.VisualScale));
            var preferred=MeasureCaption(width);
            float height=Mathf.Max(36,preferred.y+8);
            rect.sizeDelta=new Vector2(width,height);
            // Reflow wider, without shrinking the font, inside the half-height lower band.
            // The full-screen inscribed rectangle sacrifices vertical space to its
            // far corners on canted displays. Use the native lower strip instead,
            // then validate every actual text/backdrop corner in both eyes below.
            SubtitleFits=VRUIReadingLayout.Place(rect,transform,padded,0,VRUIReadingLayout.CaptionPitch,out _);
            if(!subtitleBackdrop)
            {
                subtitleBackdrop=Panel("SubtitleReadabilityBackdrop");
                subtitleBackdrop.SetParent(bottom,false);
                subtitleBackdrop.SetSiblingIndex(0);
            }
            subtitleBackdrop.localScale=rect.localScale;
            subtitleBackdrop.sizeDelta=new Vector2(Mathf.Min(width,preferred.x)+24,height+8);
            subtitleBackdrop.position=rect.position+transform.forward*.001f;
            subtitleBackdrop.gameObject.SetActive(SubtitleFits&&caption.gameObject.activeSelf&&!string.IsNullOrEmpty(caption.text));
            SubtitleFits=SubtitleFits&&VRUIReadingLayout.Project(frame,subtitleBackdrop,out _,true)&&VRUIReadingLayout.Project(frame,rect,out _,true);
            caption.enabled=SubtitleFits;
            UpdateSubtitleVersion(SubtitleVisible);
            return SubtitleFits;
        }

        Vector2 MeasureCaption(float width)
        {
            if(measuredText!=caption.text||measuredFont!=caption.font||Mathf.Abs(measuredWidth-width)>.01f||
                measuredSize!=caption.fontSize||measuredStyle!=caption.fontStyle)
            {
                measuredText=caption.text;measuredFont=caption.font;measuredWidth=width;
                measuredSize=caption.fontSize;measuredStyle=caption.fontStyle;
                measuredPreferred=caption.GetPreferredValues(caption.text,width,0);captionMeasurements++;
            }
            return measuredPreferred;
        }

        bool SubtitleVisible => stereoLayout&&stereoReady&&Amount>.001f&&caption&&caption.enabled&&caption.gameObject.activeSelf&&
            subtitleBackdrop&&subtitleBackdrop.gameObject.activeSelf&&!string.IsNullOrEmpty(caption.text);

        void UpdateSubtitleVersion(bool visible)
        {
            var size=caption?caption.rectTransform.sizeDelta:Vector2.zero;
            var text=visible?caption.text:"";
            if(visible!=lastSubtitleVisible||text!=lastSubtitleText||(size-lastSubtitleSize).sqrMagnitude>.25f)
            {SubtitleLayoutVersion++;lastSubtitleVisible=visible;lastSubtitleText=text;lastSubtitleSize=size;}
        }

        public bool TryGetSubtitleViewport(StereoUISafeArea.Frame frame,out Rect region)
        {
            region=default;
            return SubtitleVisible&&VRUIReadingLayout.Project(frame,subtitleBackdrop,out region);
        }

        public bool TryGetSubtitleLocalRect(Transform plane,out Rect region)
        {
            region=default;
            if(!SubtitleVisible)return false;
            region=VRUIReadingLayout.LocalRect(subtitleBackdrop,plane);
            return true;
        }
    }
}
