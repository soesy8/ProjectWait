using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator
{
    // Layout always calculates final geometry. Only the displayed RectTransforms interpolate.
    // Input therefore uses the same moving geometry as the artwork, on desktop and XR.
    [DisallowMultipleComponent]
    public sealed class ElevatorUITransitions : MonoBehaviour
    {
        public float layoutSeconds = .22f, fadeSeconds = .18f, textSeconds = .16f;
        ElevatorView view;
        int depth;
        readonly List<Panel> panels = new List<Panel>();
        readonly List<TextChange> texts = new List<TextChange>();
        readonly List<Retired> retired = new List<Retired>();

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
            public static Geometry Blend(Geometry from,Geometry to,float amount)
            {
                var result=to;
                result.center=Vector3.Lerp(from.center,to.center,amount);
                result.size=Vector2.Lerp(from.size,to.size,amount);
                result.scale=Vector3.Lerp(from.scale,to.scale,amount);
                result.rotation=Quaternion.Slerp(from.rotation,to.rotation,amount);
                if(Vector3.Distance(result.center,to.center)<.02f)result.center=to.center;
                if(Vector2.Distance(result.size,to.size)<.02f)result.size=to.size;
                if(Vector3.Distance(result.scale,to.scale)<.0001f)result.scale=to.scale;
                if(Quaternion.Angle(result.rotation,to.rotation)<.01f)result.rotation=to.rotation;
                return result;
            }
        }
        sealed class Panel
        {
            public RectTransform rect;
            public Transform parent;
            public Geometry target, display;
            public bool ready, world, visibility, visible;
            public CanvasGroup group;
            public float opacity=1;
            public Vector3 worldCenter,worldScale;
            public Quaternion worldRotation;
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
            public CanvasGroup group;
            public float elapsed;
            public float opacity;
        }

        public void Initialize(ElevatorView owner){view=owner;}
        Panel Track(RectTransform rect,bool world=false)
        {
            if(!rect)return null;
            foreach(var panel in panels)if(panel.rect==rect){panel.world|=world;return panel;}
            var created=new Panel{rect=rect,parent=rect.parent,target=Geometry.Read(rect),display=Geometry.Read(rect),world=world};
            panels.Add(created);return created;
        }
        void Tree(RectTransform root,bool world=false)
        {
            if(!root)return;
            // New text uses its final line box immediately; fading a clipped intermediate
            // line box would reintroduce the overflow this UI's typography avoids.
            if(root.GetComponent<TMP_Text>())return;
            Track(root,world);
            foreach(Transform child in root)
            {
                var r=child as RectTransform;
                if(!r||child.name=="TransitionText"||child.name=="RetiredText"||child.name.StartsWith("Retiring_"))continue;
                // Button surfaces have their own pointer feedback; never fight its offsets.
                if(root.GetComponent<Button>())continue;
                if(r.anchorMin==r.anchorMax)Tree(r);
            }
        }
        void Discover()
        {
            var ui=view.spatial;
            Tree(view.dialoguePanel,true);Tree(ui.speakerTab,true);Tree(view.actionPanel,view.actionPanel.parent==view.transform);
            Tree(view.thoughtPanel);Tree(ui.mission);
            TrackText(view.dialogueText);TrackText(ui.speaker);TrackText(view.subtitle);TrackText(ui.progress);
            TrackText(ui.notebookCaption);TrackText(ui.notebookDetail);
            foreach(var text in view.thoughtPanel.GetComponentsInChildren<TMP_Text>(true))TrackText(text);
        }
        void TrackText(TMP_Text text)
        {
            if(!text||text.name=="TransitionText"||text.name=="RetiredText")return;
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
        public void Begin()
        {
            if(depth++>0)return;
            Discover();
            for(int i=panels.Count-1;i>=0;i--)if(!panels[i].rect)panels.RemoveAt(i);
            foreach(var panel in panels)
            {
                var r=panel.rect;panel.display=Geometry.Read(r);
                panel.worldCenter=r.TransformPoint(r.rect.center);panel.worldRotation=r.rotation;panel.worldScale=r.lossyScale;
                if(panel.ready&&panel.parent==r.parent)panel.target.Apply(r);
            }
        }
        public void End()
        {
            if(depth==0||--depth>0)return;
            Discover();
            foreach(var panel in panels)
            {
                var r=panel.rect;panel.target=Geometry.Read(r);
                bool newParent=panel.parent!=r.parent;
                if(!panel.ready||newParent||!Application.isPlaying)
                {panel.display=panel.target;panel.ready=true;panel.parent=r.parent;}
                else if(panel.world&&r.parent)
                {
                    var position=r.parent.InverseTransformPoint(panel.worldCenter);
                    // Large stage/teleport changes fade at the destination, never fly across the room.
                    if(Vector3.Distance(panel.worldCenter,r.TransformPoint(r.rect.center))>1.25f)
                    {
                        panel.display=panel.target;
                        if(panel.visibility&&panel.visible)panel.opacity=0;
                    }
                    else
                    {
                        panel.display.center=position;
                        panel.display.rotation=Quaternion.Inverse(r.parent.rotation)*panel.worldRotation;
                        var scale=r.parent.lossyScale;
                        panel.display.scale=new Vector3(panel.worldScale.x/scale.x,panel.worldScale.y/scale.y,panel.worldScale.z/scale.z);
                    }
                }
                panel.display.min=panel.target.min;panel.display.max=panel.target.max;panel.display.pivot=panel.target.pivot;
                panel.display.Apply(r);
            }
            foreach(var item in texts)
            {
                if(!item.text)continue;
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
        public void RetireChoice(RectTransform rect)
        {
            if(!rect)return;
            foreach(var button in rect.GetComponentsInChildren<Button>(true)){button.onClick.RemoveAllListeners();button.interactable=false;button.enabled=false;}
            foreach(var graphic in rect.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            panels.RemoveAll(p=>!p.rect||p.rect==rect||p.rect.IsChildOf(rect));
            for(int i=texts.Count-1;i>=0;i--)
                if(!texts[i].text||texts[i].text.transform.IsChildOf(rect)){if(texts[i].ghost)Destroy(texts[i].ghost.gameObject);texts.RemoveAt(i);}
            rect.name="Retiring_"+rect.name;rect.SetParent(view.actionPanel.parent,true);
            var group=rect.GetComponent<CanvasGroup>();if(!group)group=rect.gameObject.AddComponent<CanvasGroup>();
            group.interactable=false;group.blocksRaycasts=false;
            retired.Add(new Retired{root=rect.gameObject,group=group,opacity=group.alpha});
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
            float motion=Amount(delta,layoutSeconds),fade=Amount(delta,fadeSeconds);
            foreach(var panel in panels)
            {
                if(!panel.rect||!panel.ready)continue;
                panel.display=Geometry.Blend(panel.display,panel.target,motion);panel.display.Apply(panel.rect);
                if(!panel.visibility)continue;
                panel.opacity=Mathf.Lerp(panel.opacity,panel.visible?1:0,fade);
                if(Mathf.Abs(panel.opacity-(panel.visible?1:0))<.001f)panel.opacity=panel.visible?1:0;
                panel.group.alpha=panel.opacity;
                if(!panel.visible&&panel.opacity==0)panel.rect.gameObject.SetActive(false);
            }
            for(int i=texts.Count-1;i>=0;i--)
            {
                var item=texts[i];if(!item.text){if(item.ghost)Destroy(item.ghost.gameObject);texts.RemoveAt(i);continue;}
                item.elapsed+=delta;ApplyText(item);
            }
            for(int i=retired.Count-1;i>=0;i--)
            {
                var item=retired[i];item.elapsed+=delta;
                if(!item.root||item.elapsed>=fadeSeconds){if(item.root)Destroy(item.root);retired.RemoveAt(i);}
                else item.group.alpha=item.opacity*(1-Mathf.SmoothStep(0,1,item.elapsed/Mathf.Max(.01f,fadeSeconds)));
            }
        }
        void OnDestroy(){foreach(var item in texts)if(item.ghost)Destroy(item.ghost.gameObject);foreach(var item in retired)if(item.root)Destroy(item.root);}
    }
}