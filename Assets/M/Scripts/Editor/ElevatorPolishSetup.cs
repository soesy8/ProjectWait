#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Rendering.Universal;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorPolishSetup
    {
        const string Root="Assets/M/";
        const string Mats=Root+"Arts/Materials/Elevator/";
        const string Models=Root+"Arts/Models/Elevator/";
        static TMP_FontAsset font;
        static Material cream,mint,teal,paper,ink,orange;
        public static string Refine()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Open the saved M scene in Edit Mode.");
            var l=Object.FindFirstObjectByType<ElevatorLesson>();
            cream=Mat("Cream");orange=Mat("BasketballOrange");
            foreach(string name in new[]{"SafetyNotice","SafetyRules"})
            {var obsolete=l.transform.Find("Cabin/"+name);if(obsolete)Object.DestroyImmediate(obsolete.gameObject);}
            foreach(var door in new[]{l.presentation.leftDoor,l.presentation.rightDoor})door.localPosition=new Vector3(door.localPosition.x,1.10f,1.74f);
            if(!l.presentation.reactionMarks)
            {
                var marks=new GameObject("FriendReactionMarks").transform;marks.SetParent(l.friend,false);marks.localPosition=new Vector3(.34f,1.44f,-.1f);l.presentation.reactionMarks=marks;
                for(int i=0;i<3;i++)
                {var mark=Primitive("ExcitedMark"+i,marks,new Vector3(.075f*i,-.03f*i,0),new Vector3(.04f,.14f-i*.015f,.035f),orange,PrimitiveType.Cube);mark.transform.localRotation=Quaternion.Euler(0,0,-20-i*25);}
                marks.gameObject.SetActive(false);
            }
            var rescue=l.presentation.rescuer.transform;
            if(!rescue.Find("LeftLongTrouser"))
            {
                var trouser=Mat("Rescuer/Rescuer_9");
                Primitive("LeftLongTrouser",rescue,new Vector3(-.11f,.24f,0),new Vector3(.145f,.16f,.16f),trouser,PrimitiveType.Capsule);
                Primitive("RightLongTrouser",rescue,new Vector3(.11f,.24f,0),new Vector3(.145f,.16f,.16f),trouser,PrimitiveType.Capsule);
            }
            var interaction=l.friend.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interaction.colliders.Clear();interaction.colliders.Add(l.friend.GetComponent<Collider>());
            var ball=l.presentation.friendBall.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            ball.colliders.Clear();ball.colliders.Add(l.presentation.friendBall.GetComponent<Collider>());
            RoundedMesh("Rail",new Vector3(.06f,.06f,3.5f),.028f);
            RoundedMesh("LeftDoor",new Vector3(.81f,2.18f,.08f),.025f);RoundedMesh("RightDoor",new Vector3(.81f,2.18f,.08f),.025f);
            RoundedMesh("IntercomPanel",Vector3.one,.07f);
            font=l.view.font;RefreshGlyphs(font,l);
            l.view.Build();l.view.subtitle.text="농구를 마치고, 친구 집으로";l.view.dialogueText.text=l.settings.lobbyDialogue;l.view.ApplyCompactLayout();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Text height, responsive composition, rounded meshes and exclusive XR collider registration saved.";
        }
        static void RefreshGlyphs(TMP_FontAsset f,ElevatorLesson l)
        {
            var chars=new string((string.Join("",Directory.GetFiles(Root+"Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText))+JsonUtility.ToJson(l.settings)).Where(c=>!char.IsControl(c)).Distinct().ToArray());
            f.atlasPopulationMode=AtlasPopulationMode.Dynamic;f.TryAddCharacters(chars,out string missing);f.atlasPopulationMode=AtlasPopulationMode.Static;
            EditorUtility.SetDirty(f);foreach(var atlas in f.atlasTextures)EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssetIfDirty(f);
        }
        [MenuItem("Wait/M/Polish Elevator Story")]
        public static string Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Open the saved M elevator scene in Edit Mode first.");
            var l=Object.FindFirstObjectByType<ElevatorLesson>();var p=l.presentation;
            if(l.transform.Find("PolishedDetails"))throw new InvalidOperationException("Polish already applied; refine existing objects.");
            Directory.CreateDirectory(Models);Directory.CreateDirectory(Mats+"Rescuer");
            font=CreateFonts(l);l.view.font=font;
            foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true)))
            {t.font=font;t.fontSharedMaterial=font.material;t.fontStyle=FontStyles.Normal;t.enableAutoSizing=false;}
            cream=Mat("Cream");mint=Mat("Mint");teal=Mat("Teal");paper=Mat("Paper");ink=Mat("Ink");orange=Mat("BasketballOrange");
            int removed=0;
            var cabin=l.transform.Find("Cabin");
            foreach(var t in l.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"FrontWall","FrontMint","DoorFrame","DoorSeam","TileJoint","SunlightPatch0","SunlightPatch1","SunlightPatch2"}.Contains(t.name)).ToArray())
            {Object.DestroyImmediate(t.gameObject);removed++;}
            var oldRenderer=p.closedDoorCollider.GetComponent<MeshRenderer>();if(oldRenderer)Object.DestroyImmediate(oldRenderer);
            var oldFilter=p.closedDoorCollider.GetComponent<MeshFilter>();if(oldFilter)Object.DestroyImmediate(oldFilter);
            p.closedDoorCollider.name="DoorInteraction";
            var group=new GameObject("PolishedDetails").transform;group.SetParent(l.transform,false);
            var shadowMat=NewMat("ContactShadow",new Color32(155,134,101,255),0);
            p.friendShadow=Primitive("FriendContactShadow",group,new Vector3(-.72f,.012f,.45f),new Vector3(.58f,.008f,.34f),shadowMat,PrimitiveType.Sphere).transform;
            // Quiet floor, rounded handrails and a distinct control panel replace the busy tile grid.
            foreach(var rail in new[]{"SafetyRailLeft","SafetyRailRight"})
            {var t=cabin.Find(rail);t.GetComponent<MeshFilter>().sharedMesh=RoundedMesh("Rail",new Vector3(.06f,.06f,3.5f),.028f);t.localScale=Vector3.one;t.GetComponent<Renderer>().sharedMaterial=paper;}
            foreach(var door in new[]{p.leftDoor,p.rightDoor})
            {door.GetComponent<MeshFilter>().sharedMesh=RoundedMesh(door.name,new Vector3(.81f,2.18f,.08f),.025f);door.localScale=Vector3.one;door.GetComponent<Renderer>().sharedMaterial=NewMat("DoorIvory",new Color32(223,218,196,255),.65f);}
            var intercom=cabin.Find("EmergencyIntercom");intercom.GetComponent<MeshFilter>().sharedMesh=RoundedMesh("IntercomPanel",new Vector3(1,1,1),.07f);
            p.intercomButton=cabin.Find("BellButton");p.intercomButton.GetComponent<Renderer>().sharedMaterial=orange;
            var label=cabin.Find("IntercomLabel").GetComponent<TMP_Text>();label.fontSize=.62f;label.text="비상통화";label.color=new Color32(255,246,219,255);
            var bell=cabin.Find("BellSymbol").GetComponent<TMP_Text>();bell.text="☎";bell.fontSize=1.05f;bell.color=new Color32(73,51,39,255);
            var identity=cabin.Find("IdentityText").GetComponent<TMP_Text>();identity.text="햇살아파트\nM-01";identity.fontSize=.72f;identity.rectTransform.sizeDelta=new Vector2(.78f,.30f);identity.alignment=TextAlignmentOptions.Center;
            var safety=cabin.Find("SafetyRules").GetComponent<TMP_Text>();safety.text="안전하게 함께\n\n멈추면 비상통화\n문에서 떨어지기";safety.fontSize=.61f;safety.rectTransform.sizeDelta=new Vector2(.65f,.86f);safety.alignment=TextAlignmentOptions.Center;
            l.floorLabel.fontSize=.82f;
            // Reuse the existing friend mesh; only local M copies gain expression shapes.
            var face=l.friend.GetComponentInChildren<SkinnedMeshRenderer>();
            face.sharedMesh=ExpressionMesh(face.sharedMesh,"FriendExpressions",false);
            var ballCollider=p.friendBall.gameObject.AddComponent<SphereCollider>();ballCollider.radius=.17f;
            var observation=p.friendBall.gameObject.AddComponent<ElevatorObservation>();observation.lesson=l;observation.clue=ElevatorClue.Ball;
            observation.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().colliders.Add(ballCollider);
            MakeRescuer(l,p);
            // Warm, clearer cel bands without changing project-wide lighting or the URP asset.
            l.cabinLight.transform.rotation=Quaternion.Euler(35,-30,0);
            foreach(var r in l.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.Contains("Wall")||r.name.Contains("Ceiling")||r.name.Contains("DoorSide")||r.name.Contains("UpperWall")))r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach(var path in new[]{"Cream","Mint","Teal","Floor","Paper","DoorIvory"})
            {var m=Mat(path);m.SetColor("_ShadeColor",new Color32(189,200,195,255));m.SetFloat("_MidTone",.90f);m.SetFloat("_ShadowThreshold",.02f);m.SetFloat("_LightThreshold",.62f);m.SetFloat("_BandSoftness",.012f);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}
            var camera=l.playerCamera.GetUniversalAdditionalCameraData();camera.renderPostProcessing=true;camera.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;camera.antialiasingQuality=AntialiasingQuality.High;
            p.playerBall.localScale=Vector3.one*.65f;
            l.settings.friendReplySeconds=.5f;l.settings.jumpSeconds=.75f;l.settings.afterJumpSeconds=1.2f;l.settings.connectionSeconds=2.1f;l.settings.firstHintSeconds=12;l.settings.secondHintSeconds=24;l.settings.rescueWaitSeconds=10;l.settings.rescueSubtitleSeconds=3.4f;
            l.settings.liftId="M-01";l.settings.safetyNotice="안전하게 함께\n멈추면 비상통화\n문에서 떨어지기";
            EditorUtility.SetDirty(l.settings);AssetDatabase.SaveAssetIfDirty(l.settings);
            int uiBefore=l.view.GetComponentsInChildren<Transform>(true).Length;
            l.view.Build();l.view.subtitle.text="농구를 마치고, 친구 집으로";l.view.dialogueText.text=l.settings.lobbyDialogue;
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("M scene save failed.");
            return $"Removed {removed} obsolete scene objects; rebuilt UI ({uiBefore} old objects). Font {font.faceInfo.styleName}, {font.characterTable.Count} static glyphs. M scene saved.";
        }
        static TMP_FontAsset CreateFonts(ElevatorLesson l)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"Arts/Fonts/NotoSansCJKkr-Medium.otf");if(!source)throw new Exception("Import the Medium font first.");
            var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Arts/Fonts/ElevatorReadable.asset");if(f)return f;
            var fallback=TMP_FontAsset.CreateFontAsset(source,48,5,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);fallback.name="ElevatorKoreanFallback";
            var serialized=new SerializedObject(fallback);serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();SaveFont(fallback,Root+"Arts/Fonts/ElevatorKoreanFallback.asset");
            f=TMP_FontAsset.CreateFontAsset(source,48,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);f.name="ElevatorReadable";
            string characters=string.Join("",Directory.GetFiles(Root+"Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText))+JsonUtility.ToJson(l.settings)+"☎↑—●○… ·0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            characters=new string(characters.Where(c=>!char.IsControl(c)).Distinct().OrderBy(c=>c).ToArray());
            f.TryAddCharacters(characters,out string missing);if(!string.IsNullOrEmpty(missing))Debug.LogWarning("M font unsupported source characters: "+missing);
            f.atlasPopulationMode=AtlasPopulationMode.Static;f.fallbackFontAssetTable=new List<TMP_FontAsset>{fallback};SaveFont(f,Root+"Arts/Fonts/ElevatorReadable.asset");return f;
        }
        static void SaveFont(TMP_FontAsset f,string path)
        {
            AssetDatabase.CreateAsset(f,path);AssetDatabase.AddObjectToAsset(f.material,f);foreach(var atlas in f.atlasTextures){atlas.filterMode=FilterMode.Bilinear;AssetDatabase.AddObjectToAsset(atlas,f);}EditorUtility.SetDirty(f);AssetDatabase.SaveAssetIfDirty(f);
        }
        static Mesh ExpressionMesh(Mesh source,string name,bool rescuer)
        {
            string path=Models+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
            var m=Object.Instantiate(source);m.name=name;m.ClearBlendShapes();var vertices=m.vertices;
            var blink=new Vector3[vertices.Length];
            foreach(int i in m.GetTriangles(1).Distinct())blink[i].z=(1.02f-vertices[i].z)*.92f;
            foreach(int i in m.GetTriangles(16).Distinct().Where(i=>vertices[i].z>.96f))blink[i].z=(1.02f-vertices[i].z)*.92f;
            m.AddBlendShapeFrame("Blink",100,blink,null,null);
            var surprised=new Vector3[vertices.Length];
            foreach(int sub in new[]{18,19,20,16})foreach(int i in m.GetTriangles(sub).Distinct().Where(i=>vertices[i].z<.96f))
            {surprised[i].x=-vertices[i].x*.45f;surprised[i].z=(vertices[i].z-.883f)*.25f;if(sub==16||sub==20)surprised[i].y=.025f;}
            m.AddBlendShapeFrame("Surprised",100,surprised,null,null);
            if(rescuer)
            {
                foreach(int sub in new[]{13,14,15,21,23})m.SetTriangles(Array.Empty<int>(),sub);
                var shoes=m.GetTriangles(12);var keep=new List<int>();for(int i=0;i<shoes.Length;i+=3)if(vertices[shoes[i]].z<.28f){keep.Add(shoes[i]);keep.Add(shoes[i+1]);keep.Add(shoes[i+2]);}m.SetTriangles(keep,12);
            }
            AssetDatabase.CreateAsset(m,path);return m;
        }
        static void MakeRescuer(ElevatorLesson l,ElevatorStoryPresentation p)
        {
            var root=p.rescuer.transform;for(int i=root.childCount-1;i>=0;i--)Object.DestroyImmediate(root.GetChild(i).gameObject);
            var actor=Object.Instantiate(l.friend.GetChild(0).gameObject,root);actor.name="RescuerCharacter";actor.transform.localPosition=new Vector3(0,.1f,0);actor.transform.localRotation=Quaternion.Euler(0,180,0);actor.transform.localScale=Vector3.one;
            root.localScale=new Vector3(1.10f,1.28f,1.10f);
            foreach(var a in actor.GetComponentsInChildren<Animator>())a.enabled=false;
            var r=actor.GetComponentInChildren<SkinnedMeshRenderer>();r.sharedMesh=ExpressionMesh(r.sharedMesh,"RescuerExpressions",true);
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                if(!new[]{7,8,9,10,11,12,17}.Contains(i))continue;
                var copy=new Material(materials[i]);copy.name="Rescuer_"+i;
                var c=i==8?new Color32(71,142,131,255):i==17?new Color32(231,149,112,255):new Color32(45,87,80,255);
                copy.SetColor("_BaseColor",c);AssetDatabase.CreateAsset(copy,Mats+"Rescuer/"+copy.name+".mat");materials[i]=copy;
            }
            r.sharedMaterials=materials;
            foreach(var h in actor.GetComponentsInChildren<Transform>().Where(t=>t.name=="head"))h.localScale=Vector3.one*.87f;
            Primitive("SafetyCap",root,new Vector3(0,1.34f,0),new Vector3(.63f,.22f,.56f),teal,PrimitiveType.Sphere);
            Primitive("CapBrim",root,new Vector3(0,1.27f,-.23f),new Vector3(.61f,.045f,.31f),teal,PrimitiveType.Sphere);
            Primitive("CapMark",root,new Vector3(0,1.34f,-.276f),new Vector3(.10f,.065f,.012f),orange,PrimitiveType.Cube);
            for(int i=0;i<2;i++)
            {
                float x=i==0?-.105f:.105f;
                Primitive("SafetyVest"+i,root,new Vector3(x,.70f,-.144f),new Vector3(.18f,.34f,.048f),orange,PrimitiveType.Cube);
                Primitive("ReflectiveBand"+i,root,new Vector3(x,.68f,-.171f),new Vector3(.18f,.038f,.01f),paper,PrimitiveType.Cube);
            }
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");
        static Material NewMat(string name,Color color,float outline)
        {
            var m=Mat(name);if(m)return m;m=new Material(cream);m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_OutlineWidth",outline);AssetDatabase.CreateAsset(m,Mats+name+".mat");return m;
        }
        static GameObject Primitive(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static Mesh RoundedMesh(string name,Vector3 size,float radius)
        {
            string path=Models+name+"Rounded.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();
            Vector3 half=size*.5f,core=half-Vector3.one*radius;const int steps=8;
            for(int face=0;face<6;face++)
            {
                Vector3 normal=face==0?Vector3.right:face==1?Vector3.left:face==2?Vector3.up:face==3?Vector3.down:face==4?Vector3.forward:Vector3.back;
                Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,v=Vector3.Cross(normal,u);int start=vertices.Count;
                for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
                {
                    float hu=Mathf.Abs(Vector3.Dot(half,u)),hv=Mathf.Abs(Vector3.Dot(half,v));
                    var cube=Vector3.Scale(normal,half)+u*Coord(x,hu,radius)+v*Coord(y,hv,radius);
                    var inner=new Vector3(Mathf.Clamp(cube.x,-core.x,core.x),Mathf.Clamp(cube.y,-core.y,core.y),Mathf.Clamp(cube.z,-core.z,core.z));var n=(cube-inner).normalized;vertices.Add(inner+n*radius);normals.Add(n);
                }
                for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;indices.AddRange(new[]{a,b,c,b,d,c});}
            }
            var m=new Mesh{name=name+"Rounded"};m.SetVertices(vertices);m.SetNormals(normals);m.SetTriangles(indices,0);m.RecalculateBounds();
            if(saved){EditorUtility.CopySerialized(m,saved);Object.DestroyImmediate(m);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);return saved;}
            AssetDatabase.CreateAsset(m,path);return m;
        }
        static float Coord(int index,float half,float radius)
        {
            switch(index){case 0:return -half;case 1:return -half+radius*.134f;case 2:return -half+radius*.5f;case 3:return -half+radius;case 4:return 0;case 5:return half-radius;case 6:return half-radius*.5f;case 7:return half-radius*.134f;default:return half;}
        }
    }
}
#endif
