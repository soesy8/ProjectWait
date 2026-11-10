// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorUITransitions.cs. Original is not modified.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Transitions affect opacity only. Panels never chase a moving layout target.
    [DisallowMultipleComponent]
    public sealed class ElevatorUITransitions : MonoBehaviour
    {
        public float fadeSeconds = .18f, textSeconds = .16f;
        ElevatorView view;
        int depth;
        readonly List<Panel> panels = new List<Panel>();
        readonly List<TextChange> texts = new List<TextChange>();
        readonly List<Retired> retired = new List<Retired>();
        readonly List<TMP_Text> thoughtTexts = new List<TMP_Text>();
        int thoughtHierarchy=-1;

        struct Geometry
        {
            public Vector2 min, max, pivot, size;
            public Vector3 center, scale;
            public Quaternion rotation;
            public static Geometry Read(RectTransform r) => new Geometry {
                min=r.anchorMin,max=r.anchorMax,pivot=r.pivot,size=r.sizeDelta,
                center=r.localPosition+r.localRotation*Vector3.Scale(r.rect.center,r.localScale),
                scale=r.localScale,rotation=r.localRotation };
            public void Apply(RectTransform r)
            {
                r.anchorMin=min;r.anchorMax=max;r.pivot=pivot;r.sizeDelta=size;
                r.localRotation=rotation;r.localScale=scale;
                r.localPosition=center-rotation*Vector3.Scale(r.rect.center,scale);
            }
        }
        struct WorldGeometry
        {
            public Geometry local;
            public Vector3 center, scale;
            public Quaternion rotation;
            public static WorldGeometry Read(RectTransform rect)=>new WorldGeometry {
                local=Geometry.Read(rect),center=rect.TransformPoint(rect.rect.center),
                scale=rect.lossyScale,rotation=rect.rotation };
            public void Apply(RectTransform rect)
            {
                local.Apply(rect);rect.rotation=rotation;
                var parentScale=rect.parent?rect.parent.lossyScale:Vector3.one;
                rect.localScale=new Vector3(scale.x/parentScale.x,scale.y/parentScale.y,scale.z/parentScale.z);
                rect.position=center-rect.TransformVector(rect.rect.center);
            }
        }
        sealed class Panel
        {
            public RectTransform rect;
            public bool visibility, visible;
            public CanvasGroup group;
            public float opacity=1;
            public bool outgoing;
            public WorldGeometry outgoingGeometry;
        }
        sealed class TextChange
        {
            public TMP_Text text, ghost;
            public CanvasGroup group;
            public string content;
            public int visibleCharacters;
            public Geometry geometry;
            public float elapsed=1,ghostOpacity;
        }
        sealed class Retired
        {
            public GameObject root;
            public RectTransform rect;
            public WorldGeometry geometry;
            public CanvasGroup group;
            public float elapsed;
            public float opacity;
        }

        public void Initialize(ElevatorView owner){view=owner;}
        Panel Track(RectTransform rect)
        {
            if(!rect)return null;
            foreach(var panel in panels)if(panel.rect==rect)return panel;
            var created=new Panel{rect=rect};
            panels.Add(created);return created;
        }
        void Discover()
        {
            var ui=view.spatial;
            TrackText(view.dialogueText);TrackText(ui.speaker);TrackText(view.subtitle);TrackText(ui.progress);
            TrackText(ui.notebookCaption);TrackText(ui.notebookDetail);
            if(thoughtHierarchy!=view.thoughtPanel.hierarchyCount)
            {
                thoughtTexts.Clear();view.thoughtPanel.GetComponentsInChildren(true,thoughtTexts);
                thoughtHierarchy=view.thoughtPanel.hierarchyCount;
            }
            foreach(var text in thoughtTexts)TrackText(text);
        }
        void TrackText(TMP_Text text)
        {
            if(!text||!text.gameObject.activeInHierarchy||text.name=="TransitionText"||text.name=="RetiredText"||text.name=="Heading"||text.name=="ThoughtHeading")return;
            if(IsThoughtHeader(text))return;
            foreach(var item in texts)if(item.text==text)return;
            var group=text.GetComponent<CanvasGroup>();if(!group)group=text.gameObject.AddComponent<CanvasGroup>();
            group.interactable=false;group.blocksRaycasts=false;
            texts.Add(new TextChange{text=text,group=group,content=text.text,visibleCharacters=text.maxVisibleCharacters,geometry=Geometry.Read(text.rectTransform)});
        }
        static void RemoveGhost(TextChange item)
        {
            if(!item.ghost)return;
            item.ghost.gameObject.SetActive(false);item.ghost.name="RetiredText";
            Destroy(item.ghost.gameObject);item.ghost=null;
        }
        bool IsThoughtHeader(TMP_Text text)
        {
            return text&&view&&view.thoughtPanel&&text.transform.IsChildOf(view.thoughtPanel)&&ElevatorThoughtHeaderPolicy.IsHeader(text);
        }
        public void Begin()
        {
            if(depth++>0)return;
            Discover();
            for(int i=panels.Count-1;i>=0;i--)if(!panels[i].rect)panels.RemoveAt(i);
        }
        public void End()
        {
            if(depth==0||--depth>0)return;
            Discover();
            RestoreOutgoingGeometry();
            foreach(var item in texts)
            {
                if(!item.text)continue;
                if(IsThoughtHeader(item.text))
                {RemoveGhost(item);item.content="";item.text.text="";item.text.gameObject.SetActive(false);continue;}
                if(!item.text.gameObject.activeInHierarchy)
                {RemoveGhost(item);item.content=item.text.text;item.elapsed=textSeconds;item.group.alpha=1;continue;}
                if(item.content!=item.text.text)
                {
                    RemoveGhost(item);
                    if(Application.isPlaying&&item.text.gameObject.activeInHierarchy&&item.group.alpha>.01f&&!string.IsNullOrEmpty(item.content))
                    {
                        item.ghost=Instantiate(item.text,item.text.transform.parent,false);item.ghost.name="TransitionText";
                        item.ghost.text=item.content;item.ghost.maxVisibleCharacters=item.visibleCharacters;item.ghost.raycastTarget=false;
                        item.geometry.Apply(item.ghost.rectTransform);item.ghost.transform.SetSiblingIndex(item.text.transform.GetSiblingIndex());
                        item.ghostOpacity=item.group.alpha;
                    }
                    item.content=item.text.text;item.elapsed=Application.isPlaying?0:textSeconds;
                }
                item.visibleCharacters=item.text.maxVisibleCharacters;item.geometry=Geometry.Read(item.text.rectTransform);
                ApplyText(item);
            }
        }
        public void Visibility(RectTransform rect,bool visible)
        {
            var panel=Track(rect);if(panel==null)return;
            if(!panel.visibility)
            {
                panel.visibility=true;panel.opacity=Application.isPlaying?0:visible?1:0;
                panel.group=rect.GetComponent<CanvasGroup>();if(!panel.group)panel.group=rect.gameObject.AddComponent<CanvasGroup>();
            }
            if(visible)
            {
                // World-space compensation belongs only to the outgoing frame.
                // Reopening/restarting must restore the authored local pose first.
                if(panel.outgoing)panel.outgoingGeometry.local.Apply(rect);
                panel.outgoing=false;
            }
            else if(panel.visible&&panel.opacity>.001f)
            {panel.outgoingGeometry=WorldGeometry.Read(rect);panel.outgoing=true;}
            panel.visible=visible;
            if(visible)rect.gameObject.SetActive(true);
            panel.group.interactable=visible;panel.group.blocksRaycasts=visible;
            if(!Application.isPlaying)panel.opacity=visible?1:0;
            panel.group.alpha=panel.opacity;
            if(!visible&&panel.opacity<=.001f)rect.gameObject.SetActive(false);
        }
        public bool IsVisible(RectTransform rect)
        {
            foreach(var panel in panels)if(panel.rect==rect&&panel.visibility)return panel.visible;
            return rect&&rect.gameObject.activeSelf;
        }
        public void FadeContent(RectTransform rect)
        {
            var panel=Track(rect);
            if(panel==null||!panel.visibility||!panel.visible)return;
            panel.opacity=0;panel.group.alpha=0;
        }
        public void RetireChoice(RectTransform rect)
        {
            if(!rect)return;
            var geometry=WorldGeometry.Read(rect);
            foreach(var button in rect.GetComponentsInChildren<Button>(true)){button.onClick.RemoveAllListeners();button.interactable=false;button.enabled=false;}
            foreach(var graphic in rect.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            panels.RemoveAll(p=>!p.rect||p.rect==rect||p.rect.IsChildOf(rect));
            for(int i=texts.Count-1;i>=0;i--)
                if(!texts[i].text||texts[i].text.transform.IsChildOf(rect)){RemoveGhost(texts[i]);texts.RemoveAt(i);}
            rect.name="Retiring_"+rect.name;rect.SetParent(view.actionPanel.parent,true);
            var group=rect.GetComponent<CanvasGroup>();if(!group)group=rect.gameObject.AddComponent<CanvasGroup>();
            group.interactable=false;group.blocksRaycasts=false;
            retired.Add(new Retired{root=rect.gameObject,rect=rect,geometry=geometry,group=group,opacity=group.alpha});
        }
        void RestoreOutgoingGeometry()
        {
            // Layout may relocate the live column while its old cards/buttons fade.
            // Keep outgoing geometry in world space, including after reparenting.
            foreach(var panel in panels)
                if(panel.rect&&panel.outgoing&&!panel.visible&&panel.opacity>0)panel.outgoingGeometry.Apply(panel.rect);
            foreach(var item in retired)if(item.rect)item.geometry.Apply(item.rect);
        }
        static float Amount(float delta,float duration)=>duration<=0?1:1-Mathf.Exp(-delta*5/Mathf.Max(.01f,duration));
        void ApplyText(TextChange item)
        {
            float progress=textSeconds<=0?1:Mathf.Clamp01(item.elapsed/textSeconds);
            // Fade through the change: overlapping two Korean sentences is hard to read.
            float amount=item.ghost?Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.5f)*2)):Mathf.SmoothStep(0,1,progress);
            item.group.alpha=amount;
            if(item.ghost)
            {
                item.ghost.GetComponent<CanvasGroup>().alpha=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(progress*2)))*item.ghostOpacity;
                if(progress>=1)RemoveGhost(item);
            }
        }
        public void Advance(float delta)
        {
            if(depth!=0||delta<0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            float fade=Amount(delta,fadeSeconds);
            foreach(var panel in panels)
            {
                if(!panel.rect)continue;
                if(!panel.visibility)continue;
                panel.opacity=Mathf.Lerp(panel.opacity,panel.visible?1:0,fade);
                if(Mathf.Abs(panel.opacity-(panel.visible?1:0))<.001f)panel.opacity=panel.visible?1:0;
                panel.group.alpha=panel.opacity;
                if(!panel.visible&&panel.opacity==0)panel.rect.gameObject.SetActive(false);
            }
            for(int i=texts.Count-1;i>=0;i--)
            {
                var item=texts[i];if(!item.text){RemoveGhost(item);texts.RemoveAt(i);continue;}
                if(IsThoughtHeader(item.text)||(item.text.transform.IsChildOf(view.thoughtPanel)&&ElevatorThoughtHeaderPolicy.IsTitle(item.content)))
                {RemoveGhost(item);item.text.text="";item.text.gameObject.SetActive(false);texts.RemoveAt(i);continue;}
                if(!item.text.gameObject.activeInHierarchy){RemoveGhost(item);item.elapsed=textSeconds;item.group.alpha=1;continue;}
                item.elapsed+=delta;ApplyText(item);
            }
            for(int i=retired.Count-1;i>=0;i--)
            {
                var item=retired[i];item.elapsed+=delta;
                if(!item.root||item.elapsed>=fadeSeconds){if(item.root)Destroy(item.root);retired.RemoveAt(i);}
                else item.group.alpha=item.opacity*(1-Mathf.SmoothStep(0,1,item.elapsed/Mathf.Max(.01f,fadeSeconds)));
            }
            RestoreOutgoingGeometry();
        }
        void OnDestroy(){foreach(var item in texts)if(item.ghost)Destroy(item.ghost.gameObject);foreach(var item in retired)if(item.root)Destroy(item.root);}
    }
}
