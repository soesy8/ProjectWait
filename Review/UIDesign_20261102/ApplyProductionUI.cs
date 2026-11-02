using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class ApplyProductionUI
{
    const string Root = "Assets/M/UI/Storybook/";
    static Color Ink = new Color32(58,36,24,255), Muted = new Color32(98,108,85,255), Paper = new Color32(255,249,231,255);
    static ElevatorStorybookUI skin;
    static string artFolder = "";
    static TMP_FontAsset font;
    static readonly List<string> prefabs = new List<string>();
    static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Root + "Art/" + artFolder + name + ".png").OfType<Sprite>().Single();
    public static object Cartoon() { artFolder = "Cartoon/"; return Main(); }
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode.");
        if (SceneManager.GetActiveScene().isDirty) throw new Exception("Current scene has unsaved edits.");
        var original = SceneManager.GetActiveScene().path;
        var results = new List<object>();
        foreach (string path in new[] { "Assets/M/Scene/Elevator_Safety_Prototype.unity", "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity" })
        {
            string backup = "Review/UIDesign_20261102/BeforeProduction/" + Path.GetFileName(path);
            if (!File.Exists(backup)) File.Copy(path, backup);
            var scene = EditorSceneManager.OpenScene(path);
            var view = Object.FindFirstObjectByType<ElevatorView>();
            if (!view || !view.spatial) throw new Exception("Missing spatial dialogue: " + path);
            skin = view.GetComponent<ElevatorStorybookUI>();
            if (!skin) skin = view.gameObject.AddComponent<ElevatorStorybookUI>();
            skin.view = view; view.spatial.storybook = skin;
            skin.dialogueFrame = Sprite("DialogueFrame"); skin.thoughtCloud = Sprite("ThoughtCloud");
            skin.primaryButton = Sprite("ButtonTeal"); skin.secondaryButton = Sprite("ButtonCream");
            skin.speakerRibbon = Sprite("SpeakerRibbon"); skin.thoughtRibbon = Sprite("ThoughtRibbon");
            skin.observationNote = Sprite("ObservationNote"); skin.friendPortrait = Sprite("FriendPortrait"); skin.speechTail = Sprite("SpeechTail");
            skin.worldMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/M/Arts/Materials/ElevatorInteraction/UIOverlay.mat");
            font = view.font; skin.ApplyStatic(); skin.SkinPause(view.lesson.pauseMenu);
            // Use the existing narrative layout and anchors; only presentation is replaced.
            float body = Mathf.Max(64,view.dialogueText.GetPreferredValues(view.dialogueText.text,418,0).y+8);
            view.dialoguePanel.sizeDelta=new Vector2(510,body+88);
            skin.LayoutWorld();skin.LayoutThought(false);
            foreach (var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true)) skin.SkinButton(button);
            EditorUtility.SetDirty(skin); EditorUtility.SetDirty(view.spatial);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            int missing = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing != 0) throw new Exception("Missing scripts after UI application.");
            results.Add(new { path, missingScripts = missing, imageCount = view.GetComponentsInChildren<UnityEngine.UI.Image>(true).Length });
        }
        EditorSceneManager.OpenScene("Assets/M/Scene/Elevator_Safety_Prototype.unity");
        skin = Object.FindFirstObjectByType<ElevatorStorybookUI>(); font = skin.view.font;
        MakePrefabs(); MakeGallery();
        EditorSceneManager.OpenScene(original);
        var report = new { scenes = results, prefabs = prefabs.ToArray(), sprites = 9 };
        File.WriteAllText("Review/UIDesign_20261102/"+(artFolder==""?"production":"cartoon")+"-application.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }

    static RectTransform Rect(string name, Transform parent, float w, float h, float x=0,float y=0)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
        if(parent)r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return r;
    }
    static TMP_Text Text(Transform p,string name,string value,float size,Color color,float w,float h,float x,float y,bool bold=false,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var t=Rect(name,p,w,h,x,y).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;
        t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.alignment=align;
        t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;t.lineSpacing=5;t.extraPadding=true;return t;
    }
    static UnityEngine.UI.Image Art(Transform p,string name,Sprite sprite,float w,float h,float x=0,float y=0,bool sliced=false)
    {
        var r=Rect(name,p,w,h,x,y);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();
        i.sprite=sprite;i.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
        i.pixelsPerUnitMultiplier=4;i.raycastTarget=false;return i;
    }
    static RectTransform RootCard(string name,float w,float h,Sprite sprite,bool sliced=true)
    {
        var r=Rect(name,null,w,h);var art=Art(r,"Artwork",sprite,w,h,0,0,sliced);
        art.rectTransform.anchorMin=Vector2.zero;art.rectTransform.anchorMax=Vector2.one;
        art.rectTransform.offsetMin=art.rectTransform.offsetMax=Vector2.zero;return r;
    }
    static RectTransform Button(Transform p,string name,string text,float w,float h,float x,float y,bool primary)
    {
        var r=Rect(name,p,w,h,x,y);
        var t=Text(r,"Label",text,28,primary?Paper:Ink,w-100,h-22,0,0,true,TextAlignmentOptions.Center);
        var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();
        skin.SkinButton(b);
        var image=b.targetGraphic as UnityEngine.UI.Image;
        image.sprite=primary?skin.primaryButton:skin.secondaryButton;image.material=null;
        t.color=primary?Paper:Ink;t.fontSize=28;
        return r;
    }
    static RectTransform Dialogue(bool call)
    {
        var r=RootCard(call?"IntercomDialogueCard":"DialogueCard",760,254,skin.dialogueFrame);
        Art(r,"SpeechTail",skin.speechTail,98,58,-155,-139).transform.SetSiblingIndex(0);
        if(!call)Art(r,"FriendPortrait",skin.friendPortrait,118,118,-302,100);
        Art(r,"SpeakerRibbon",call?skin.thoughtRibbon:skin.speakerRibbon,call?292:170,61,call?-205:-170,118);
        Text(r,"Speaker",call?"관리실 · 통화 중":"친구",call?24:26,Ink,call?250:130,46,call?-205:-170,119,true,TextAlignmentOptions.Center);
        Text(r,"Dialogue",call?"괜찮아요. 지금 계신 곳을\n천천히 알려 주세요.":"조금 놀랐어…\n우리 같이 천천히 기다릴까?",34,Ink,642,134,0,-10);
        return r;
    }
    static RectTransform Thought()
    {
        var r=RootCard("ThoughtCloud",520,368,skin.thoughtCloud,false);
        Art(r,"ThoughtRibbon",skin.thoughtRibbon,178,56,0,111);
        Text(r,"Heading","나의 생각",22,Ink,140,40,0,112,true,TextAlignmentOptions.Center);
        Text(r,"Thought","친구가 많이 놀랐구나.\n내가 옆에 있어 줘야겠어.",28,Ink,364,110,0,2,false,TextAlignmentOptions.Center);
        return r;
    }
    static RectTransform Observation()
    {
        var r=RootCard("ObservationCard",520,254,skin.observationNote,false);
        Text(r,"Heading","살펴보니",22,Ink,140,38,-42,75,true,TextAlignmentOptions.Center);
        Text(r,"Description","이 버튼으로 관리실에\n도움을 요청할 수 있겠어.",27,Ink,382,96,-3,-31);return r;
    }
    static RectTransform Choices()
    {
        var r=Rect("ChoiceCard",null,760,285);
        var options=Rect("Options",r,760,278);
        var layout=options.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing=13;layout.childControlWidth=layout.childControlHeight=true;
        layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        string[] labels={"괜찮아, 내가 옆에 있을게.","우리 같이 도움을 요청하자.","주변을 조금 더 살펴볼래."};
        for(int i=0;i<labels.Length;i++)
        {
            var row=Button(options,"Choice"+i,labels[i],760,82,0,0,i==0);
            var e=row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();e.preferredHeight=e.minHeight=82;
        }
        return r;
    }
    static RectTransform Mission()
    {
        var r=RootCard("MissionCard",520,168,skin.dialogueFrame);
        Text(r,"Heading","지금 우리가 할 일",19,Muted,420,30,0,37);
        Text(r,"Objective","도움을 요청해요",30,Ink,420,52,0,-10,true);return r;
    }
    static RectTransform Completion()
    {
        var r=RootCard("CompletionCard",520,384,skin.dialogueFrame);
        Text(r,"Heading","오늘의 안전 기록",19,Muted,414,32,0,138);
        Text(r,"Title","우리, 함께 해냈어!",31,Ink,414,53,0,88,true);
        string[] labels={"도움 요청  ·  완료","위치 전달  ·  완료","함께 대기  ·  완료"};
        for(int i=0;i<3;i++)Text(r,"Done"+i,labels[i],24,Ink,414,37,0,23-i*42);
        Button(r,"KeepMemory","이 마음, 기억할게",424,64,0,-133,true);return r;
    }
    static RectTransform Pause()
    {
        var r=RootCard("PausePanel",520,438,skin.dialogueFrame);
        Text(r,"Heading","잠깐 쉬어 가요",19,Muted,414,32,0,161,false,TextAlignmentOptions.Center);
        Text(r,"Title","준비되면 다시 함께!",29,Ink,414,53,0,112,true,TextAlignmentOptions.Center);
        Button(r,"ResumeButton","이어서 하기",430,68,0,31,true);
        Button(r,"RestartButton","처음부터 다시 하기",430,68,0,-55,false);
        Button(r,"TitleButton","타이틀로 돌아가기",430,68,0,-141,false);return r;
    }
    static void Save(string name,RectTransform content)
    {
        var root=Rect(name+"_WorldCanvas",null,content.rect.width,content.rect.height);
        root.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().dynamicPixelsPerUnit=10;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        content.SetParent(root,false);root.localScale=Vector3.one*.0015f;
        foreach(var i in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))i.material=null;
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"Prefabs/"+name+".prefab");
        prefabs.Add(Root+"Prefabs/"+name+".prefab");Object.DestroyImmediate(root.gameObject);
    }
    static void MakePrefabs()
    {
        Save("DialogueCard",Dialogue(false));Save("IntercomDialogueCard",Dialogue(true));
        Save("ThoughtCloud",Thought());Save("ObservationCard",Observation());Save("ChoiceCard",Choices());
        Save("MissionCard",Mission());Save("CompletionCard",Completion());Save("PausePanel",Pause());
        var next=Button(null,"DialogueNext","다음  ›",196,74,0,0,true);Save("NextButton",next);
    }
    static void Place(string name,Transform board,float x,float y,float scale=1)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+name+".prefab"));
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var r=(RectTransform)go.transform;r.SetParent(board,false);r.anchoredPosition=new Vector2(x,y);r.localScale=Vector3.one*scale;
        Object.DestroyImmediate(go.GetComponent<UnityEngine.UI.GraphicRaycaster>());Object.DestroyImmediate(go.GetComponent<TrackedDeviceGraphicRaycaster>());
        Object.DestroyImmediate(go.GetComponent<UnityEngine.UI.CanvasScaler>());Object.DestroyImmediate(go.GetComponent<Canvas>());
    }
    static void MakeGallery()
    {
        var source=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var board=Rect("StorybookUI_Gallery",null,1920,1320);
        board.gameObject.AddComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=board.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1320);scaler.matchWidthOrHeight=.5f;board.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var bg=Rect("Background",board,2400,1800).gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color32(233,235,213,255);bg.raycastTarget=false;
        Text(board,"Title","함께, 천천히.",48,Ink,1000,77,-385,575,true);
        Text(board,"Subtitle","게임 속 색과 선으로 맞춘 카툰 UI",21,Muted,1000,35,-385,516);
        Text(board,"Edition","DwtD  /  CARTOON UI",17,Muted,510,35,640,575,false,TextAlignmentOptions.Right);
        Text(board,"DialogueHeading","대화",22,Muted,780,36,-468,430,true);
        Place("DialogueCard",board,-468,242,1.02f);Place("NextButton",board,-174,51,.90f);
        Place("ChoiceCard",board,-468,-177,1.02f);
        Text(board,"ThoughtHeading","속마음",22,Muted,460,36,250,430,true);Place("ThoughtCloud",board,250,204,.90f);
        Text(board,"ObservationHeading","관찰 · 발견",22,Muted,460,36,250,-62,true);Place("ObservationCard",board,250,-226,.90f);
        Text(board,"CompletionHeading","함께한 기록",22,Muted,370,36,720,430,true);Place("CompletionCard",board,720,209,.73f);
        Text(board,"PauseHeading","잠깐 쉬기",22,Muted,370,36,720,-88,true);Place("PausePanel",board,720,-323,.73f);
        Place("IntercomDialogueCard",board,-641,-459,.48f);Place("MissionCard",board,-194,-464,.72f);
        Text(board,"MissionHeading","통화와 지금 할 일",18,Muted,780,30,-468,-359);
        Text(board,"Footer","모든 문구는 수정 가능한 Unity 텍스트입니다.  /  마우스·XR 포인터 선택 대응",17,Muted,1680,35,0,-604);
        var camera=new GameObject("PreviewCamera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(233,235,213,255);
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene,Root+"Scenes/StorybookUI_Showcase.unity");
        SceneManager.SetActiveScene(source);EditorSceneManager.CloseScene(scene,true);
    }
}
