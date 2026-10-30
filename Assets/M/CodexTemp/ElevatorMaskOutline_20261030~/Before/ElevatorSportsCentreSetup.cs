#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorSportsCentreSetup
    {
        const string Model="Assets/M/Arts/Models/SportsCentre3F";
        const string Mats="Assets/M/Arts/Materials/SportsCentre3F";
        const string Prefab="Assets/M/Prefabs/SportsCentre3F/SportsCentreMap.prefab";
        const string QA="Assets/M/CodexTemp/UnitySportsCentreRebuild~";
        [Serializable] public class Manifest {public string source,sourceSha256;public float floorHeight;public Mat[] materials;public Part[] objects;}
        [Serializable] public class Mat {public string name,texture;public float[] color,emission;public float metallic,roughness,emissionStrength,transmission;}
        [Serializable] public class Part {public string name,parent,type,body;public string[] collections;public float[] position,rotation,scale;public long offset;public int count;}
        struct Vertex:IEquatable<Vertex>
        {
            public Vector3 p,n;public Vector2 uv;
            public bool Equals(Vertex v)=>p.Equals(v.p)&&n.Equals(v.n)&&uv.Equals(v.uv);
            public override bool Equals(object o)=>o is Vertex v&&Equals(v);
            public override int GetHashCode()=>unchecked((p.GetHashCode()*397^n.GetHashCode())*397^uv.GetHashCode());
        }
        class Chunk
        {
            public readonly List<Vector3> p=new List<Vector3>(),n=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> tri=new List<int>();
            readonly Dictionary<Vertex,int> vertices=new Dictionary<Vertex,int>();
            public void Add(Vertex v){if(!vertices.TryGetValue(v,out int i)){i=p.Count;vertices.Add(v,i);p.Add(v.p);n.Add(v.n);uv.Add(v.uv);}tri.Add(i);}
        }
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        static Quaternion Q(float[] a)=>new Quaternion(a[0],a[1],a[2],a[3]);
        static Color C(float[] a)=>new Color(a[0],a[1],a[2],a.Length>3?a[3]:1);
        static Transform Child(Transform p,string name){var c=p.Find(name);if(c)return c;var g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
        static bool Dynamic(Part p)=>p.name.StartsWith("3F · Lift 1 · ")&&p.name.Contains("[슬라이드]") || p.name.StartsWith("3F · Cabin · ")&&(p.name.Contains("01층 링")||p.name.Contains("03층 링")||p.name.Contains("비상 버튼 [")) || (p.parent??"").Contains("자동문")&&(p.parent.Contains("문짝"));
        static string Section(Part p)=>p.name.StartsWith("3F ·")||p.collections.Any(c=>c.StartsWith("31 ·")||c.StartsWith("32 ·")||c.StartsWith("3F ·"))?"3F":p.collections.Any(c=>c.StartsWith("20 ·"))?"2F":p.collections.Any(c=>c.StartsWith("40 ·")||c.StartsWith("41 ·"))?"Building & Stairs":p.collections.Any(c=>c.StartsWith("12 ·"))?"Background":"1F";

        public static string ImportModel()
        {
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Model+"/SportsCentreManifest.json"));
            Directory.CreateDirectory(Mats);Directory.CreateDirectory(Model+"/Meshes");Directory.CreateDirectory(Path.GetDirectoryName(Prefab));
            AssetDatabase.Refresh();
            Material[] materials=new Material[data.materials.Length];
            for(int i=0;i<materials.Length;i++)
            {
                var d=data.materials[i];string path=Mats+"/SC_"+i.ToString("00")+".mat";
                var m=AssetDatabase.LoadAssetAtPath<Material>(path);bool fresh=!m;
                if(fresh)m=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.name=d.name;var color=C(d.color).gamma;color.a=1;
                if(!string.IsNullOrEmpty(d.texture))color=Color.white;
                m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",d.name.StartsWith("03")||d.name.StartsWith("04")?.5f:d.metallic);m.SetFloat("_Smoothness",Mathf.Clamp01(1-d.roughness));m.SetFloat("_Cull",2);
                if(!string.IsNullOrEmpty(d.texture))m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(d.texture));
                if(d.emissionStrength>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",C(d.emission)*Mathf.Min(d.emissionStrength,.85f));}
                bool glass=d.name.StartsWith("09")||d.name.StartsWith("10")||d.name.StartsWith("33");
                if(glass)
                {
                    color.a=d.name.StartsWith("33")?.065f:d.name.StartsWith("10")?.3f:.13f;m.SetColor("_BaseColor",color);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
                }
                if(fresh)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);materials[i]=m;
            }
            var root=new GameObject("SportsCentreMap");var chunks=new Dictionary<string,Chunk>();var parents=new Dictionary<string,Transform>();var matIndex=new Dictionary<string,int>();
            int triangles=0;
            using(var reader=new BinaryReader(File.OpenRead(Model+"/SportsCentreGeometry.bytes")))
            {
                foreach(var part in data.objects)
                {
                    bool dynamic=Dynamic(part);string section=Section(part);Transform parent;
                    if(dynamic)
                    {
                        if((part.parent??"").Contains("자동문")&&part.parent.Contains("문짝"))
                        {parent=Child(Child(root.transform,section),part.parent);}
                        else
                        {
                            parent=Child(Child(root.transform,section),part.name);parent.localPosition=V(part.position);parent.localRotation=Q(part.rotation);parent.localScale=V(part.scale);
                        }
                    }
                    else parent=Child(Child(root.transform,section),part.collections.FirstOrDefault()??"Architecture");
                    Matrix4x4 matrix=Matrix4x4.TRS(V(part.position),Q(part.rotation),V(part.scale));
                    if(dynamic&&!((part.parent??"").Contains("자동문")&&part.parent.Contains("문짝")))matrix=Matrix4x4.identity;
                    var normalMatrix=matrix.inverse.transpose;
                    reader.BaseStream.Position=part.offset;int count=reader.ReadInt32();triangles+=count/3;
                    for(int k=0;k<count;k++)
                    {
                        var v=new Vertex{p=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),n=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),uv=new Vector2(reader.ReadSingle(),reader.ReadSingle())};int material=reader.ReadInt32();
                        v.p=matrix.MultiplyPoint3x4(v.p);v.n=normalMatrix.MultiplyVector(v.n).normalized;
                        string key=section+"/"+parent.name+"/"+material;
                        if(!chunks.TryGetValue(key,out var chunk)){chunk=new Chunk();chunks.Add(key,chunk);parents.Add(key,parent);matIndex.Add(key,material);}
                        chunk.Add(v);
                    }
                }
            }
            int index=0,vertices=0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach(var pair in chunks)
                {
                    var d=pair.Value;vertices+=d.p.Count;var mesh=new Mesh{name=pair.Key.Replace('/','_'),indexFormat=d.p.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                    mesh.SetVertices(d.p);mesh.SetNormals(d.n);mesh.SetUVs(0,d.uv);mesh.SetTriangles(d.tri,0);mesh.RecalculateBounds();
                    string path=Model+"/Meshes/SC_"+index.ToString("000")+".asset";index++;
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
                    var go=new GameObject(materials[matIndex[pair.Key]].name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parents[pair.Key],false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=materials[matIndex[pair.Key]];
                    string n=parents[pair.Key].name;bool collision=n.Contains("벽")||n.Contains("바닥")||n.Contains("계단")||n.Contains("외장")||n.Contains("내부")||n.Contains("슬라이드")||n.Contains("문짝")||n.Contains("통창")||n.Contains("비워둔 구조")||n.Contains("농구 연습장")||n.Contains("탈의")||n.Contains("뒤쪽 자동문 / 진입부");
                    if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
                    go.isStatic=!n.Contains("슬라이드")&&!n.Contains("문짝")&&!n.Contains("층 링")&&!n.Contains("비상 버튼");
                    var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=materials[matIndex[pair.Key]].renderQueue>=3000?ShadowCastingMode.Off:ShadowCastingMode.On;
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            string report=Newtonsoft.Json.JsonConvert.SerializeObject(new{source=data.source,sourceSha256=data.sourceSha256,sourceParts=data.objects.Length,chunks=index,vertices,triangles,floorHeight=data.floorHeight,thirdFloor=7.92,modelPrefab=Prefab},Newtonsoft.Json.Formatting.Indented);
            Directory.CreateDirectory(QA);File.WriteAllText(QA+"/ImportReport.json",report);return report;
        }

        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        static ElevatorObservation Observe(Transform parent,ElevatorLesson lesson,string name,ElevatorClue clue,Vector3 position,Vector3 size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;var c=go.AddComponent<BoxCollider>();c.size=size;
            var o=go.AddComponent<ElevatorObservation>();o.lesson=lesson;o.clue=clue;
            var line=new GameObject("PointerOutline").AddComponent<LineRenderer>();line.transform.SetParent(go.transform,false);line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.widthMultiplier=.005f;line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/M/Arts/Materials/Elevator/ObservationTeal.mat");
            if(size.x<size.z)line.SetPositions(new[]{new Vector3(-size.x*.51f,-size.y*.5f,-size.z*.5f),new Vector3(-size.x*.51f,size.y*.5f,-size.z*.5f),new Vector3(-size.x*.51f,size.y*.5f,size.z*.5f),new Vector3(-size.x*.51f,-size.y*.5f,size.z*.5f)});
            else line.SetPositions(new[]{new Vector3(-size.x*.5f,-size.y*.5f,size.z*.51f),new Vector3(-size.x*.5f,size.y*.5f,size.z*.51f),new Vector3(size.x*.5f,size.y*.5f,size.z*.51f),new Vector3(size.x*.5f,-size.y*.5f,size.z*.51f)});
            line.enabled=false;o.outline=line;return o;
        }
        [MenuItem("Tools/Wait/Elevator/Reconstruct with 3F sports centre")]
        public static void Rebuild()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/M/Scene/Elevator_Safety_Prototype.unity")throw new InvalidOperationException("Open the M elevator scene first.");
            Directory.CreateDirectory(QA);
            if(!File.Exists(QA+"/BeforeSportsCentre.unity"))EditorSceneManager.SaveScene(scene,QA+"/BeforeSportsCentre.unity",true);
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(Prefab))ImportModel();
            var l=Object.FindFirstObjectByType<ElevatorLesson>();var p=l.presentation;var space=p.space;
            var prior=l.GetComponent<ElevatorSportsCentreMap>();if(prior&&prior.geometry)Object.DestroyImmediate(prior.geometry.gameObject);
            if(l.transform.Find("SportsCentre_Interactions"))Object.DestroyImmediate(l.transform.Find("SportsCentre_Interactions").gameObject);
            foreach(Transform t in l.transform.Find("Cabin"))t.gameObject.SetActive(false);
            var oldLobby=l.transform.Find("BasketballLobby");if(oldLobby)oldLobby.gameObject.SetActive(false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),l.transform);
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            var map=prior?prior:l.gameObject.AddComponent<ElevatorSportsCentreMap>();map.space=space;map.geometry=model.transform;map.lobby=new Vector3(-1.8f,7.92f,-1.8f);map.friendLobby=new Vector3(-3.2f,7.92f,1.4f);map.friendInside=new Vector3(-1.1f,7.92f,4.12f);space.sportsCentre=map;space.insidePosition=map.inside;space.lobbyPosition=map.lobby;space.friendInside=map.friendInside;space.friendOutside=map.friendLobby;
            p.lobby=model;p.exitHall=model;p.leftDoor=Find(model.transform,"3F · Lift 1 · 왼쪽 문 [슬라이드]");p.rightDoor=Find(model.transform,"3F · Lift 1 · 오른쪽 문 [슬라이드]");
            var interact=Child(l.transform,"SportsCentre_Interactions");
            p.closedDoorCollider=Observe(interact,l,"3F_Door_KeepClear",ElevatorClue.Door,new Vector3(-1.8f,9.3f,3.3f),new Vector3(1.8f,2.65f,.1f)).GetComponent<Collider>();
            map.sideIntercom=Observe(interact,l,"3F_Side_Emergency_1.28m",ElevatorClue.Intercom,new Vector3(-.435f,9.20f,4.36f),new Vector3(.045f,.15f,.15f)).transform;
            map.frontIntercom=Observe(interact,l,"3F_Front_Emergency_2.15m",ElevatorClue.FrontIntercom,new Vector3(-3.01f,10.07f,3.725f),new Vector3(.16f,.16f,.045f)).transform;
            map.identity=Observe(interact,l,"3F_LiftIdentity_HB-EL-01",ElevatorClue.Identity,new Vector3(-3.01f,9.913f,3.73f),new Vector3(.51f,.155f,.045f)).transform;
            map.facilityGuide=Observe(interact,l,"3F_FacilityDirectory_NoHighlight",ElevatorClue.FacilityGuide,new Vector3(-3.195f,9.80f,5.20f),new Vector3(.045f,1.13f,.55f)).transform;
            Observe(interact,l,"3F_SafetyPoster",ElevatorClue.Notice,new Vector3(-3.195f,9.80f,4.26f),new Vector3(.045f,1.13f,.75f));
            Observe(interact,l,"3F_Side_Floor01",ElevatorClue.FloorButtons,new Vector3(-.435f,9.17f,5.05f),new Vector3(.045f,.12f,.12f));
            Observe(interact,l,"3F_Front_Floor01",ElevatorClue.FrontFloorButtons,new Vector3(-2.87f,9.24f,3.725f),new Vector3(.12f,.12f,.045f));
            Observe(interact,l,"3F_Display_LastKnown3",ElevatorClue.Display,new Vector3(-1.8f,10.69f,3.62f),new Vector3(1.8f,.23f,.08f));
            Observe(interact,l,"3F_Lobby_Call",ElevatorClue.LobbyCall,new Vector3(-.12f,9.21f,2.975f),new Vector3(.2f,.43f,.065f));
            Observe(interact,l,"1F_MainExit",ElevatorClue.GroundExit,new Vector3(-.6f,1.25f,-5.30f),new Vector3(2.4f,2.4f,.08f));
            p.intercomButton=Find(model.transform,"3F · Cabin · 측면 비상 버튼 [높이 1.28m]");
            map.firstFloorRings=model.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("3F · Cabin ·")&&t.name.Contains("01층 링")).SelectMany(t=>t.GetComponentsInChildren<Renderer>()).ToArray();
            map.thirdFloorRings=model.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("3F · Cabin ·")&&t.name.Contains("03층 링")).SelectMany(t=>t.GetComponentsInChildren<Renderer>()).ToArray();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Model+"/SportsCentreManifest.json"));
            map.normalButton=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/SC_"+Array.FindIndex(manifest.materials,m=>m.name.StartsWith("08")).ToString("00")+".mat");
            map.selectedButton=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/SC_"+Array.FindIndex(manifest.materials,m=>m.name.StartsWith("19")).ToString("00")+".mat");
            map.autoDoorLeft=Find(Find(model.transform,"1F"),"자동문 왼쪽 문짝 [애니메이션]");map.autoDoorRight=Find(Find(model.transform,"1F"),"자동문 오른쪽 문짝 [애니메이션]");
            l.friend.position=map.friendLobby;l.friend.rotation=Quaternion.identity;
            var controller=space.playerRig.GetComponent<CharacterController>();bool enabled=controller&&controller.enabled;if(enabled)controller.enabled=false;space.playerRig.position=map.lobby;space.playerRig.rotation=Quaternion.identity;if(enabled)controller.enabled=true;
            l.playerCamera.transform.rotation=Quaternion.identity;l.playerCamera.nearClipPlane=.06f;l.playerCamera.farClipPlane=180;
            var desktop=Object.FindFirstObjectByType<ElevatorDesktopControls>();l.playerCamera.transform.position=map.lobby+Vector3.up*desktop.eyeHeight;
            l.view.spatial.intercomAnchor=map.sideIntercom;l.view.spatial.rescuerAnchor=p.rescuer.transform;
            if(!l.view.spatial.progressBackdrop)
            {
                var back=(RectTransform)new GameObject("ProgressPaper",typeof(RectTransform)).transform;back.SetParent(l.view.spatial.progress.transform.parent,false);back.SetAsFirstSibling();var graphic=back.gameObject.AddComponent<DwtD.TestScene.CorridorBubbleGraphic>();graphic.color=new Color(1,.965f,.86f,.96f);graphic.borderColor=new Color(.28f,.19f,.14f);graphic.cornerRadius=12;graphic.borderWidth=1.4f;graphic.raycastTarget=false;l.view.spatial.progressBackdrop=back;
            }
            if(!l.view.spatial.controlsBackdrop)
            {
                var back=(RectTransform)new GameObject("ControlsPaper",typeof(RectTransform)).transform;back.SetParent(l.view.spatial.controls.transform.parent,false);back.SetAsFirstSibling();var graphic=back.gameObject.AddComponent<DwtD.TestScene.CorridorBubbleGraphic>();graphic.color=new Color(1,.965f,.86f,.94f);graphic.borderColor=new Color(.28f,.19f,.14f);graphic.cornerRadius=8;graphic.borderWidth=1;graphic.raycastTarget=false;l.view.spatial.controlsBackdrop=back;
            }
            l.cabinLight.color=new Color(1,.98f,.94f);l.cabinLight.intensity=1.6f;l.cabinLight.transform.rotation=Quaternion.Euler(45,-30,0);
            var settings=l.settings;settings.building="한빛 생활체육센터";settings.liftId="본관 1호기 · HB-EL-01";settings.lobbyDialogue="오늘 패스 좋았어! 가방 챙겼지?\n3층에서 농구도 마쳤으니, 1층으로 내려가서 집에 가자.";settings.handoffDialogue="1층 눌렀지? 공 좀 잠깐 맡아 줘.\n튀기지 말고 안고 있어 줘!";settings.waitDialogue="도움을 요청했어. 문에서 떨어져 같이 기다리자.";settings.rescueWaitSeconds=12;settings.firstHintSeconds=20;settings.secondHintSeconds=40;settings.safetyNotice="안에서 뛰지 않아요. 문에 기대지 않아요.\n멈추면 비상통화로 도움 요청.\n문에서 떨어져 직원의 안내를 기다려요.";EditorUtility.SetDirty(settings);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.68f,.73f,.78f);RenderSettings.ambientEquatorColor=new Color(.46f,.49f,.51f);RenderSettings.ambientGroundColor=new Color(.30f,.29f,.27f);RenderSettings.reflectionIntensity=.85f;
            var lighting=Child(model.transform,"Unity_InteriorLighting");
            foreach(var item in new[]{new Vector3(-1.8f,10.5f,4.9f),new Vector3(-1.8f,2.6f,4.9f),new Vector3(0,10.8f,-.3f),new Vector3(0,2.8f,-.3f),new Vector3(0,11,-14)})
            {var light=new GameObject("Neutral interior fill").AddComponent<Light>();light.transform.SetParent(lighting,false);light.transform.position=item;light.type=LightType.Point;light.intensity=item.z>3?18:7;light.range=item.z<-10?17:8;light.color=new Color(1,.98f,.93f);light.shadows=LightShadows.None;}
            foreach(string path in new[]{Mats+"/SC_02.mat",Mats+"/SC_03.mat"}){var steel=AssetDatabase.LoadAssetAtPath<Material>(path);if(steel){steel.SetFloat("_Metallic",.5f);EditorUtility.SetDirty(steel);}}
            DynamicGI.UpdateEnvironment();
            var probe=new GameObject("Cabin reflection environment").AddComponent<ReflectionProbe>();probe.transform.SetParent(lighting,false);probe.transform.position=new Vector3(-1.8f,9.4f,4.8f);probe.size=new Vector3(3,3,3);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;probe.boxProjection=true;probe.intensity=.8f;
            EditorUtility.SetDirty(l);EditorUtility.SetDirty(p);EditorUtility.SetDirty(space);EditorUtility.SetDirty(map);EditorUtility.SetDirty(l.view.spatial);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(-.8f,9.2f,.6f),Quaternion.Euler(13,0,0),8);
            File.WriteAllText(QA+"/Reconstruction.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,model=Prefab,currentFloor=3,unusedLift=5,interactiveTargets=interact.childCount,sourceHash=manifest.sourceSha256,backup=QA+"/BeforeSportsCentre.unity"},Newtonsoft.Json.Formatting.Indented));
            ElevatorInteractionSetup.Apply();
        }
    }
}
#endif
