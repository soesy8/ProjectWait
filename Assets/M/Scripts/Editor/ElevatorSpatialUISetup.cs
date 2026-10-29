#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ProjectWait.M.Elevator;
using DwtD.TestScene;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorSpatialUISetup
    {
        const string Models="Assets/M/Arts/Models/ElevatorUI/";
        const string Mats="Assets/M/Arts/Materials/ElevatorUI/";
        const string Prefabs="Assets/M/Prefabs/ElevatorUI/";
        static readonly Color Ink=new Color(.28f,.19f,.14f),Paper=new Color(1,.965f,.86f),Teal=new Color(.16f,.48f,.44f),Mint=new Color(.79f,.89f,.76f);
        static ElevatorView view;
        static ElevatorSpatialUI spatial;
        static Material cover,paper,edge,ring;

        [MenuItem("Wait/M/Apply Spatial Elevator UI")]
        public static string Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/M/Scene/Elevator_Safety_Prototype.unity"||scene.isDirty)
                throw new InvalidOperationException("Open the saved M elevator scene in Edit Mode first.");
            view=Object.FindFirstObjectByType<ElevatorView>();
            if(view.spatial)throw new InvalidOperationException("Spatial UI already exists. Edit its anchors and surfaces in place.");
            Directory.CreateDirectory(Models);Directory.CreateDirectory(Mats);Directory.CreateDirectory(Prefabs);
            spatial=view.gameObject.AddComponent<ElevatorSpatialUI>();view.spatial=spatial;spatial.view=view;
            var serialized=new SerializedObject(view);
            spatial.mission=(RectTransform)serialized.FindProperty("mission").objectReferenceValue;
            spatial.speaker=(TMP_Text)serialized.FindProperty("speaker").objectReferenceValue;
            spatial.progress=(TMP_Text)serialized.FindProperty("progress").objectReferenceValue;
            spatial.controls=(TMP_Text)serialized.FindProperty("controls").objectReferenceValue;
            spatial.intercomAnchor=view.lesson.presentation.intercomButton;
            spatial.rescuerAnchor=view.lesson.presentation.rescuer.transform;

            var hudRect=Rect("Elevator_Screen_Thoughts",view.lesson.transform,new Vector2(1280,720),Vector2.zero);
            spatial.hud=hudRect.gameObject.AddComponent<Canvas>();spatial.hud.renderMode=RenderMode.ScreenSpaceCamera;spatial.hud.worldCamera=view.playerCamera;spatial.hud.sortingOrder=35;
            var hudGroup=hudRect.gameObject.AddComponent<CanvasGroup>();hudGroup.interactable=false;hudGroup.blocksRaycasts=false;
            hudRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            foreach(var item in new Transform[]{spatial.mission,spatial.progress.transform,spatial.controls.transform})item.SetParent(hudRect,false);
            spatial.mission.GetComponent<CorridorBubbleGraphic>().extrusion=5;
            view.title.fontSize=13;view.subtitle.fontSize=21;
            view.thoughtPanel=Card("PlayerThought",hudRect,new Vector2(480,120),Vector2.zero,Paper);
            var thoughtGraphic=view.thoughtPanel.GetComponent<CorridorBubbleGraphic>();thoughtGraphic.extrusion=7;thoughtGraphic.bevelWidth=4;thoughtGraphic.borderWidth=2;thoughtGraphic.cornerRadius=22;
            spatial.thoughtHeading=Text("ThoughtHeading",view.thoughtPanel,new Vector2(428,26),Vector2.zero,"나의 생각 · 힌트",16,Teal);
            view.thought=Text("Thought",view.thoughtPanel,new Vector2(428,66),new Vector2(0,-16),"",24,Ink);
            view.thoughtPanel.gameObject.SetActive(false);
            view.infoPanel=view.thoughtPanel;view.infoText=view.thought;

            view.gameObject.name="Elevator_World_Dialogue";
            var dialogueGraphic=view.dialoguePanel.GetComponent<CorridorBubbleGraphic>();
            dialogueGraphic.speechTail=true;dialogueGraphic.extrusion=8;dialogueGraphic.bevelWidth=5;dialogueGraphic.borderWidth=2.7f;dialogueGraphic.cornerRadius=22;dialogueGraphic.shadowOffset=new Vector2(5,-11);
            view.dialogueText.fontSize=32;view.dialogueText.color=Ink;
            spatial.speakerTab=Card("SpeakerTab",view.dialoguePanel,new Vector2(130,46),Vector2.zero,Teal);
            spatial.speaker.transform.SetParent(spatial.speakerTab,false);spatial.speaker.color=Paper;spatial.speaker.fontSize=23;
            spatial.speaker.text="친구";
            view.actionPanel.SetParent(view.transform,false);
            view.developerPanel.localPosition=new Vector3(0,0,-20);
            foreach(var b in view.developerPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))spatial.StyleButton(b);
            var scaler=view.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.dynamicPixelsPerUnit=10;

            cover=Material("TealCover",Teal,.3f);paper=Material("Paper",Paper,.8f);edge=Material("PageEdges",new Color(.84f,.73f,.53f),.35f);ring=Material("Binding",new Color(.28f,.56f,.51f),.3f);
            spatial.notebook=new GameObject("Completion_3D_Notebook").transform;spatial.notebook.SetParent(view.lesson.transform,false);
            MakeNotebook();
            view.dialogueText.text=view.lesson.settings.lobbyDialogue;view.subtitle.text="농구를 마치고, 친구 집으로";
            spatial.Layout();spatial.notebook.gameObject.SetActive(false);
            // Store useful artwork templates with no scene or lesson references.
            SaveTemplates();
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(spatial);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "World dialogue + choices, screen thoughts and a mesh notebook saved in Assets/M. Reusable templates: "+Prefabs;
        }

        static void MakeNotebook()
        {
            var book=spatial.notebook;
            Mesh panel=RoundedBox("NotebookPanel",.53f,.64f,.025f,.026f);
            Mesh pages=RoundedBox("PageBlock",.50f,.606f,.023f,.018f);
            Mesh line=RoundedBox("PageEdge",.488f,.0016f,.0018f,.0005f);
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                var leaf=new GameObject(side==0?"LeftLeaf":"RightLeaf").transform;leaf.SetParent(book,false);
                leaf.localPosition=new Vector3(sign*.275f,0,0);leaf.localRotation=Quaternion.Euler(0,sign*-5,0);
                MeshObject("HardCover",leaf,panel,cover,new Vector3(0,0,.035f));
                MeshObject("PaperBlock",leaf,pages,edge,new Vector3(0,0,.010f));
                MeshObject("TopPage",leaf, RoundedBox("TopPage",.494f,.599f,.006f,.018f),paper,new Vector3(0,0,-.005f));
                for(int i=0;i<3;i++)MeshObject("VisiblePageLayer"+i,leaf,line,paper,new Vector3(0,-.299f,.005f+i*.006f));
                var canvasRect=Rect("PageSurface",leaf,new Vector2(480,576),Vector2.zero);canvasRect.localPosition=new Vector3(0,0,-.011f);canvasRect.localScale=Vector3.one*.001f;
                var c=canvasRect.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=view.playerCamera;c.sortingOrder=24;
                canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();canvasRect.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                if(side==0)spatial.leftPage=canvasRect;else spatial.rightPage=canvasRect;
            }
            var spine=RoundedBox("Spine",.05f,.641f,.04f,.014f);MeshObject("BookSpine",book,spine,cover,new Vector3(0,0,.030f));
            var torus=Torus();
            for(int i=0;i<5;i++)MeshObject("BindingRing"+i,book,torus,ring,new Vector3(0,-.23f+i*.115f,-.004f));
            Text("NotebookTitle",spatial.leftPage,new Vector2(426,64),new Vector2(0,230),"우리 둘이 해냈어!",32,Ink);
            Text("NotebookEyebrow",spatial.leftPage,new Vector2(420,24),new Vector2(0,273),"함께 쓴 안전 기록",15,Teal);
            string[] labels={"도움 요청","위치 전달","함께 대기"};
            spatial.completionMarks=new TMP_Text[3];spatial.completionRows=new RectTransform[3];
            for(int i=0;i<3;i++)
            {
                var row=Card("CompletedStep"+i,spatial.leftPage,new Vector2(430,85),new Vector2(0,130-i*105),Paper);spatial.completionRows[i]=row;
                var icon=Rect("Icon",row,new Vector2(54,54),new Vector2(-171,0)).gameObject.AddComponent<ElevatorMemoryGraphic>();icon.illustration=(ElevatorMemoryGraphic.Illustration)(i+1);icon.raycastTarget=false;
                Text("StepLabel",row,new Vector2(185,50),new Vector2(-35,0),labels[i],29,Ink);
                var badge=Card("DoneBadge",row,new Vector2(67,44),new Vector2(157,0),Mint);
                spatial.completionMarks[i]=Text("Done",badge,new Vector2(62,36),Vector2.zero,"완료",19,Teal);spatial.completionMarks[i].alignment=TextAlignmentOptions.Center;
            }
            Text("NotebookFooter",spatial.leftPage,new Vector2(416,88),new Vector2(0,-206),"도움을 청하는 용기,\n함께 기다리는 마음을 기억해요.",24,Teal);
            var picture=Rect("TeamworkIllustration",spatial.rightPage,new Vector2(405,254),new Vector2(0,130)).gameObject.AddComponent<ElevatorMemoryGraphic>();picture.illustration=ElevatorMemoryGraphic.Illustration.Teamwork;picture.raycastTarget=false;
            var caption=Card("PictureCaption",spatial.rightPage,new Vector2(430,54),new Vector2(0,-23),Paper);
            caption.localRotation=Quaternion.Euler(0,0,2);
            spatial.notebookCaption=Text("MemoryTitle",caption,new Vector2(402,44),Vector2.zero,"함께 안전하게 기다렸어요.",24,Ink);spatial.notebookCaption.alignment=TextAlignmentOptions.Center;
            spatial.notebookDetail=Text("MemoryDetail",spatial.rightPage,new Vector2(426,84),new Vector2(0,-112),"도움을 요청하고, 위치를 알리고,\n친구와 함께 안내를 기다렸어요.",22,Teal);spatial.notebookDetail.alignment=TextAlignmentOptions.Center;
        }

        [MenuItem("Wait/M/Refresh Spatial UI Templates")]
        public static void RefreshTemplates()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            view=Object.FindFirstObjectByType<ElevatorView>();spatial=view.spatial;
            if(!spatial)throw new InvalidOperationException("Apply the spatial UI first.");
            SaveTemplates();
        }
        static void SaveTemplates()
        {
            var root=Rect("WorldDialogue",null,new Vector2(580,460),Vector2.zero);
            try
            {
                var c=root.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;root.localScale=Vector3.one*spatial.worldPixelSize;
                root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                var copy=Object.Instantiate(view.dialoguePanel,root,false);copy.name="SpeechCard";
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"WorldDialogue.prefab");
            }
            finally{Object.DestroyImmediate(root.gameObject);}
            var choice=Rect("WorldChoice",null,new Vector2(510,100),Vector2.zero);
            try
            {
                var c=choice.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;choice.localScale=Vector3.one*spatial.worldPixelSize;
                choice.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();choice.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                var card=Card("ChoiceButton",choice,new Vector2(510,88),Vector2.zero,Teal);
                var graphic=card.GetComponent<CorridorBubbleGraphic>();graphic.raycastTarget=true;
                var button=card.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=graphic;
                Text("Label",card,new Vector2(466,68),Vector2.zero,"함께 기다리자.",28,Paper).alignment=TextAlignmentOptions.Center;spatial.StyleButton(button);
                PrefabUtility.SaveAsPrefabAsset(choice.gameObject,Prefabs+"WorldChoice.prefab");
            }
            finally{Object.DestroyImmediate(choice.gameObject);}
            var screen=Rect("ScreenThought",null,new Vector2(1280,720),Vector2.zero);
            try
            {
                screen.gameObject.AddComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=screen.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
                var copy=Object.Instantiate(view.thoughtPanel,screen,false);copy.name="PlayerThought";copy.gameObject.SetActive(true);copy.Find("Thought").GetComponent<TMP_Text>().text="도움을 요청할 장치를 찾아보자.";
                PrefabUtility.SaveAsPrefabAsset(screen.gameObject,Prefabs+"ScreenThought.prefab");
            }
            finally{Object.DestroyImmediate(screen.gameObject);}
            var clone=Object.Instantiate(spatial.notebook.gameObject);clone.name="OpenNotebook3D";
            try{foreach(var c in clone.GetComponentsInChildren<Canvas>())c.worldCamera=null;PrefabUtility.SaveAsPrefabAsset(clone,Prefabs+"OpenNotebook3D.prefab");}
            finally{Object.DestroyImmediate(clone);}
        }

        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        static RectTransform Card(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
        {
            var r=Rect(name,parent,size,pos);var g=r.gameObject.AddComponent<CorridorBubbleGraphic>();g.color=color;g.borderColor=Ink;g.cornerRadius=14;g.borderWidth=1.8f;g.extrusion=4;g.bevelWidth=3;g.shadowOffset=new Vector2(3,-6);g.shadowOpacity=.12f;g.raycastTarget=false;return r;
        }
        static TMP_Text Text(string name,Transform parent,Vector2 size,Vector2 pos,string value,float point,Color color)
        {
            var r=Rect(name,parent,size,pos);r.localPosition+=Vector3.back*.2f;
            var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=view.font;t.fontSize=point;t.color=color;t.text=value;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;return t;
        }
        static Material Material(string name,Color color,float unlit)
        {
            string path=Mats+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("ProjectWait/M/Elevator Storybook Toon"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetColor("_OutlineColor",new Color32(73,51,39,255));m.SetColor("_ShadeColor",new Color32(189,200,195,255));m.SetFloat("_MidTone",.90f);m.SetFloat("_ShadowThreshold",.02f);m.SetFloat("_LightThreshold",.62f);m.SetFloat("_BandSoftness",.012f);m.SetFloat("_EmissionStrength",unlit);m.SetFloat("_OutlineWidth",.8f);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        static void MeshObject(string name,Transform parent,Mesh mesh,Material mat,Vector3 pos)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
        {
            string path=Models+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool exists=mesh;
            if(!mesh)mesh=new Mesh();else mesh.Clear();mesh.name=name;mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(!exists)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);}return mesh;
        }
        static Mesh RoundedBox(string name,float w,float h,float depth,float radius)
        {
            string existing=Models+name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(existing);if(prior)return prior;
            var contour=new List<Vector2>();
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?w*.5f-radius:-w*.5f+radius,corner<2?h*.5f-radius:-h*.5f+radius);
                for(int i=0;i<=8;i++){float a=(corner*90+i*90f/8)*Mathf.Deg2Rad;contour.Add(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
            }
            var v=new List<Vector3>();var t=new List<int>();int n=contour.Count;
            for(int side=0;side<2;side++)
            {
                float z=(side==0?-1:1)*depth*.5f;int start=v.Count;v.Add(new Vector3(0,0,z));foreach(var p in contour)v.Add(new Vector3(p.x,p.y,z));
                for(int i=0;i<n;i++){if(side==0)t.AddRange(new[]{start,start+(i+1)%n+1,start+i+1});else t.AddRange(new[]{start,start+i+1,start+(i+1)%n+1});}
            }
            for(int i=0;i<n;i++)
            {
                var a=contour[i];var b=contour[(i+1)%n];int start=v.Count;
                v.Add(new Vector3(a.x,a.y,-depth*.5f));v.Add(new Vector3(b.x,b.y,-depth*.5f));v.Add(new Vector3(b.x,b.y,depth*.5f));v.Add(new Vector3(a.x,a.y,depth*.5f));t.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            return SaveMesh(name,v,t);
        }
        static Mesh Torus()
        {
            var v=new List<Vector3>();var t=new List<int>();const int segments=40,tube=10;
            for(int i=0;i<=segments;i++)for(int j=0;j<=tube;j++)
            {float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/tube,r=.028f+.0055f*Mathf.Cos(b);v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*.0055f,Mathf.Sin(a)*r));}
            for(int i=0;i<segments;i++)for(int j=0;j<tube;j++){int a=i*(tube+1)+j,b=a+tube+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            return SaveMesh("BindingRing",v,t);
        }
    }
}
#endif
