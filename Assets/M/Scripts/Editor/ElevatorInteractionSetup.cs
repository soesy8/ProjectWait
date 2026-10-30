#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using DwtD.TestScene;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorInteractionSetup
    {
        const string Temp="Assets/M/CodexTemp/ElevatorInteraction_20261030~";
        const string Models="Assets/M/Arts/Models/ElevatorInteraction";
        const string Materials="Assets/M/Arts/Materials/ElevatorInteraction";
        static Material overlay;
        const string MaskModels="Assets/M/Arts/Models/ElevatorMaskOutline";
        static Dictionary<string,ElevatorSportsCentreSetup.Part> parts;
        static readonly Color Paper=new Color(1,.965f,.86f),Ink=new Color(.28f,.19f,.14f),Teal=new Color(.16f,.48f,.44f);
        static Material Material(string path,Shader shader)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
            EditorUtility.SetDirty(m);return m;
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            mesh.name=name;
            var triangles=mesh.triangles;mesh.subMeshCount=1;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            string path=name.StartsWith("Target_")?MaskModels+"/MaskGeometry_"+name.Substring(7)+".asset":Models+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Mesh Extract(string part,string key)
        {
            var p=parts[part];var vertices=new Vector3[p.count];var normals=new Vector3[p.count];var triangles=new int[p.count];
            using(var reader=new BinaryReader(File.OpenRead("Assets/M/Arts/Models/SportsCentre3F/SportsCentreGeometry.bytes")))
            {
                reader.BaseStream.Position=p.offset;int count=reader.ReadInt32();
                for(int i=0;i<count;i++){vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());reader.ReadSingle();reader.ReadSingle();reader.ReadInt32();triangles[i]=i;}
            }
            var mesh=new Mesh{indexFormat=p.count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;return SaveMesh(mesh,key);
        }
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        static void ConfigureOutline(ElevatorObservation observation,Transform geometry,string[] names,int id)
        {
            var inputs=new List<ElevatorOutlineGeometry>();
            if(names!=null)
            {
                for(int i=0;i<names.Length;i++)
                {
                    string n=names[i];var native=Find(geometry,n);var p=parts[n];var anchor=new GameObject("OutlineMaskAnchor_"+i).transform;anchor.SetParent(observation.transform,false);
                    if(native)anchor.SetParent(native,false);else{anchor.SetPositionAndRotation(new Vector3(p.position[0],p.position[1],p.position[2]),new Quaternion(p.rotation[0],p.rotation[1],p.rotation[2],p.rotation[3]));anchor.localScale=new Vector3(p.scale[0],p.scale[1],p.scale[2]);}
                    inputs.Add(new ElevatorOutlineGeometry{mesh=Extract(n,"Target_"+id+"_"+i),anchor=anchor});
                }
            }
            else if(observation.clue==ElevatorClue.Friend||observation.clue==ElevatorClue.Ball||observation.clue==ElevatorClue.Rescuer||observation.clue==ElevatorClue.Phone)
            {
                foreach(var source in observation.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is LineRenderer)&&!r.name.Contains("Reaction")&&!r.name.Contains("Shadow")&&!r.name.StartsWith("FaceExpression_")&&!r.name.StartsWith("ButtonPress")&&!r.GetComponent<TMP_Text>()))
                    if(source is SkinnedMeshRenderer||source.GetComponent<MeshFilter>())inputs.Add(new ElevatorOutlineGeometry{sourceRenderer=source});
            }
            var target=observation.GetComponent<ElevatorOutlineTarget>();if(!target)target=observation.gameObject.AddComponent<ElevatorOutlineTarget>();target.geometry=inputs.ToArray();target.SetVisible(false);observation.outlineTarget=target;EditorUtility.SetDirty(observation);EditorUtility.SetDirty(target);
        }
        public static void AddActorOutline(ElevatorObservation observation,int key){ConfigureOutline(observation,null,null,key);}
        static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        static TMP_Text Text(string name,Transform parent,Vector2 size,Vector2 pos,string text,TMP_FontAsset font,float point)
        {
            var r=Rect(name,parent,size,pos);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=point;t.color=Ink;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,float y,string text,TMP_FontAsset font)
        {
            var rect=Rect(name,parent,new Vector2(450,70),new Vector2(0,y));var graphic=rect.gameObject.AddComponent<CorridorBubbleGraphic>();graphic.color=Teal;graphic.borderColor=Paper;graphic.borderWidth=2;graphic.cornerRadius=15;graphic.extrusion=5;graphic.bevelWidth=3;graphic.raycastTarget=true;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=graphic;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.94f,.9f);colors.pressedColor=new Color(.6f,.8f,.8f);colors.disabledColor=new Color(.65f,.65f,.65f,.6f);button.colors=colors;
            var label=Text("Label",rect,new Vector2(420,62),Vector2.zero,text,font,26);label.color=Paper;return button;
        }
        static void Overlay(Canvas canvas)
        {
            foreach(var g in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                if(g is TMP_Text text)
                {
                    var original=text.fontSharedMaterial;if(!original)continue;string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));if(string.IsNullOrEmpty(key))key="Default";
                    if(original.shader.name=="TextMeshPro/Distance Field Overlay"&&AssetDatabase.GetAssetPath(original).StartsWith(Materials))continue;
                    string path=Materials+"/FontOverlay_"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!m){m=Object.Instantiate(original);m.name="Readable UI Overlay";m.shader=Shader.Find("TextMeshPro/Distance Field Overlay");m.renderQueue=4101;AssetDatabase.CreateAsset(m,path);}text.fontSharedMaterial=m;
                }
                else if(!(g is TMP_SubMeshUI))g.material=overlay;
                EditorUtility.SetDirty(g);
            }
        }
        [MenuItem("Tools/Wait/Elevator/Apply movement, pause and mask outlines")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/M/Scene/Elevator_Safety_Prototype.unity")throw new InvalidOperationException("Open the M elevator scene.");
            Directory.CreateDirectory(Temp);Directory.CreateDirectory(Models);Directory.CreateDirectory(MaskModels);Directory.CreateDirectory(Materials);Directory.CreateDirectory("Assets/M/Arts/Rendering");AssetDatabase.Refresh();
            if(!File.Exists(Temp+"/BeforeInteraction.unity"))EditorSceneManager.SaveScene(scene,Temp+"/BeforeInteraction.unity",true);
            var l=Object.FindFirstObjectByType<ElevatorLesson>();var space=l.presentation.space;var map=space.sportsCentre;var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;string pipelinePath="Assets/M/Arts/Rendering/ElevatorScenePipeline.asset";
            if(!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(pipeline),pipelinePath);
            var scenePipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);scenePipeline.shadowDistance=60;scenePipeline.shadowCascadeCount=4;scenePipeline.mainLightShadowmapResolution=2048;EditorUtility.SetDirty(scenePipeline);
            var rendering=l.GetComponent<ElevatorSceneRendering>();if(!rendering)rendering=l.gameObject.AddComponent<ElevatorSceneRendering>();rendering.scenePipeline=scenePipeline;
            var movement=space.playerRig.GetComponent<ElevatorPlayerMovement>();if(!movement)movement=space.playerRig.gameObject.AddComponent<ElevatorPlayerMovement>();movement.lesson=l;movement.playerCamera=l.playerCamera;controls.movement=movement;
            var controller=space.playerRig.GetComponent<CharacterController>();controller.height=1.23f;controller.radius=.22f;controller.center=new Vector3(0,.615f,0);controller.stepOffset=.26f;controller.skinWidth=.015f;controller.minMoveDistance=0;controller.enabled=true;
            foreach(var b in space.playerRig.GetComponentsInChildren<Behaviour>(true))if(b.GetType().FullName.Contains(".Locomotion.")||b.GetType().Name=="DynamicMoveProvider"||b.GetType().Name=="CharacterControllerDriver")b.enabled=false;
            overlay=Material(Materials+"/UIOverlay.mat",Shader.Find("ProjectWait/M/UI Overlay"));overlay.renderQueue=4100;
            ElevatorMaskOutlineSetup.EnsureRenderer();
            var spatial=l.view.spatial;spatial.hud.renderMode=RenderMode.ScreenSpaceOverlay;spatial.hud.sortingOrder=50;Overlay(spatial.hud);
            if(!controls.reticle){var r=Rect("CentreAim",spatial.hud.transform,new Vector2(28,28),Vector2.zero);controls.reticle=r.gameObject.AddComponent<ElevatorReticleGraphic>();controls.reticle.raycastTarget=false;controls.reticle.material=overlay;}
            if(!controls.reticle.GetComponent<CanvasRenderer>())controls.reticle.gameObject.AddComponent<CanvasRenderer>();
            controls.centerLaser.startWidth=.0014f;controls.centerLaser.endWidth=.0022f;controls.centerLaser.positionCount=2;controls.centerLaser.useWorldSpace=true;controls.centerLaser.shadowCastingMode=ShadowCastingMode.Off;
            var pause=l.GetComponent<ElevatorPauseMenu>();if(!pause)pause=l.gameObject.AddComponent<ElevatorPauseMenu>();pause.lesson=l;pause.controls=controls;l.pauseMenu=pause;
            if(!pause.canvas)
            {
                var root=Rect("ElevatorPauseCanvas",l.transform,new Vector2(1280,720),Vector2.zero);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;canvas.worldCamera=l.playerCamera;
                var scaler=root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
                root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();var xr=root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();xr.checkFor3DOcclusion=false;xr.checkFor2DOcclusion=false;
                var dark=Rect("Dim",root,new Vector2(6000,4000),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();dark.color=new Color(.05f,.065f,.07f,.76f);dark.raycastTarget=true;
                var card=Rect("PauseCard",root,new Vector2(550,560),Vector2.zero);var paper=card.gameObject.AddComponent<CorridorBubbleGraphic>();paper.color=Paper;paper.borderColor=Ink;paper.borderWidth=2;paper.cornerRadius=25;paper.extrusion=6;paper.bevelWidth=3;paper.raycastTarget=false;
                Text("Heading",card,new Vector2(470,55),new Vector2(0,222),"일시 정지",l.view.font,36);
                pause.resumeButton=Button("Resume",card,125,"게임 재개",l.view.font);pause.restartButton=Button("Restart",card,32,"처음부터 다시하기",l.view.font);pause.titleButton=Button("Title",card,-61,"타이틀로 돌아가기",l.view.font);pause.quitButton=Button("Quit",card,-154,"게임 종료하기",l.view.font);pause.titleButton.interactable=false;pause.canvas=canvas;
            }
            Overlay(pause.canvas);pause.canvas.gameObject.SetActive(false);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(canvas.renderMode!=RenderMode.WorldSpace||canvas==spatial.hud||canvas==pause.canvas||canvas==l.view.GetComponent<Canvas>()||canvas.GetComponent<ElevatorCinematicOverlay>()||canvas.transform.IsChildOf(spatial.notebook))continue;
                var b=canvas.GetComponent<ElevatorBillboardCanvas>();if(!b)b=canvas.gameObject.AddComponent<ElevatorBillboardCanvas>();b.playerCamera=l.playerCamera;
            }
            var manifest=JsonUtility.FromJson<ElevatorSportsCentreSetup.Manifest>(File.ReadAllText("Assets/M/Arts/Models/SportsCentre3F/SportsCentreManifest.json"));parts=manifest.objects.ToDictionary(p=>p.name);
            // The courtyard was decorative while the player was stationary.
            // Match its authored surface so walking remains grounded after the exit.
            const string courtyardPart="진입부 외부 중정 바닥";
            var courtyard=Find(map.geometry,"ExteriorCourtyardCollision");
            if(!courtyard){courtyard=new GameObject("ExteriorCourtyardCollision").transform;courtyard.SetParent(map.geometry,false);}
            var groundPart=parts[courtyardPart];courtyard.SetPositionAndRotation(new Vector3(groundPart.position[0],groundPart.position[1],groundPart.position[2]),new Quaternion(groundPart.rotation[0],groundPart.rotation[1],groundPart.rotation[2],groundPart.rotation[3]));courtyard.localScale=new Vector3(groundPart.scale[0],groundPart.scale[1],groundPart.scale[2]);
            var groundCollider=courtyard.GetComponent<MeshCollider>();if(!groundCollider)groundCollider=courtyard.gameObject.AddComponent<MeshCollider>();groundCollider.sharedMesh=Extract(courtyardPart,"ExteriorCourtyardCollision");
            foreach(var t in l.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("OutlineMaskAnchor_")).ToArray())if(t)Object.DestroyImmediate(t.gameObject);
            var bindings=new Dictionary<string,string[]>{
                {"3F_Door_KeepClear",new[]{"3F · Lift 1 · 왼쪽 문 [슬라이드]","3F · Lift 1 · 오른쪽 문 [슬라이드]"}},
                {"3F_Side_Emergency_1.28m",new[]{"3F · Cabin · 측면 비상 버튼 [높이 1.28m]"}},
                {"3F_Front_Emergency_2.15m",new[]{"3F · Cabin · 전면 비상 버튼 [높이 2.15m]"}},
                {"3F_LiftIdentity_HB-EL-01",new[]{"3F · Cabin · 승강기 식별 정보판"}},
                {"3F_FacilityDirectory_NoHighlight",new[]{"3F · 층별 시설 안내 프레임"}},
                {"3F_SafetyPoster",new[]{"3F · Cabin · 안전 안내 프레임"}},
                {"3F_Side_Floor01",new[]{"3F · Cabin · 측면 01층 링"}},
                {"3F_Front_Floor01",new[]{"3F · Cabin · 전면 01층 링"}},
                {"3F_Display_LastKnown3",new[]{"3F · Cabin · 전면 디스플레이"}},
                {"3F_Lobby_Call",new[]{"3F · 호출 버튼 외곽.001"}},
                {"1F_MainExit",new[]{"자동문 이동 세로틀","자동문 이동 세로틀.001","자동문 이동 세로틀.002","자동문 이동 세로틀.003","자동문 이동 가로틀","자동문 이동 가로틀.001","자동문 이동 가로틀.002","자동문 이동 가로틀.003"}}
            };
            int index=0;
            foreach(var o in l.GetComponentsInChildren<ElevatorObservation>(true))
            {
                if(!o.gameObject.activeInHierarchy)continue;
                if(o.name=="3F_Display_LastKnown3")
                {
                    var p=parts["3F · Cabin · 전면 디스플레이"];o.transform.position=new Vector3(p.position[0],p.position[1],p.position[2]+.04f);o.GetComponent<BoxCollider>().size=new Vector3(.53f,.26f,.045f);
                }
                bindings.TryGetValue(o.name,out var names);ConfigureOutline(o,map.geometry,names,index++);
            }
            map.thirdFloorAutoDoorLeft=Find(map.geometry,"3F · 자동문 왼쪽 문짝 [애니메이션]");map.thirdFloorAutoDoorRight=Find(map.geometry,"3F · 자동문 오른쪽 문짝 [애니메이션]");
            foreach(var component in new UnityEngine.Object[]{l,map,movement,controls,pause,rendering,spatial})EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Temp+"/Setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,targets=index,shadowDistance=scenePipeline.shadowDistance,pipeline=pipelinePath,pc="WASD / mouse / centre ray / Escape",vr="Left-hand primary2DAxis; menu or right B pauses"},Newtonsoft.Json.Formatting.Indented));
        }
    }
}
#endif
