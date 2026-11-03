#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    // Non-interactive typography surfaces only: original button rings, colliders,
    // observation targets and scenario rules are deliberately left untouched.
    public static class ElevatorReadabilitySetup
    {
        const string Folder = "Assets/M/Arts/Materials/ElevatorUI/";
        const string Prefab = "Assets/M/Prefabs/ElevatorUI/ReadableSignage.prefab";
        static readonly string[] Scenes = {
            "Assets/M/Scene/Elevator_Safety_Prototype.unity",
            "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"
        };
        static Material dark, paper, gold, lightText, inkText;
        static TMP_FontAsset font;
        static Transform root;

        [MenuItem("Wait/M/Apply Elevator Readability")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save user scene changes first.");
            string original=SceneManager.GetActiveScene().path;
            if (Array.IndexOf(Scenes,original)<0) throw new InvalidOperationException("Open an M elevator scene first.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/ElevatorReadable.asset");
            if(!font)throw new InvalidOperationException("M readable font is missing.");
            dark=Surface("ReadableSignDark",new Color32(29,39,44,255));
            paper=Surface("ReadableSignPaper",new Color32(255,246,219,255));
            gold=Surface("ReadableSignGold",new Color32(210,173,95,255));
            lightText=TextMaterial("ReadableSignLightText");inkText=TextMaterial("ReadableSignInkText");
            var go=new GameObject("ReadableSignage");root=go.transform;
            try
            {
                AddFloor(0);AddFloor(ElevatorSportsCentreMap.ThirdFloorHeight);
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,Prefab);
                if(!prefab)throw new InvalidOperationException("Could not save M signage prefab.");
                Object.DestroyImmediate(go);
                foreach(string path in Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    var map=Object.FindFirstObjectByType<ElevatorSportsCentreMap>();
                    if(!map||!map.geometry)throw new InvalidOperationException("Sports centre map missing: "+path);
                    var old=map.geometry.Find("ReadableSignage");if(old)Object.DestroyImmediate(old.gameObject);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,map.geometry);
                    instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;instance.transform.localScale=Vector3.one;
                    var view=Object.FindFirstObjectByType<ElevatorView>();
                    view.spatial.storybook.ApplyStatic();
                    EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save "+path);
                }
            }
            finally
            {
                if(go)Object.DestroyImmediate(go);
                if(SceneManager.GetActiveScene().path!=original)EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
            }
        }

        static Material Surface(string name,Color color)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Cull",2);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        static Material TextMaterial(string name)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(font.material);AssetDatabase.CreateAsset(m,path);}
            m.shader=Shader.Find("TextMeshPro/Distance Field");m.SetFloat("_ZTestMode",4);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        static Transform Plate(string name,Vector3 position,float yaw,Vector2 size,Material material,bool round=false)
        {
            var go=new GameObject(name);var t=go.transform;t.SetParent(root,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);
            var face=GameObject.CreatePrimitive(round?PrimitiveType.Cylinder:PrimitiveType.Quad);face.name="ReadingSurface";Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(t,false);
            face.transform.localRotation=round?Quaternion.Euler(90,0,0):Quaternion.identity;
            face.transform.localScale=round?new Vector3(size.x,.0004f,size.y):new Vector3(size.x,size.y,1);
            var r=face.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            return t;
        }
        static TMP_Text Label(Transform parent,string value,Vector2 size,float point,Vector2 offset,bool ink=false)
        {
            var go=new GameObject("Label");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(offset.x,offset.y,-.0015f);
            var text=go.AddComponent<TextMeshPro>();text.font=font;text.fontSharedMaterial=ink?inkText:lightText;
            ElevatorTypography.Apply(text,point);text.color=ink?new Color(.28f,.19f,.14f):new Color(1,.975f,.89f);
            text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.text=value;
            text.rectTransform.sizeDelta=size;text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return text;
        }
        static void AddFloor(float y)
        {
            string floor=y>0?"3F_":"1F_";
            for(int i=1;i<=12;i++)
            {
                var p=Plate(floor+"FrontFloor"+i,new Vector3(-2.87f-((i-1)%3)*.14f,y+1.32f+((i-1)/3)*.17f,3.707f),180,new Vector2(.086f,.086f),dark,true);
                Label(p,i.ToString(),new Vector2(.092f,.09f),.54f,Vector2.zero);
                p=Plate(floor+"SideFloor"+i,new Vector3(-.426f,y+1.25f+((i-1)/4)*.15f,5.05f-((i-1)%4)*.12f),90,new Vector2(.078f,.078f),dark,true);
                Label(p,i.ToString(),new Vector2(.082f,.085f),.50f,Vector2.zero);
            }
            var front=Plate(floor+"FrontEmergency",new Vector3(-3.01f,y+2.15f,3.708f),180,new Vector2(.104f,.104f),gold,true);
            Label(front,"비상",new Vector2(.10f,.075f),.43f,Vector2.zero,true);
            var side=Plate(floor+"SideEmergency",new Vector3(-.427f,y+1.28f,4.36f),90,new Vector2(.092f,.092f),gold,true);
            Label(side,"비상",new Vector2(.09f,.07f),.38f,Vector2.zero,true);
            var p1=Plate(floor+"FrontEmergencyGuide",new Vector3(-3.01f,y+2.286f,3.705f),180,new Vector2(.34f,.09f),dark);
            Label(p1,"비상 호출",new Vector2(.33f,.085f),.53f,Vector2.zero);
            p1=Plate(floor+"SideEmergencyGuide",new Vector3(-.423f,y+1.425f,4.36f),90,new Vector2(.31f,.085f),dark);
            Label(p1,"비상 호출",new Vector2(.30f,.08f),.48f,Vector2.zero);
            for(int i=0;i<2;i++)
            {
                p1=Plate(floor+"DoorGuide"+i,new Vector3(-.424f,y+1.665f,4.47f-i*.22f),90,new Vector2(.13f,.082f),dark);
                Label(p1,i==0?"열림":"닫힘",new Vector2(.125f,.080f),.43f,Vector2.zero);
                p1=Plate(floor+"CallArrow"+i,new Vector3(-.12f,y+1.4f-i*.22f,2.985f),0,new Vector2(.076f,.076f),dark,true);
                Label(p1,i==0?"▲":"▼",new Vector2(.076f,.08f),.53f,Vector2.zero);
            }
            p1=Plate(floor+"LiftIdentity",new Vector3(-3.01f,y+1.993f,3.718f),180,new Vector2(.49f,.178f),paper);
            Label(p1,"한빛 생활체육센터",new Vector2(.47f,.06f),.38f,new Vector2(0,.052f),true);
            Label(p1,"본관 1호기 · HB-EL-01",new Vector2(.47f,.06f),.34f,Vector2.zero,true);
            Label(p1,"1000 kg / 13명",new Vector2(.47f,.055f),.29f,new Vector2(0,-.051f),true);
            p1=Plate(floor+"LobbyDirectory",new Vector3(-4.99f,y+1.15f,3.072f),0,new Vector2(1.24f,.54f),dark);
            Label(p1,"층별 안내",new Vector2(1.12f,.105f),.68f,new Vector2(0,.175f));
            Label(p1,"1F  로비 · 주출입구    2F  준비 중\n3F  농구장 · 탈의실    4F  운동교실\n5F  체력단련실    6–12F  문화 · 운영",new Vector2(1.12f,.34f),.48f,new Vector2(0,-.055f));
            p1=Plate(floor+"CabinDirectory",new Vector3(-3.18f,y+1.88f,5.2f),270,new Vector2(.47f,1.04f),dark);
            Label(p1,"층별 시설 안내",new Vector2(.45f,.10f),.48f,new Vector2(0,.43f));
            string[] lines={"6–12F  문화 · 운영","5F  체력단련실","4F  운동교실","3F  농구장 · 탈의실","2F  준비 중","1F  로비 · 주출입구"};
            for(int i=0;i<lines.Length;i++)Label(p1,lines[i],new Vector2(.44f,.08f),.32f,new Vector2(0,.30f-i*.15f));
        }
    }
}
#endif