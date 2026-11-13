#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectWait.M.Elevator;
using ProjectWait.M.Elevator.DialoguePreview;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace ProjectWait.M.EditorTools
{
    // Constructs temporary UI, not scenes. A live XR provider is neither started nor reconfigured.
    public static class VRUIComfortValidation
    {
        const string Output="Assets/M/CodexTemp/VRUIComfort_20261113~";
        static readonly List<Object> temporary=new List<Object>();
        static readonly List<string> report=new List<string>();
        static TMP_FontAsset font;
        static float worstGuideElevation,widestGuideFraction,widestStatusFraction,questGuideFraction,questStatusFraction;
        static int captionCases,guideCases,worldCases,statusCases,anchorCases;
        static readonly string[] Captions={
            ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotStory),
            ElevatorDialogue.Caption("친구",ElevatorDialogue.ShotGesture),
            ElevatorDialogue.Caption("친구",ElevatorDialogue.Stopped),
            ElevatorDialogue.Caption("관리실",ElevatorDialogue.Greeting(false)),
            ElevatorDialogue.Caption("119 · 모의 통화",ElevatorDialogue.Greeting(true)),
            ElevatorDialogue.Caption("나",ElevatorDialogue.SituationReport),
            ElevatorDialogue.Caption("나","엘리베이터가 고장 나서 움직이지 않아요. 친구랑 둘이 있어요."),
            ElevatorDialogue.Caption("관리실",ElevatorDialogue.LocationQuestion),
            ElevatorDialogue.Caption("119 · 모의 통화",ElevatorDialogue.IdentityGuide),
            ElevatorDialogue.Caption("나","한빛 생활체육센터예요. 엘리베이터 번호는 HB-EL-01이에요."),
            ElevatorDialogue.Caption("관리실",ElevatorDialogue.SafetyAdvice),
            ElevatorDialogue.Caption("친구",ElevatorDialogue.FriendQuestion),
            ElevatorDialogue.Caption("나",ElevatorDialogue.DoorWarning),
            ElevatorDialogue.Caption("친구",ElevatorDialogue.FriendMoving),
            ElevatorDialogue.Caption("친구",ElevatorDialogue.FriendSettled),
            ElevatorDialogue.Caption("소방 구조대원",ElevatorDialogue.RescueArrival),
            ElevatorDialogue.Caption("승강기 담당자",ElevatorDialogue.RescueChecking),
            ElevatorDialogue.Caption("소방 구조대원",ElevatorDialogue.RescueReady),
            ElevatorOpeningSequence.Caption(1),ElevatorOpeningSequence.Caption(6),
            "비상통화 연결 중…","친구가 뛰려고 해요. 안전 안내를 살펴보세요.",
            "나  ·  여기서는 뛰면 위험해. 슛은 밖에서 하자.","계단으로 1층에 내려가요.","잠시 후…"
        };

        [MenuItem("Tools/Wait/M/Validate VR UI Comfort")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run outside Play Mode.");
            Directory.CreateDirectory(Output);report.Clear();captionCases=guideCases=worldCases=statusCases=anchorCases=0;
            worstGuideElevation=widestGuideFraction=widestStatusFraction=questGuideFraction=questStatusFraction=0;
            var settingsField=typeof(TMP_Settings).GetField("s_Instance",BindingFlags.NonPublic|BindingFlags.Static);
            var originalSettings=TMP_Settings.instance;
            var isolatedSettings=originalSettings?Object.Instantiate(originalSettings):ScriptableObject.CreateInstance<TMP_Settings>();
            isolatedSettings.hideFlags=HideFlags.HideAndDontSave;settingsField.SetValue(null,isolatedSettings);
            try
            {
                if(!TMP_Settings.leadingCharacters)typeof(TMP_Settings).GetField("m_leadingCharacters",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(isolatedSettings,AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/LineBreaking Leading Characters.txt"));
                if(!TMP_Settings.followingCharacters)typeof(TMP_Settings).GetField("m_followingCharacters",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(isolatedSettings,AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/LineBreaking Following Characters.txt"));
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/M/Arts/Fonts/NotoSansCJKkr-Medium.otf");
                if(!source)source=AssetDatabase.LoadAssetAtPath<Font>("Assets/ValidationFont.otf");
                Need(source,"Noto CJK source font missing");
                font=TMP_FontAsset.CreateFontAsset(source,48,6,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="M VR comfort validation font (temporary)";font.hideFlags=HideFlags.HideAndDontSave;font.boldSpacing=7;
                TMP_Settings.defaultFontAsset=font;
                var all=string.Join("",Captions)+string.Concat(typeof(ElevatorDialogue).GetFields(BindingFlags.Public|BindingFlags.Static)
                    .Where(f=>f.FieldType==typeof(string)&&f.IsLiteral).Select(f=>(string)f.GetRawConstantValue()))+
                    "소방 구조대원승강기 담당자친구와 함께 기다리기엘리베이터 안내판1층 선택 · 공 받기내려가는 버튼가리킨 뒤검지 트리거를눌러 주세요순서는 자유예요레이로 가리키고눈높이이동메뉴잠시멈춤";
                Need(font.TryAddCharacters(all,out var missing),"Missing font glyphs: "+missing);
                Check("Letterbox geometry preserved / no legacy text across 125 inputs",CaptionLayouts);
                Check("Symmetrical 200-point bars / animation / no subtitle ownership",CaptionBandAnimation);
                Check("Actual VR tutorial reflow / angular position / width",GuideLayouts);
                Check("Actual status reflow without obsolete subtitle exclusion",StatusLayouts);
                Check("Source bubble text + choices / legacy caption cannot duplicate or move it",WorldLayouts);
                Check("Friend / intercom / phone / both rescuers: source following, not gaze following",AnchoredRegions);
                Check("Rescue world-card ownership, ready-to-exit and PC policy",RescueOwnership);
                Check("VR scale independent of monitor FOV; PC layout retained",Modes);
                Check("Floor/device spawn, tracked readiness and authored facing reset",SpawnPlacement);
                Check("Turned lobby PC arrival resets heading / preserves height / gates startup",ArrivalReset);
                Check("Stereo geometry cache invalidation",CachedLayouts);
                Check("Goal uses dialogue-only gaze and reversible one-second fade",GoalGazeFade);
                Check("Player departure shifts exactly 0.2s, including XR comfort fade",DepartureDelay);
                Check("Legacy region helper retained for older callers",RescueMeetingAnchors);
                Check("Cinematic speech visible in source bubbles / narration and timed expiry",SpeechOwnership);
                Check("Bubble tail points at its source without intercepting input",SpeechTailDirections);
                report.Add($"COUNTS: {captionCases} legacy-caption suppression, {guideCases} tutorial, {statusCases} status, {worldCases} dialogue/choice layouts; {anchorCases} actor/emitter anchor checks.");
                report.Add($"MEASURED: guide max text-centre elevation={worstGuideElevation:F2}deg; Quest guide/common width={questGuideFraction:P1}; Quest status/common width={questStatusFraction:P1}; worst tested guide/status width={widestGuideFraction:P1}/{widestStatusFraction:P1}.");
                report.Add("NOT TESTED: headset optics/distortion, user comfort, raster screenshots, or full story playback. Geometry uses recorded Quest frusta and actual runtime UI/TMP text layout.");
            }
            finally
            {
                Clean();
                settingsField.SetValue(null,originalSettings);Object.DestroyImmediate(isolatedSettings);
                if(font){foreach(var texture in font.atlasTextures)if(texture)Object.DestroyImmediate(texture);if(font.material)Object.DestroyImmediate(font.material);Object.DestroyImmediate(font);}
                File.WriteAllLines(Output+"/validation.txt",report);
            }
            Debug.Log("[M VR UI comfort]\n"+string.Join("\n",report));
            if(report.Any(line=>line.StartsWith("FAIL:")))throw new Exception("VR UI comfort validation failed. See "+Output+"/validation.txt");
        }

        static void Need(bool condition,string reason){if(!condition)throw new Exception(reason);}
        static void Check(string name,Action test)
        {
            try{test();report.Add("PASS: "+name);}
            catch(Exception ex){report.Add("FAIL: "+name+" -- "+ex);}
            finally{Clean();}
        }
        static void Clean(){for(int i=temporary.Count-1;i>=0;i--)if(temporary[i])Object.DestroyImmediate(temporary[i]);temporary.Clear();}
        static GameObject Go(string name,bool rect=false,Transform parent=null)
        {
            var go=rect?new GameObject(name,typeof(RectTransform)):new GameObject(name);
            go.hideFlags=HideFlags.HideAndDontSave;if(parent)go.transform.SetParent(parent,false);temporary.Add(go);return go;
        }
        static RectTransform Rect(string name,Transform parent=null)
        {
            var rect=(RectTransform)Go(name,true,parent).transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;return rect;
        }
        static TMP_Text Text(string name,Transform parent,float size)
        {
            var label=Rect(name,parent).gameObject.AddComponent<TextMeshProUGUI>();label.font=font;ElevatorTypography.Apply(label,size);return label;
        }
        static void Set(object value,string property,object data)=>value.GetType().GetProperty(property,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(value,data);
        static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
        static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,args);
        static float Elevation(Transform eyes,RectTransform panel)
        {
            var p=eyes.InverseTransformPoint(panel.TransformPoint(panel.rect.center));return Mathf.Atan2(p.y,p.z)*Mathf.Rad2Deg;
        }
        static void Fits(StereoUISafeArea.Frame frame,RectTransform panel,Transform eyes,bool centralReading=true)
        {
            Need(VRUIReadingLayout.Project(frame,panel,out _,true),panel.name+" clipped in an eye");
            if(centralReading)for(int i=0;i<4;i++)Need(VRUIReadingLayout.Comfortable(eyes,panel.TransformPoint(new Vector3((i&1)==0?panel.rect.xMin:panel.rect.xMax,(i&2)==0?panel.rect.yMin:panel.rect.yMax,0))),panel.name+" outside angular reading bounds");
        }
        static void TextFits(TMP_Text text)
        {
            text.ForceMeshUpdate(true,true);var preferred=text.GetPreferredValues(text.text,text.rectTransform.rect.width,0);
            Need(preferred.y<=text.rectTransform.rect.height+.5f,text.name+" text exceeds its rectangle");
            if(text.textWrappingMode==TextWrappingModes.NoWrap)Need(preferred.x<=text.rectTransform.rect.width+.5f,text.name+" name overflows its ribbon");
            Need(!text.enableAutoSizing,text.name+" was auto-shrunk");
        }
        static Rect EyeRect(StereoUISafeArea.Frame frame,RectTransform panel,int eye)
        {
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            for(int i=0;i<4;i++)
            {
                var corner=panel.TransformPoint(new Vector3((i&1)==0?panel.rect.xMin:panel.rect.xMax,(i&2)==0?panel.rect.yMin:panel.rect.yMax,0));
                Need(frame.Project(corner,eye,out var p),"Eye projection failed");min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            return UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        static StereoUISafeArea.Frame Frame(Camera camera,int variant)
        {
            var flip=Matrix4x4.Scale(new Vector3(1,1,-1));
            var frame=new StereoUISafeArea.Frame{
                leftView=flip*Matrix4x4.TRS(new Vector3(-.032f,0,0),Quaternion.Euler(0,variant==2?-5:0,0),Vector3.one).inverse*camera.transform.worldToLocalMatrix,
                rightView=flip*Matrix4x4.TRS(new Vector3(.032f,0,0),Quaternion.Euler(0,variant==2?5:0,0),Vector3.one).inverse*camera.transform.worldToLocalMatrix,
                leftProjection=Matrix4x4.Perspective(98.04016f,1,.05f,100),rightProjection=Matrix4x4.Perspective(98.04016f,1,.05f,100),near=.05f,far=100};
            frame.leftProjection.m00=frame.rightProjection.m00=variant==3?1.2f:.877215445f;
            frame.leftProjection.m11=frame.rightProjection.m11=variant==3?1.2f:.8686716f;
            frame.leftProjection.m02=variant==1?0:-.122784555f;frame.rightProjection.m02=variant==1?0:.122784555f;
            frame.leftProjection.m12=frame.rightProjection.m12=-.0352425165f;
            if(variant==4){frame.leftProjection.m12=-.08f;frame.rightProjection.m12=.02f;}
            return frame;
        }

        sealed class Fixture
        {
            public readonly Camera camera;
            public readonly ElevatorCinematicOverlay overlay;
            public readonly TMP_Text caption;
            public readonly ElevatorLesson lesson;
            public readonly ElevatorView view;
            public readonly ElevatorSpatialUI spatial;
            public Fixture()
            {
                camera=Go("comfort camera").AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.fieldOfView=98.04016f;camera.transform.position=Vector3.up*1.3f;
                lesson=Go("comfort lesson").AddComponent<ElevatorLesson>();lesson.settings=ScriptableObject.CreateInstance<ElevatorSettings>();temporary.Add(lesson.settings);
                Set(lesson,"Flow",new ElevatorFlow(lesson.settings));lesson.playerCamera=camera;lesson.friend=Go("friend").transform;lesson.friend.position=new Vector3(-.7f,0,1.8f);
                view=Rect("comfort view").gameObject.AddComponent<ElevatorView>();view.gameObject.AddComponent<Canvas>();view.playerCamera=camera;view.lesson=lesson;view.font=font;lesson.view=view;
                spatial=view.gameObject.AddComponent<ElevatorSpatialUI>();view.spatial=spatial;spatial.view=view;spatial.worldPixelSize=.002f;spatial.compactDesktopDialogue=false;
                spatial.hud=Rect("comfort HUD").gameObject.AddComponent<Canvas>();spatial.hud.gameObject.AddComponent<CanvasScaler>();
                spatial.mission=Rect("Mission",spatial.hud.transform);spatial.mission.sizeDelta=new Vector2(VRUIReadingLayout.StatusWidth,150);
                view.title=Text("Status heading",spatial.mission,20);view.subtitle=Text("Status body",spatial.mission,26);
                spatial.progress=Text("Progress",spatial.hud.transform,22);spatial.controls=Text("Controls",spatial.hud.transform,20);
                view.thoughtPanel=Rect("Thought",spatial.hud.transform);view.thought=Text("Thought body",view.thoughtPanel,27);spatial.thoughtHeading=Text("Thought heading",view.thoughtPanel,18);view.thoughtPanel.gameObject.SetActive(false);
                view.dialoguePanel=Rect("Dialogue",view.transform);view.dialoguePanel.gameObject.AddComponent<Image>();view.dialogueText=Text("Dialogue body",view.dialoguePanel,35);
                spatial.speakerTab=Rect("Speaker",view.dialoguePanel);spatial.speakerTab.gameObject.AddComponent<Image>();spatial.speaker=Text("Speaker text",spatial.speakerTab,28);spatial.speaker.text="친구";
                view.actionPanel=Rect("Actions",view.transform);view.actionPanel.pivot=new Vector2(.5f,1);
                view.developerPanel=Rect("Developer",view.transform);view.developerPanel.gameObject.SetActive(false);
                spatial.notebook=Go("notebook").transform;spatial.rightPage=Rect("Right page",spatial.notebook);spatial.leftPage=Rect("Left page",spatial.notebook);
                spatial.notebookCaption=Text("Book caption",spatial.notebook,27);spatial.notebookDetail=Text("Book detail",spatial.notebook,27);
                spatial.completionRows=Array.Empty<RectTransform>();spatial.completionMarks=Array.Empty<TMP_Text>();
                overlay=Rect("comfort overlay").gameObject.AddComponent<ElevatorCinematicOverlay>();overlay.playerCamera=camera;overlay.lesson=lesson;overlay.font=font;overlay.Build();Set(overlay,"Amount",1f);
                Need(overlay.GetComponentsInChildren<TMP_Text>(true).Length==0,"New letterbox still creates a text object");
                // Simulate an older serialized scene caption; production Build creates no text.
                caption=Text("CinematicSubtitle",overlay.transform.Find("BottomLetterbox"),28);
                typeof(ElevatorCinematicOverlay).GetField("caption",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(overlay,caption);
                lesson.presentation=Go("presentation").AddComponent<ElevatorStoryPresentation>();lesson.presentation.space=Go("space").AddComponent<ElevatorSpacePresentation>();lesson.presentation.space.overlay=overlay;
                var storybook=view.gameObject.AddComponent<ElevatorStorybookUI>();storybook.view=view;spatial.storybook=storybook;
                var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f);temporary.Add(sprite);
                storybook.dialogueFrame=storybook.speakerRibbon=storybook.thoughtRibbon=storybook.friendPortrait=storybook.speechTail=storybook.primaryButton=storybook.secondaryButton=sprite;
            }
            public void Caption(string text,StereoUISafeArea.Frame frame)
            {
                caption.text=text;caption.gameObject.SetActive(true);
                Need(overlay.LayoutVR(frame),"Letterbox layout failed");
                Need(!caption.gameObject.activeSelf&&!caption.enabled&&caption.text=="","Legacy letterbox text was revived");
            }
            public void Choice(string text)
            {
                var rect=Rect("Choice"+view.ChoiceButtons.Count,view.actionPanel);rect.gameObject.AddComponent<DwtD.TestScene.CorridorBubbleGraphic>();
                var button=rect.gameObject.AddComponent<Button>();var label=Text("Choice label",rect,34);label.text=text;view.ChoiceButtons.Add(button);spatial.StyleButton(button);
            }
        }

        static void CaptionLayouts()
        {
            for(int variant=0;variant<5;variant++)
            {
                var f=new Fixture();var frame=Frame(f.camera,variant);
                foreach(var text in Captions)
                {
                    f.Caption(text,frame);
                    Need(!f.overlay.HasVisibleSubtitle&&!f.overlay.TryGetSubtitleViewport(frame,out _)&&!f.overlay.TryGetSubtitleLocalRect(f.overlay.transform,out _),"Letterbox still owns a subtitle area");
                    Need(Mathf.Abs(f.camera.transform.InverseTransformPoint(f.overlay.transform.position).z-VRUIReadingLayout.Distance)<.0001f,"Subtitle plane depth changed");
                    int version=f.overlay.SubtitleLayoutVersion;f.overlay.LayoutVR(frame);Need(version==f.overlay.SubtitleLayoutVersion,"Unchanged subtitle invalidates world placement every frame");
                    captionCases++;
                }
                var bottom=f.overlay.transform.Find("BottomLetterbox") as RectTransform;VRUIReadingLayout.Project(frame,bottom,out var bar);
                // Vertically asymmetric eyes require a union band; the lower eye necessarily sees that offset too.
                Need(bar.xMin<0&&bar.xMax>1&&bar.yMin<0&&bar.yMax>.27f&&bar.yMax<.38f,"200-point bottom band coverage changed");
                frame.EyeRect(f.overlay.transform,0,out var left);frame.EyeRect(f.overlay.transform,1,out var right);
                frame.Plane(f.overlay.transform,out _,out var coverage);
                frame.EyeVerticalRange(f.overlay.transform,0,out var lv);frame.EyeVerticalRange(f.overlay.transform,1,out var rv);
                float expected=Mathf.Max(lv.x,rv.x)-(coverage.yMin-coverage.height*.02f)+Mathf.Min(lv.y-lv.x,rv.y-rv.x)*200/720;
                expected=Mathf.Max(expected,coverage.yMax+coverage.height*.02f-Mathf.Min(lv.y,rv.y)+Mathf.Min(lv.y-lv.x,rv.y-rv.x)*200/720);
                Need(VRUIReadingLayout.LetterboxHeight==200&&Mathf.Abs(bottom.rect.height-expected)<.05f,"VR band is not 200 reference points");
                var top=f.overlay.transform.Find("TopLetterbox") as RectTransform;
                Need(top.gameObject.activeSelf&&Mathf.Abs(top.rect.height-bottom.rect.height)<.05f,"VR top and bottom bars differ");Clean();
            }
        }
        static void GuideLayouts()
        {
            for(int variant=0;variant<5;variant++)
            {
                var f=new Fixture();var frame=Frame(f.camera,variant);var guide=Go("tutorial").AddComponent<ElevatorTutorialPreview>();guide.lesson=f.lesson;guide.font=font;
                Call(guide,"BuildViews");
                foreach(ElevatorTutorialPreview.GuideStep step in Enum.GetValues(typeof(ElevatorTutorialPreview.GuideStep)))
                {
                    if(step==ElevatorTutorialPreview.GuideStep.None)continue;Set(guide,"Step",step);
                    foreach(bool expanded in new[]{false,true})
                    {
                        guide.SetContent(expanded,true);Need(guide.LayoutVR(frame),"Guide did not fit");Fits(frame,guide.InstructionPanel,f.camera.transform);
                        foreach(var text in guide.InstructionPanel.GetComponentsInChildren<TMP_Text>(true))
                        {TextFits(text);float angle=Elevation(f.camera.transform,text.rectTransform);worstGuideElevation=Mathf.Max(worstGuideElevation,angle);Need(Mathf.Abs(angle)<23,"Guide text too far from straight-ahead");}
                        var plane=guide.InstructionCanvas.transform;frame.Plane(plane,out var common,out _);
                        widestGuideFraction=Mathf.Max(widestGuideFraction,VRUIReadingLayout.LocalRect(guide.InstructionPanel,plane).width/common.width);
                        if(variant==0)questGuideFraction=Mathf.Max(questGuideFraction,VRUIReadingLayout.LocalRect(guide.InstructionPanel,plane).width/common.width);
                        Need(VRUIReadingLayout.LocalRect(guide.InstructionPanel,plane).width/common.width<.5f,"Guide still occupies most binocular width");guideCases++;
                    }
                }
                Clean();
            }
        }
        static void CaptionBandAnimation()
        {
            for(int variant=0;variant<5;variant++)
            {
                var f=new Fixture();var frame=Frame(f.camera,variant);f.caption.text=Captions[16];
                foreach(float amount in new[]{0f,.1f,.3f,.5f,.8f,1f})
                {
                    Set(f.overlay,"Amount",amount);bool fits=f.overlay.LayoutVR(frame);
                    var top=f.overlay.transform.Find("TopLetterbox") as RectTransform;
                    var bottom=f.overlay.transform.Find("BottomLetterbox") as RectTransform;
                    Need(top.gameObject.activeSelf==(amount>0)&&bottom.gameObject.activeSelf==(amount>0),"Bar activation is asymmetric");
                    Need(Mathf.Abs(top.rect.height-bottom.rect.height)<.05f,"Animation bar heights differ");
                    Need(fits&&!f.caption.enabled&&!f.caption.gameObject.activeSelf&&!f.overlay.HasVisibleSubtitle,"Animation revived subtitle text");
                }
                Clean();
            }
        }
        static void StatusLayouts()
        {
            var f=new Fixture();var frame=Frame(f.camera,0);viewHidden(f);
            foreach(var text in Captions)
            {
                f.Caption(text,frame);Set(f.lesson.Flow,"Step",ElevatorStep.DoorPosterPointing);Status(f,frame,0);
            }
            for(int variant=0;variant<5;variant++)foreach(var step in new[]{ElevatorStep.Stopped,ElevatorStep.Unanswered,ElevatorStep.Call,ElevatorStep.DoorLean,ElevatorStep.DoorPosterPointing,ElevatorStep.SafeWallMove,ElevatorStep.SafeWallWaiting,ElevatorStep.Rescue})
            {
                frame=Frame(f.camera,variant);f.Caption(Captions[16],frame);Set(f.lesson.Flow,"Step",step);Set(f.lesson.Flow,"Phase",InteractionPhase.Investigation);
                Set(f.lesson.Flow,"IdentityReviewOpen",step==ElevatorStep.Call);Set(f.lesson.Flow,"IdentityFound",step==ElevatorStep.Call);Set(f.lesson.Flow,"ReadyToExit",true);
                Status(f,frame,variant);
            }
            Need(widestStatusFraction<.4f,"Status still occupies most binocular width");
        }
        static void Status(Fixture f,StereoUISafeArea.Frame frame,int variant)
        {
            f.spatial.mission.gameObject.SetActive(false);f.spatial.LayoutVRHud(frame);f.spatial.LayoutHud(true);Need(f.spatial.LayoutVRHud(frame),"Status did not fit above subtitle");
            Fits(frame,f.spatial.mission,f.camera.transform);TextFits(f.view.title);TextFits(f.view.subtitle);
            Need(!f.overlay.TryGetSubtitleLocalRect(f.spatial.hud.transform,out _),"Removed subtitle is reserving HUD space");
            frame.Plane(f.spatial.hud.transform,out var common,out _);float width=VRUIReadingLayout.LocalRect(f.spatial.mission,f.spatial.hud.transform).width/common.width;
            widestStatusFraction=Mathf.Max(widestStatusFraction,width);if(variant==0)questStatusFraction=Mathf.Max(questStatusFraction,width);statusCases++;
        }
        static void viewHidden(Fixture f){f.view.dialoguePanel.gameObject.SetActive(false);f.view.thoughtPanel.gameObject.SetActive(false);}
        static void WorldLayouts()
        {
            for(int variant=0;variant<5;variant++)
            {
                var f=new Fixture();var frame=Frame(f.camera,variant);
                Set(f.lesson.Flow,"Step",ElevatorStep.DoorPosterPointing);
                f.Choice(ElevatorDialogue.SafeReply);f.Choice(ElevatorDialogue.UnsafeReply);f.view.dialogueText.text=ElevatorDialogue.FriendQuestion;
                Vector3 firstPosition=default;Quaternion firstRotation=default;
                foreach(bool subtitles in new[]{false,true})
                {
                    f.spatial.speaker.text="친구";
                    f.caption.gameObject.SetActive(subtitles);f.caption.text=ElevatorDialogue.Caption("승강기 담당자",ElevatorDialogue.RescueChecking);f.overlay.LayoutVR(frame);
                    f.spatial.LayoutVRHud(frame);f.spatial.Layout(true);Call(f.spatial,"PlaceStereoDialogue",frame,ElevatorStep.DoorPosterPointing);
                    var position=Field<Vector3>(f.spatial,"dialoguePosition");var rotation=Field<Quaternion>(f.spatial,"dialogueRotation");var scale=Field<Vector3>(f.spatial,"dialogueScale");
                    f.view.transform.SetPositionAndRotation(position,rotation);f.view.transform.localScale=scale;
                    if(!subtitles){firstPosition=position;firstRotation=rotation;}
                    else Need(position==firstPosition&&rotation==firstRotation,"Subtitle moved the authored world anchor");
                    if(!f.spatial.StereoPlacementFits)
                    {
                        Vector2 low=Vector2.one*float.MaxValue,high=Vector2.one*float.MinValue;
                        foreach(var corner in Field<List<Vector3>>(f.spatial,"readingGeometryCorners"))
                        {var p=f.camera.transform.InverseTransformPoint(f.view.transform.TransformPoint(corner));var angle=new Vector2(Mathf.Atan2(p.x,p.z),Mathf.Atan2(p.y,p.z))*Mathf.Rad2Deg;low=Vector2.Min(low,angle);high=Vector2.Max(high,angle);}
                        report.Add($"WORLD DIAGNOSTIC: eye={variant}, position={position}, bounds={Field<Bounds>(f.spatial,"readingBounds")}, angular extent={low}..{high}");
                    }
                    Need(f.spatial.StereoPlacementFits,"Dialogue/choices failed recorded-frustum placement");
                    foreach(var graphic in f.view.dialoguePanel.GetComponentsInChildren<Graphic>(false))Fits(frame,graphic.rectTransform,f.camera.transform);
                    TextFits(f.spatial.speaker);TextFits(f.view.dialogueText);
                    foreach(var button in f.view.ChoiceButtons){Fits(frame,(RectTransform)button.transform,f.camera.transform);TextFits(button.GetComponentInChildren<TMP_Text>());}
                    var stamp=new StereoUISafeArea.LayoutStamp();stamp.Update(f.camera,frame);f.camera.transform.rotation=Quaternion.Euler(12,25,6);var moved=Frame(f.camera,variant);
                    Need(!stamp.Update(f.camera,moved),"Head turn triggered reanchor");Need(f.view.transform.position==position&&f.view.transform.rotation==rotation,"Head turn moved world card");
                    Call(f.spatial,"PlaceStereoDialogue",moved,ElevatorStep.DoorPosterPointing);
                    Need(Field<Vector3>(f.spatial,"dialoguePosition")==position&&Field<Quaternion>(f.spatial,"dialogueRotation")==rotation,"Head turn moved the source bubble");
                    f.camera.transform.rotation=Quaternion.identity;worldCases++;
                }
                Clean();
            }
        }
        static void RescueOwnership()
        {
            var f=new Fixture();var frame=Frame(f.camera,0);f.spatial.LayoutVRHud(frame);Set(f.lesson.Flow,"Step",ElevatorStep.Rescue);Set(f.lesson.Flow,"ReadyToExit",false);f.view.dialogueText.text=ElevatorDialogue.RescueChecking;
            f.spatial.SetCinematicVisibility(true);Need(f.view.GetComponent<CanvasGroup>().alpha==1,"VR rescue world dialogue hidden");
            Need(!ElevatorCinematicOverlay.UsesRescueSubtitle(true,ElevatorStep.Rescue,false),"VR rescue subtitle duplicates the world card");
            f.spatial.SetCinematicVisibility(false);Need(f.view.GetComponent<CanvasGroup>().alpha==1,"PC rescue policy changed");
            Set(f.lesson.Flow,"ReadyToExit",true);f.spatial.SetCinematicVisibility(true);Need(f.view.GetComponent<CanvasGroup>().alpha==1,"Ready-to-exit dialogue did not return");
            Need(!ElevatorCinematicOverlay.UsesRescueSubtitle(true,ElevatorStep.Stopped,false),"Non-rescue dialogue ownership changed");
        }
        static void AnchoredRegions()
        {
            for(int variant=0;variant<5;variant++)for(int role=0;role<5;role++)
            {
                var f=new Fixture();var map=Go("authored map").AddComponent<ElevatorSportsCentreMap>();
                map.space=f.lesson.presentation.space;map.space.sportsCentre=map;
                var team=Go("speaker team").AddComponent<ElevatorRescueTeam>();f.lesson.presentation.rescueTeam=team;
                team.firefighter=Go("firefighter").transform;team.firefighter.position=new Vector3(-.6f,0,2.8f);Go("head",false,team.firefighter).transform.localPosition=Vector3.up*1.5f;
                team.specialist=Go("specialist").transform;team.specialist.position=new Vector3(.6f,0,2.8f);Go("head",false,team.specialist).transform.localPosition=Vector3.up*1.5f;
                var phone=Go("phone",false,f.lesson.transform).AddComponent<ElevatorObservation>();phone.clue=ElevatorClue.Phone;phone.transform.position=new Vector3(-.6f,1.1f,2.2f);
                map.sideIntercom=Go("intercom").transform;map.sideIntercom.position=new Vector3(-.6f,1.1f,2.2f);
                Set(f.lesson.Flow,"Step",role>=3?ElevatorStep.Rescue:role>=1?ElevatorStep.CallQuestion:ElevatorStep.Lobby);Set(f.lesson.Flow,"UsedPhone",role==2);
                f.lesson.friend.position=new Vector3(-.7f,0,2.4f);
                f.spatial.speaker.text=new[]{"친구","관리실","119 · 모의 통화","소방 구조대원","승강기 담당자"}[role];
                Transform target=role==0?f.lesson.friend:role==1?map.sideIntercom:role==2?phone.transform:role==3?team.firefighter:team.specialist;
                var frame=Frame(f.camera,variant);f.view.dialogueText.text=ElevatorDialogue.FriendQuestion;f.Choice(ElevatorDialogue.SafeReply);
                f.spatial.LayoutVRHud(frame);f.spatial.Layout(true);Call(f.spatial,"PlaceStereoDialogue",frame,f.lesson.Flow.Step);
                var position=Field<Vector3>(f.spatial,"dialoguePosition");var rotation=Field<Quaternion>(f.spatial,"dialogueRotation");
                f.view.transform.SetPositionAndRotation(position,rotation);f.view.transform.localScale=Field<Vector3>(f.spatial,"dialogueScale");
                Need(f.spatial.SpeechSource==target,"Wrong speaker/emitter: "+f.spatial.speaker.text);
                Need(f.spatial.StereoPlacementFits,"Source bubble clipped: role="+role+" variant="+variant+" position="+position);
                Need(Vector3.Dot(rotation*Vector3.forward,(position-f.camera.transform.position).normalized)>.98f,"Anchor does not face the player");
                f.view.thought.text=ElevatorDialogue.FriendQuestion;f.view.thoughtPanel.gameObject.SetActive(true);f.spatial.LayoutHud(true);
                Need(f.view.thoughtPanel.parent==f.view.transform,"Object description still belongs to the head-following HUD");
                var thoughtPosition=f.view.thoughtPanel.position;var thoughtRotation=f.view.thoughtPanel.rotation;
                f.camera.transform.rotation=Quaternion.Euler(15,85,8);
                var moved=Frame(f.camera,variant);Call(f.spatial,"PlaceStereoDialogue",moved,f.lesson.Flow.Step);
                Need(Field<Vector3>(f.spatial,"dialoguePosition")==position&&Field<Quaternion>(f.spatial,"dialogueRotation")==rotation,"Bubble followed head rotation");
                f.spatial.LayoutVRHud(moved);
                Need(Vector3.Distance(thoughtPosition,f.view.thoughtPanel.position)<.0001f&&Quaternion.Angle(thoughtRotation,f.view.thoughtPanel.rotation)<.01f,"Object description followed the moving HUD");
                var delta=new Vector3(.2f,0,.1f);target.position+=delta;Call(f.spatial,"PlaceStereoDialogue",moved,f.lesson.Flow.Step);
                Need(Vector3.Distance(Field<Vector3>(f.spatial,"dialoguePosition"),position+delta)<.0001f,"Bubble did not follow speaker translation");
                f.spatial.SetCinematicVisibility(true);Need(f.view.GetComponent<CanvasGroup>().alpha==1,"Looking away hid the source bubble");
                anchorCases++;Clean();
            }
        }
        static void Modes()
        {
            var f=new Fixture();var frame=Frame(f.camera,0);f.Caption(Captions[16],frame);var scale=f.overlay.transform.lossyScale;var size=f.caption.rectTransform.sizeDelta;var position=f.caption.transform.position;
            foreach(float fov in new[]{55f,80f,110f}){f.camera.fieldOfView=fov;f.overlay.LayoutVR(frame);Need(f.overlay.transform.lossyScale==scale&&f.caption.rectTransform.sizeDelta==size&&Vector3.Distance(position,f.caption.transform.position)<.0001f,"Monitor FOV changes VR typography/layout");}
            f.camera.fieldOfView=60;f.camera.aspect=16f/9;Call(f.overlay,"Position");
            var top=f.overlay.transform.Find("TopLetterbox") as RectTransform;var bottom=f.overlay.transform.Find("BottomLetterbox") as RectTransform;
            Need(top.gameObject.activeSelf&&top.rect.height==82&&bottom.rect.height==82&&Mathf.Abs(top.rect.width-1300)<.01f,"PC bars changed");
            Need(!f.caption.enabled&&!f.caption.gameObject.activeSelf&&f.caption.text=="","PC legacy subtitle reappeared");
            var guide=Go("mode guide").AddComponent<ElevatorTutorialPreview>();guide.lesson=f.lesson;guide.font=font;Call(guide,"BuildViews");Set(guide,"Step",ElevatorTutorialPreview.GuideStep.BoardingActions);
            guide.SetContent(true,true);guide.LayoutVR(frame);guide.SetContent(true,false);
            Need(guide.InstructionPanel.sizeDelta==new Vector2(400,158),"PC guide dimensions changed");
        }

        static void SpawnPlacement()
        {
            foreach(bool floor in new[]{true,false})foreach(float yaw in new[]{-135f,0f,77f,180f})foreach(float height in new[]{.85f,1.3f,1.8f})
            {
                var origin=Go("arrival origin").transform;origin.SetPositionAndRotation(new Vector3(-1.8f,7.92f,-1.8f),Quaternion.Euler(0,yaw,0));
                var eye=Go("tracked eye",false,origin).transform;eye.localPosition=new Vector3(.37f,floor?height:.03f,-.22f);eye.localRotation=Quaternion.Euler(12,43,7);
                var localPosition=eye.localPosition;var localRotation=eye.localRotation;var original=origin.position;
                var destination=new Vector3(0,7.92f,-13);
                Need(!ElevatorSpawnPlacement.TryApply(origin,eye,destination,0,false,true,floor,height,out _),"Untracked pose accepted");
                Need(!ElevatorSpawnPlacement.TryApply(origin,eye,destination,0,true,false,floor,height,out _),"Uninitialized tracking origin accepted");
                Need(origin.position==original,"Waiting moved the origin");
                Need(ElevatorSpawnPlacement.TryApply(origin,eye,destination,0,true,true,floor,height,out var actual),"Ready spawn rejected");
                Need(Vector3.Distance(eye.position,destination+Vector3.up*height)<.0001f,"Spawn eye/feet footprint or height changed");
                Need(Mathf.Abs(Mathf.DeltaAngle(eye.eulerAngles.y,0))<.01f,"Lobby yaw survived arrival");
                Need(eye.localPosition==localPosition&&Quaternion.Angle(eye.localRotation,localRotation)<.01f,"Tracked local pose was overwritten");
                Need(Mathf.Abs(actual-height)<.0001f,"Floor/device eye height wrong");
                if(floor)Need(Mathf.Abs(origin.position.y-destination.y)<.0001f,"Floor origin is not on the floor");
                var movement=origin.gameObject.AddComponent<ElevatorPlayerMovement>();movement.playerCamera=eye.gameObject.AddComponent<Camera>();movement.CalibrateFeet(destination.y);
                var capsule=origin.GetComponent<CharacterController>();
                Set(movement,"Controller",capsule);movement.CalibrateFeet(destination.y);
                Need(Mathf.Abs(origin.TransformPoint(capsule.center).y-capsule.height*.5f-destination.y)<.0001f,"Capsule feet are not on the floor");
                // A later physical head turn remains live; no per-frame gaze pinning.
                eye.localRotation*=Quaternion.Euler(0,20,0);Need(Mathf.Abs(Mathf.DeltaAngle(eye.eulerAngles.y,0))>10,"Head movement is pinned");
                Clean();
            }
        }

        static void CachedLayouts()
        {
            var f=new Fixture();var frame=Frame(f.camera,0);f.Caption(Captions[0],frame);
            for(int i=0;i<60;i++)f.overlay.LayoutVR(frame);
            Need(!f.overlay.HasVisibleSubtitle,"Repeated layout revived subtitles");
            var cache=new StereoUISafeArea.PlaneCache();
            for(int variant=0;variant<5;variant++)foreach(float scale in new[]{1f,.9f,1.1f})
            {
                f.camera.transform.SetPositionAndRotation(new Vector3(3,1.5f,-4),Quaternion.Euler(8,73,4));
                VRUIReadingLayout.SetPlane(f.overlay.transform,f.camera.transform);f.overlay.transform.localScale*=scale;frame=Frame(f.camera,variant);
                Need(cache.Get(frame,f.overlay.transform,out var safe,out var union,out _,out _),"Cache rejected valid geometry");
                Need(frame.Plane(f.overlay.transform,out var expected,out var coverage),"Uncached geometry failed");
                Need(Vector2.Distance(safe.min,expected.min)<.01f&&Vector2.Distance(safe.max,expected.max)<.01f&&Vector2.Distance(union.size,coverage.size)<.01f,"Cached geometry differs after pose/projection/scale change");
            }
            VRUIReadingLayout.SetPlane(f.overlay.transform,f.camera.transform);frame=Frame(f.camera,0);
            frame.leftView.m03+=.02f;frame.rightView.m03-=.02f;
            bool cached=cache.Get(frame,f.overlay.transform,out var ipdSafe,out _,out _,out _);
            bool native=frame.Plane(f.overlay.transform,out var ipdExpected,out _);
            Need(cached&&native,"Changed IPD rejected");
            Need(Vector2.Distance(ipdSafe.size,ipdExpected.size)<.01f,"IPD retained stale plane geometry");
            frame.near=2;
            Need(!cache.Get(frame,f.overlay.transform,out _,out _,out _,out _),"Changed clipping retained stale plane geometry");
            Need(!cache.Get(default,f.overlay.transform,out _,out _,out _,out _),"Invalid projection retained stale geometry");
        }

        static void ArrivalReset()
        {
            var f=new Fixture();var origin=Go("PC arrival origin").transform;
            f.view.spatial=null; // This test isolates pose ownership, not UI-reveal construction.
            f.lesson.presentation.space.lesson=f.lesson;
            var map=Go("arrival map").AddComponent<ElevatorSportsCentreMap>();map.space=f.lesson.presentation.space;map.space.sportsCentre=map;
            f.camera.transform.SetParent(origin,false);f.camera.transform.localPosition=new Vector3(.3f,-.255f,-.2f);
            var controls=Go("arrival controls").AddComponent<ElevatorDesktopControls>();controls.lesson=f.lesson;controls.xrOrigin=origin;controls.targetCamera=f.camera;
            var movement=origin.gameObject.AddComponent<ElevatorPlayerMovement>();movement.playerCamera=f.camera;controls.movement=movement;
            Set(movement,"Controller",origin.GetComponent<CharacterController>());
            typeof(ElevatorLesson).GetField("controls",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(f.lesson,controls);
            var source=Go("turned lobby gaze").transform;source.SetPositionAndRotation(Vector3.up*1.57f,Quaternion.Euler(18,123,7));
            var pose=new ProjectWait.M.SceneTransition.SceneTransitionHeadPose(source,0,false);
            try
            {
                foreach(var step in new[]{ElevatorStep.StartupFade,ElevatorStep.GymDeparture,ElevatorStep.Lobby})
                {
                    Set(f.lesson.Flow,"Step",step);
                    var destination=new Vector3(0,7.92f,-13);origin.position=destination;movement.CalibrateFeet(destination.y);
                    Need(controls.TryApplyTransitionHeadPose(pose,destination),"PC arrival rejected");
                    Need(Vector3.Distance(f.camera.transform.position,destination+Vector3.up*1.57f)<.001f,"PC source eye height was not retained");
                    Need(Quaternion.Angle(f.camera.transform.rotation,Quaternion.identity)<.01f&&controls.LookAngles==Vector2.zero,"PC inherited source yaw/pitch instead of authored forward");
                    Need(Mathf.Abs(movement.FeetPosition.y-7.92f)<.001f,"PC feet moved off destination floor");
                }
                typeof(ElevatorDesktopControls).GetField("spawnPending",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controls,true);
                float elapsed=f.lesson.Flow.Elapsed;f.lesson.Advance(.5f);
                Need(f.lesson.Flow.Elapsed==elapsed&&f.lesson.CinematicBlocking&&!f.lesson.SpawnReady,"Unready startup advanced the lesson or allowed interaction");
                controls.enabled=false;Need(f.lesson.SpawnReady,"Disabled controls stalled the deferred preview pose owner");
            }
            finally{Call(controls,"OnDisable");}
        }

        static void GoalGazeFade()
        {
            var f=new Fixture();Set(f.lesson.Flow,"Step",ElevatorStep.DoorPosterPointing);
            f.view.dialogueText.text=ElevatorDialogue.FriendQuestion;
            f.view.transform.SetPositionAndRotation(f.camera.transform.position+Vector3.forward*2,Quaternion.identity);
            f.view.transform.localScale=Vector3.one*.002f;
            f.view.dialoguePanel.sizeDelta=new Vector2(600,220);f.view.dialoguePanel.anchoredPosition=Vector2.zero;
            f.spatial.speakerTab.anchoredPosition=new Vector2(0,190);
            f.Choice("계속");f.view.ChoiceButtons[0].transform.position=f.view.dialoguePanel.position+Vector3.down*.5f;
            Need((bool)Call(f.spatial,"LookingAtDialogue"),"Looking directly at the dialogue did not hide the goal");
            f.camera.transform.rotation=Quaternion.Euler(0,18,0);
            Need(VRUIReadingLayout.Project(Frame(f.camera,0),f.view.dialoguePanel,out _,true),"Side-visible dialogue fixture is not in both eyes");
            Need(!(bool)Call(f.spatial,"LookingAtDialogue"),"A merely visible dialogue hid the goal");
            f.spatial.LayoutHud(false);var group=f.spatial.mission.GetComponent<CanvasGroup>();
            Need(group&&group.alpha==0&&f.spatial.mission.gameObject.activeSelf,"New goal did not start transparent");
            f.spatial.AdvanceHudVisibility(.5f);Need(Mathf.Abs(group.alpha-.5f)<.001f,"Goal fade-in did not take one second");
            f.spatial.AdvanceHudVisibility(.5f);Need(group.alpha==1,"Goal did not finish appearing after one second");
            f.camera.transform.rotation=Quaternion.identity;f.spatial.LayoutHud(false);
            Need(group.alpha==1&&f.spatial.mission.gameObject.activeSelf,"Goal disappeared abruptly");
            f.spatial.AdvanceHudVisibility(.5f);Need(Mathf.Abs(group.alpha-.5f)<.001f,"Goal fade-out did not take one second");
            f.spatial.AdvanceHudVisibility(.49f);Need(f.spatial.mission.gameObject.activeSelf,"Goal was disabled before its fade completed");
            f.spatial.AdvanceHudVisibility(.01f);Need(group.alpha<.0001f&&!f.spatial.mission.gameObject.activeSelf,"Goal did not finish disappearing");
            f.camera.transform.LookAt(f.spatial.speakerTab.position);
            Need(!(bool)Call(f.spatial,"LookingAtDialogue"),"Speaker ribbon alone hid the goal");
            f.camera.transform.LookAt(f.view.ChoiceButtons[0].transform.position);
            Need(!(bool)Call(f.spatial,"LookingAtDialogue"),"Choice alone hid the goal");
            f.camera.transform.rotation=Quaternion.Euler(0,18,0);f.spatial.LayoutHud(false);f.spatial.AdvanceHudVisibility(1);
            f.camera.transform.rotation=Quaternion.identity;f.spatial.LayoutHud(false);f.spatial.AdvanceHudVisibility(.3f);
            float partial=group.alpha;f.camera.transform.rotation=Quaternion.Euler(0,18,0);f.spatial.LayoutHud(false);
            Need(Mathf.Abs(group.alpha-partial)<.0001f,"Reversing gaze jumped the opacity");
            f.spatial.AdvanceHudVisibility(1);Need(group.alpha==1,"Reversed fade did not return to full visibility");
            f.camera.transform.rotation=Quaternion.identity;f.view.dialoguePanel.gameObject.SetActive(false);
            Need(!(bool)Call(f.spatial,"LookingAtDialogue"),"Inactive dialogue hid the goal");
            f.view.dialoguePanel.gameObject.SetActive(true);var dialogueGroup=f.view.dialoguePanel.gameObject.AddComponent<CanvasGroup>();dialogueGroup.alpha=0;
            Need(!(bool)Call(f.spatial,"LookingAtDialogue"),"Transparent dialogue hid the goal");
            Set(f.lesson.Flow,"Step",ElevatorStep.Rescue);Set(f.lesson.Flow,"ReadyToExit",false);
            f.spatial.LayoutHud(false);f.spatial.AdvanceHudVisibility(1);
            Need(group.ignoreParentGroups&&group.alpha==1&&Field<bool>(f.spatial,"statusWanted"),"Cinematic HUD fade hid an available goal without dialogue gaze");
        }

        static void DepartureDelay()
        {
            var start=new Vector3(0,7.92f,-13);var threshold=new Vector3(0,7.92f,-4.2f);var end=new Vector3(-1.8f,7.92f,-1.8f);
            Need(Mathf.Abs(ElevatorOpeningSequence.PlayerWalkBegins-.6f)<.0001f&&Mathf.Abs(ElevatorOpeningSequence.TeleportAt-4.4f)<.0001f,"Departure delay is not 0.2 seconds");
            for(int i=0;i<=80;i++)
            {
                float t=i*.1f;
                var current=ElevatorOpeningSequence.WalkPath(start,threshold,end,t,ElevatorOpeningSequence.PlayerWalkBegins,ElevatorOpeningSequence.PlayerWalkFinishes);
                var delayed=ElevatorOpeningSequence.WalkPath(start,threshold,end,t-.2f,.4f,7.1f);
                Need(Vector3.Distance(current,delayed)<.0001f,"Player walk was sped up instead of delayed");
                float yaw=ElevatorOpeningSequence.ViewYaw(start,threshold,end,t,ElevatorOpeningSequence.PlayerWalkBegins,ElevatorOpeningSequence.PlayerWalkFinishes,false);
                float oldYaw=ElevatorOpeningSequence.ViewYaw(start,threshold,end,t-.2f,.4f,7.1f,false);
                Need(Mathf.Abs(Mathf.DeltaAngle(yaw,oldYaw))<.001f,"PC view turn is out of sync with delayed walking");
            }
            Need(ElevatorOpeningSequence.WalkPath(start,threshold,end,.5f,ElevatorOpeningSequence.PlayerWalkBegins,ElevatorOpeningSequence.PlayerWalkFinishes)==start,"Player moved before delayed departure");
            Need(ElevatorOpeningSequence.WalkPath(start,threshold,end,.5f,0,6.7f)!=start,"Friend departure was delayed too");
            Need(ElevatorOpeningSequence.ComfortFadeAlpha(4.4f,true)==1&&ElevatorOpeningSequence.ComfortFadeAlpha(4.4f,false)==0,"XR teleport fade is not aligned with delayed teleport");
        }

        static void RescueMeetingAnchors()
        {
            var f=new Fixture();var map=Go("rescue map").AddComponent<ElevatorSportsCentreMap>();map.space=f.lesson.presentation.space;map.space.sportsCentre=map;
            var team=Go("rescue positions").AddComponent<ElevatorRescueTeam>();f.lesson.presentation.rescueTeam=team;
            var anchors=new VRDialogueWorldAnchors();
            foreach(var step in new[]{ElevatorStep.Rescue,ElevatorStep.ReturnLobby,ElevatorStep.GroundFloor})
            {
                Set(f.lesson.Flow,"Step",step);var feet=step==ElevatorStep.Rescue?map.inside:step==ElevatorStep.ReturnLobby?map.lobby:map.groundLobby;
                f.camera.transform.SetPositionAndRotation(feet+Vector3.up*1.3f,Quaternion.Euler(0,180,0));
                var pose=anchors.Get(f.lesson,step);
                Need(Mathf.Abs(pose.position.y-feet.y-1.8f)<.001f,"Rescue dialogue was not raised by 0.22m");
                Need(f.camera.transform.InverseTransformPoint(pose.position).z>1,"Exterior rescue dialogue is behind the player");
                if(step!=ElevatorStep.Rescue)
                {
                    var meeting=team.ExteriorDialogueFootprint(step==ElevatorStep.GroundFloor);
                    Need(Mathf.Abs(pose.position.x-meeting.x)<.001f&&Mathf.Abs(pose.position.z-meeting.z)<.001f,"Exterior dialogue is not above the friend between rescuers");
                }
                f.camera.transform.rotation=Quaternion.Euler(12,40,3);Need(anchors.Get(f.lesson,step).position==pose.position,"Rescue card follows head turns");
            }
        }

        static void SpeechOwnership()
        {
            var f=new Fixture();var frame=Frame(f.camera,0);
            foreach(var step in new[]{ElevatorStep.GymDeparture,ElevatorStep.Proposal,ElevatorStep.Jump,ElevatorStep.Stopped,
                ElevatorStep.CallQuestion,ElevatorStep.CallResponse,ElevatorStep.CallLiftQuestion,ElevatorStep.CallLocationResponse,
                ElevatorStep.CallSafetyAdvice,ElevatorStep.DoorLean,ElevatorStep.DoorPosterWarning,ElevatorStep.SafeWallMove,
                ElevatorStep.SafeWallWaiting,ElevatorStep.Rescue,ElevatorStep.ReturnLobby,ElevatorStep.GroundFloor})
            {
                Set(f.lesson.Flow,"Step",step);Set(f.lesson.Flow,"Elapsed",0f);Set(f.lesson.Flow,"Phase",InteractionPhase.Dialogue);
                string who=step==ElevatorStep.CallResponse||step==ElevatorStep.CallLocationResponse||step==ElevatorStep.DoorPosterWarning?"나":
                    step==ElevatorStep.CallQuestion||step==ElevatorStep.CallLiftQuestion||step==ElevatorStep.CallSafetyAdvice?"관리실":
                    step==ElevatorStep.Rescue||step==ElevatorStep.ReturnLobby?"소방 구조대원":"친구";
                f.view.dialogueText.text=ElevatorDialogue.FriendQuestion;f.spatial.Present(who,ElevatorDialogue.FriendQuestion,"");
                f.spatial.LayoutVRHud(frame);f.spatial.SetCinematicVisibility(true);
                Need(f.view.dialoguePanel.gameObject.activeSelf&&f.view.GetComponent<CanvasGroup>().alpha==1,"Cinematic speech was hidden: "+step);
                Need(f.view.dialogueText.text==ElevatorDialogue.FriendQuestion,"Spoken content was dropped: "+step);
                if(f.lesson.Flow.IsCinematic)Need(!f.view.GetComponent<CanvasGroup>().interactable,"Cinematic speech unlocked actions");
            }
            Set(f.lesson.Flow,"Step",ElevatorStep.GymDeparture);Set(f.lesson.Flow,"Elapsed",5.1f);
            Call(f.spatial,"RefreshSpeechClock");
            Need(f.view.dialogueText.text==""&&!f.view.dialoguePanel.gameObject.activeSelf,"Opening speech did not expire at its original time");
            Set(f.lesson.Flow,"Step",ElevatorStep.Jump);Set(f.lesson.Flow,"Elapsed",f.lesson.settings.friendReplySeconds+.01f);
            Need(ElevatorSpeechAnchor.VisibleLine(f.lesson,"친구",ElevatorDialogue.ShotGesture)=="","Jump speech did not expire");
            Set(f.lesson.Flow,"Step",ElevatorStep.Returning);f.view.dialogueText.text="계단으로 1층에 내려가요.";
            f.spatial.Present("안내",f.view.dialogueText.text,"");f.spatial.SetCinematicVisibility(false);
            Need(f.view.dialogueText.text==""&&f.view.GetComponent<CanvasGroup>().alpha==0,"Narration returned as letterbox dialogue");
            foreach(var guidedStep in new[]{ElevatorStep.PosterPointing,ElevatorStep.Connecting})
            {
                Set(f.lesson.Flow,"Step",guidedStep);
                var description=new object[]{null,null};
                Need((bool)Call(f.spatial,"DescribeStatus",description)&&!string.IsNullOrEmpty((string)description[1]),"Removing captions also removed essential guidance: "+guidedStep);
            }
            var rig=Go("player speech source").transform;f.lesson.presentation.space.playerRig=rig;f.camera.transform.SetParent(rig,true);
            Set(f.lesson.Flow,"Step",ElevatorStep.CallResponse);Set(f.lesson.Flow,"Elapsed",0f);
            var anchor=new ElevatorSpeechAnchor();var bounds=new Bounds(Vector3.zero,new Vector3(580,300,0));
            var first=anchor.Get(f.lesson,"나",bounds,.0018f);
            Need(anchor.CurrentVoice==ElevatorSpeechAnchor.Voice.Player&&anchor.Source==rig,"First-person speech has no player source");
            f.camera.transform.rotation=Quaternion.Euler(15,70,0);var turned=anchor.Get(f.lesson,"나",bounds,.0018f);
            Need(first.position==turned.position&&Quaternion.Angle(first.rotation,turned.rotation)<.01f,"First-person bubble follows the head");
            var delta=new Vector3(.3f,0,.1f);rig.position+=delta;
            Need(Vector3.Distance(anchor.Get(f.lesson,"나",bounds,.0018f).position,first.position+delta)<.0001f,"First-person bubble did not follow the player object");
            var plate=Go("identity source",false,f.lesson.transform).AddComponent<ElevatorObservation>();plate.clue=ElevatorClue.Identity;plate.transform.position=new Vector3(.6f,1.1f,2);
            anchor.Reset();Set(f.lesson.Flow,"Step",ElevatorStep.DoorPosterPointing);Set(f.lesson.Flow,"PreviewClue",(ElevatorClue?)ElevatorClue.Identity);
            anchor.Get(f.lesson,"엘리베이터 안내판",bounds,.0018f);
            Need(anchor.CurrentVoice==ElevatorSpeechAnchor.Voice.Object&&anchor.Source==plate.transform,"Observed object's bubble has the wrong source");
            Set(f.lesson.Flow,"PreviewClue",null);
            Set(f.lesson.Flow,"Step",ElevatorStep.Complete);f.view.dialogueText.text=ElevatorDialogue.Complete;
            f.spatial.Present("친구",ElevatorDialogue.Complete,"");f.spatial.SetCinematicVisibility(false);
            Need(f.spatial.notebook.gameObject.activeSelf&&f.view.GetComponent<CanvasGroup>().alpha==1&&!f.view.dialoguePanel.gameObject.activeSelf,"Speech migration hid the completion notebook");
        }

        static void SpeechTailDirections()
        {
            var f=new Fixture();f.view.dialogueText.text=ElevatorDialogue.FriendQuestion;f.spatial.LayoutVRHud(Frame(f.camera,0));f.spatial.Layout(true);
            var panel=f.view.dialoguePanel;var tail=panel.Find("SpeechTail") as RectTransform;
            Need(tail&&tail.GetComponent<Image>()&&!tail.GetComponent<Image>().raycastTarget,"Speech tail blocks selection input");
            var points=new[]{Vector3.left*1000,Vector3.right*1000,Vector3.up*1000,Vector3.down*1000};
            var angles=new[]{-90f,90f,180f,0f};
            for(int i=0;i<points.Length;i++)
            {
                f.spatial.storybook.PointSpeechTail(panel.TransformPoint(points[i]));
                Need(Mathf.Abs(Mathf.DeltaAngle(tail.localEulerAngles.z,angles[i]))<.01f,"Bubble tail does not point towards source side "+i);
            }
        }
    }
}
#endif
