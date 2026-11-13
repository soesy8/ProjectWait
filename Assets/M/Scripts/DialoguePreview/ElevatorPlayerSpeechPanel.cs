using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using ProjectWait.M.SceneTransition;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // PC screen overlay; VR uses a native-stereo view-locked plane, never a camera-pose override.
    [DefaultExecutionOrder(250)]
    public sealed class ElevatorPlayerSpeechPanel : MonoBehaviour
    {
        ElevatorSpatialUI owner;
        Canvas canvas;
        CanvasGroup group;
        CanvasScaler scaler;
        RectTransform root,panel,tag;
        TMP_Text body,nameLabel;
        StereoUISafeArea.PlaneCache geometry;
        bool vrMode;
        float width=-1,height=210;
        public bool IsVisible=>canvas&&canvas.enabled&&group&&group.alpha>.001f;

        public void Initialize(ElevatorSpatialUI value)
        {
            owner=value;root=(RectTransform)transform;
            canvas=gameObject.AddComponent<Canvas>();canvas.sortingOrder=110;
            scaler=gameObject.AddComponent<CanvasScaler>();
            group=gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.interactable=group.blocksRaycasts=false;
            panel=Rect("Player dialogue",root);tag=Rect("Player speaker",panel);
            var art=owner.storybook;
            if(art){art.Artwork(panel,art.dialogueFrame);art.Artwork(tag,art.speakerRibbon,false);}
            else{var paper=panel.gameObject.AddComponent<Image>();paper.color=new Color(1,.965f,.86f);paper.raycastTarget=false;}
            body=Text("Player line",panel,ElevatorDialogueText.BodySize,false);
            nameLabel=Text("Player name",tag,ElevatorTypography.SpeakerSize,true);nameLabel.text="나";
            canvas.enabled=false;
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;return r;
        }
        TMP_Text Text(string name,Transform parent,float size,bool bold)
        {
            var t=Rect(name,parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font=owner.view.dialogueText.font;t.fontSharedMaterial=owner.view.dialogueText.fontSharedMaterial;
            ElevatorTypography.Apply(t,size,bold);t.lineSpacing=9;t.color=new Color(.28f,.19f,.14f);
            t.alignment=TextAlignmentOptions.MidlineLeft;return t;
        }
        void Content(float availableWidth)
        {
            float nextWidth=Mathf.Min(ElevatorDialogueText.PanelWidth,availableWidth);
            var source=owner.view.dialogueText;
            bool changed=body.text!=source.text||Mathf.Abs(width-nextWidth)>.01f||body.font!=source.font;
            body.font=source.font;body.fontSharedMaterial=source.fontSharedMaterial;body.maxVisibleCharacters=source.maxVisibleCharacters;
            if(!changed)return;
            width=nextWidth;body.text=source.text;
            float bodyHeight=Mathf.Max(102,body.GetPreferredValues(body.text,Mathf.Max(1,width-104),0).y+12);
            height=Mathf.Max(210,bodyHeight+80);panel.sizeDelta=new Vector2(width,height);
            body.rectTransform.sizeDelta=new Vector2(width-104,bodyHeight);body.rectTransform.anchoredPosition=Vector2.zero;
            float tagWidth=Mathf.Clamp(nameLabel.GetPreferredValues().x+54,132,Mathf.Max(132,width-60));
            tag.sizeDelta=new Vector2(tagWidth,58);tag.anchoredPosition=new Vector2(-width*.5f+30+tagWidth*.5f,height*.5f-1);
            nameLabel.rectTransform.sizeDelta=new Vector2(tagWidth-42,46);nameLabel.rectTransform.anchoredPosition=new Vector2(-3,1);
        }
        bool Allowed()=>owner&&owner.IsPlayerSpeaking&&!owner.view.lesson.IsPaused&&owner.view.lesson.SpawnReady&&
            !StorybookSceneTransition.IsBusy&&(!owner.CinematicOverlay||owner.CinematicOverlay.FadeAlpha<=.01f);
        public void Hide(){if(canvas)canvas.enabled=false;if(group)group.alpha=0;}
        void Show()
        {
            canvas.enabled=true;
            var sourceGroup=owner.view.dialoguePanel.GetComponent<CanvasGroup>();group.alpha=sourceGroup?sourceGroup.alpha:1;
        }
        public void Refresh(bool vr)
        {
            if(!owner||!canvas||!Allowed()){Hide();return;}
            if(vr)
            {
                if(!StereoUISafeArea.TryCapture(owner.view.playerCamera,out var frame)||!LayoutVR(frame)){Hide();return;}
                Show();owner.LayoutVRHud(frame);
            }
            else LayoutDesktop();
        }
        public bool LayoutVR(StereoUISafeArea.Frame frame)
        {
            if(!owner||!canvas)return false;
            vrMode=true;canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=owner.view.playerCamera;canvas.scaleFactor=1;
            VRUIReadingLayout.SetPlane(root,owner.view.playerCamera.transform);root.localScale*=StereoUISafeArea.VisualScale;
            if(!geometry.Get(frame,root,out var safe,out var coverage,out _,out _))return false;
            root.sizeDelta=coverage.size;Content(safe.width-24);
            if(width<240||safe.height<height+58)return false;
            float amount=owner.CinematicOverlay?owner.CinematicOverlay.Amount:1;
            float edge=ElevatorCinematicOverlay.LowerBandEdge(geometry.LeftVertical,geometry.RightVertical,amount);
            // edge = bottom + height/3, so the card centre is edge + height/6.
            float y=Mathf.Clamp(edge+height/6,safe.yMin+height*.5f+8,safe.yMax-height*.5f-37);
            float x=Mathf.Clamp(0,safe.xMin+width*.5f+8,safe.xMax-width*.5f-8);
            panel.anchoredPosition=new Vector2(x,y);
            return VRUIReadingLayout.Project(frame,panel,out _,true)&&VRUIReadingLayout.Project(frame,tag,out _,true);
        }
        void LayoutDesktop()
        {
            vrMode=false;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            canvas.scaleFactor=Mathf.Max(1,Screen.height)/720f;root.localScale=Vector3.one;
            float screenWidth=720f*Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height);
            root.sizeDelta=new Vector2(screenWidth,720);Content(screenWidth-32);
            if(width<240||height+58>720){Hide();return;}
            float amount=owner.CinematicOverlay?owner.CinematicOverlay.Amount:1;
            float edge=-360+VRUIReadingLayout.LetterboxHeight*Mathf.SmoothStep(0,1,amount);
            panel.anchoredPosition=new Vector2(0,Mathf.Clamp(edge+height/6,-360+height*.5f+16,360-height*.5f-45));
            Show();
        }
        public bool TryGetExclusion(Transform plane,out UnityEngine.Rect region)
        {
            region=default;if(!IsVisible||!vrMode)return false;
            var a=VRUIReadingLayout.LocalRect(panel,plane);var b=VRUIReadingLayout.LocalRect(tag,plane);
            region=UnityEngine.Rect.MinMaxRect(Mathf.Min(a.xMin,b.xMin),Mathf.Min(a.yMin,b.yMin),Mathf.Max(a.xMax,b.xMax),Mathf.Max(a.yMax,b.yMax));return true;
        }
        public bool LookingAtPanel()
        {
            if(!IsVisible)return false;
            var camera=owner.view.playerCamera;
            if(!vrMode)return RectTransformUtility.RectangleContainsScreenPoint(panel,new Vector2(Screen.width*.5f,Screen.height*.5f),null);
            var ray=new Ray(camera.transform.position,camera.transform.forward);
            return new Plane(panel.forward,panel.position).Raycast(ray,out float d)&&d>=camera.nearClipPlane&&panel.rect.Contains((Vector2)panel.InverseTransformPoint(ray.GetPoint(d)));
        }
        void OnEnable(){Application.onBeforeRender+=BeforeRender;}
        void OnDisable(){Application.onBeforeRender-=BeforeRender;Hide();}
        void LateUpdate(){Refresh(XRSettings.isDeviceActive);}
        [BeforeRenderOrder(220)]
        void BeforeRender(){Refresh(XRSettings.isDeviceActive);}
    }
}
