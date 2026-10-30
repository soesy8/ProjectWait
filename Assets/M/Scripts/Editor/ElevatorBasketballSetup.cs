#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    // Updates the existing M scene in place. No shared model, package or project settings writes.
    public static class ElevatorBasketballSetup
    {
        const string Materials="Assets/M/Arts/Materials/Elevator/";
        static Material cream,mint,teal,ink,paper,floor,orange,skin,leaf,sky;
        static TMP_FontAsset font;
        [MenuItem("Wait/M/Apply Basketball Story")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ElevatorPrototypeBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved M elevator scene in Edit Mode first. Preserve unsaved changes.");
            var l=Object.FindFirstObjectByType<ElevatorLesson>();
            if(l.presentation)throw new InvalidOperationException("Basketball story is already installed; edit the existing presentation in place.");
            font=l.view.font;
            cream=Mat("Cream",new Color32(240,223,192,255),.25f);
            mint=Mat("Mint",new Color32(157,201,183,255),.35f);
            teal=Mat("Teal",new Color32(71,142,131,255),.6f);
            ink=Mat("Ink",new Color32(73,51,39,255),.5f);
            paper=Mat("Paper",new Color32(255,246,219,255),.35f);
            floor=Mat("Floor",new Color32(233,214,175,255),.25f);
            orange=Mat("BasketballOrange",new Color32(232,121,39,255),1.8f);
            skin=Mat("RescuerSkin",new Color32(255,192,138,255),1.8f);
            leaf=Mat("LobbyLeaf",new Color32(154,191,103,255),.6f);
            sky=Mat("LobbySky",new Color32(135,206,234,255),0);
            var p=l.gameObject.AddComponent<ElevatorStoryPresentation>();p.lesson=l;l.presentation=p;
            var cabin=l.transform.Find("Cabin");
            // Keep the original observation object and its serialized XR references.
            foreach(string name in new[]{"FrontWall","FrontMint","DoorFrame","DoorSeam"})
            {
                var t=cabin.Find(name);if(t)t.gameObject.SetActive(false);
            }
            var oldDoor=cabin.Find("ClosedDoors");oldDoor.GetComponent<Renderer>().enabled=false;
            p.closedDoorCollider=oldDoor.GetComponent<Collider>();
            Box("DoorSideLeft",cabin,new Vector3(-1.3f,1.42f,1.75f),new Vector3(.92f,2.85f,.16f),cream);
            Box("DoorSideRight",cabin,new Vector3(1.3f,1.42f,1.75f),new Vector3(.92f,2.85f,.16f),cream);
            Box("DoorUpperWall",cabin,new Vector3(0,2.59f,1.75f),new Vector3(1.7f,.52f,.16f),cream);
            Box("DoorMintLeft",cabin,new Vector3(-1.3f,.48f,1.65f),new Vector3(.92f,.96f,.04f),mint);
            Box("DoorMintRight",cabin,new Vector3(1.3f,.48f,1.65f),new Vector3(.92f,.96f,.04f),mint);
            DoorFrame(cabin,1.59f);
            p.leftDoor=Box("LeftDoor",cabin,new Vector3(-.41f,1.1f,1.5f),new Vector3(.81f,2.18f,.08f),cream).transform;
            p.rightDoor=Box("RightDoor",cabin,new Vector3(.41f,1.1f,1.5f),new Vector3(.81f,2.18f,.08f),cream).transform;
            for(int i=0;i<3;i++)
                Primitive("CabinCeilingLight"+i,cabin,new Vector3(-1.1f+i*1.1f,2.74f,.45f),new Vector3(.28f,.025f,.28f),paper,PrimitiveType.Cylinder);
            Tiles(cabin,3.4f,4.5f,0);
            foreach(var animator in l.friend.GetComponentsInChildren<Animator>())animator.enabled=false;
            var actor=l.friend.GetChild(0);
            // Existing shared model remains a read-only prefab instance.
            l.friend.position=new Vector3(-.72f,0,.45f);
            p.friendBall=Ball("FriendBasketball",l.friend,new Vector3(0,.76f,-.34f));
            p.playerBall=Ball("HeldBasketball",l.transform,Vector3.zero);p.playerBall.gameObject.SetActive(false);
            p.lobby=new GameObject("BasketballLobby");p.lobby.transform.SetParent(l.transform,false);
            BuildLobby(p.lobby.transform,true);
            p.exitHall=new GameObject("RescueHall");p.exitHall.transform.SetParent(l.transform,false);
            BuildHall(p.exitHall.transform);p.exitHall.SetActive(false);
            p.rescuer=BuildRescuer(l.transform);p.rescuer.SetActive(false);
            var s=l.settings;
            s.friendReplySeconds=1.2f;s.afterJumpSeconds=1.1f;s.rescueWaitSeconds=12;s.rescueSubtitleSeconds=4;
            s.building="햇살아파트";s.proposal="이렇게 뛰어서!";
            // Preserve the settings asset GUID and all timing values not changed by this scenario.
            EditorUtility.SetDirty(s);AssetDatabase.SaveAssetIfDirty(s);
            cabin.Find("IdentityText").GetComponent<TMP_Text>().text=s.building+"\n"+s.liftId+" · 가상 정보";
            cabin.Find("SafetyRules").GetComponent<TMP_Text>().text=s.safetyNotice;
            l.view.PrepareCompactPresentation();
            string[] stages={"로비","공 맡기기","슛 재현","정지","통화","함께 대기","구조 안내"};
            for(int i=0;i<stages.Length;i++)l.view.developerPanel.Find("Stage"+i).GetComponentInChildren<TMP_Text>().text=stages[i];
            l.view.subtitle.text="01  농구를 마치고 친구 집으로";
            l.view.dialogueText.text=s.lobbyDialogue;
            // Save the initial lobby composition for edit-mode inspection; Awake restores the runtime UI.
            cabin.gameObject.SetActive(false);p.lobby.SetActive(true);
            EditorUtility.SetDirty(l);EditorUtility.SetDirty(p);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save M scene.");
            Refine();
        }
        [MenuItem("Wait/M/Refine Basketball Presentation")]
        public static void Refine()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath||scene.isDirty)
                throw new InvalidOperationException("Open the saved M scene in Edit Mode first.");
            var l=Object.FindFirstObjectByType<ElevatorLesson>();var p=l.presentation;
            font=l.view.font;
            cream=AssetDatabase.LoadAssetAtPath<Material>(Materials+"Cream.mat");
            ink=AssetDatabase.LoadAssetAtPath<Material>(Materials+"Ink.mat");
            skin=AssetDatabase.LoadAssetAtPath<Material>(Materials+"RescuerSkin.mat");
            paper=AssetDatabase.LoadAssetAtPath<Material>(Materials+"Paper.mat");
            p.friendBall.localPosition=new Vector3(0,.71f,-.24f);p.playerBall.localScale=Vector3.one*.85f;

            var cabin=l.transform.Find("Cabin");
            foreach(var slot in cabin.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="SpeakerSlot"))
            {
                var blocker=slot.GetComponent<Collider>();if(blocker)Object.DestroyImmediate(blocker);
            }
            var button=cabin.GetComponentsInChildren<Transform>(true).First(t=>t.name=="BellButton");
            button.SetParent(cabin,true);button.position=new Vector3(1.14f,1.31f,1.37f);button.localScale=new Vector3(.18f,.18f,.07f);
            Object.DestroyImmediate(button.GetComponent<Collider>());
            cabin.Find("BellSymbol").position=new Vector3(1.14f,1.31f,1.322f);
            var r=p.rescuer.transform;
            if(!r.Find("Nose"))
            {
                Primitive("Nose",r,new Vector3(0,1.46f,-.231f),new Vector3(.065f,.065f,.065f),skin,PrimitiveType.Sphere);
                Primitive("EarLeft",r,new Vector3(-.23f,1.48f,0),new Vector3(.10f,.15f,.12f),skin,PrimitiveType.Sphere);
                Primitive("EarRight",r,new Vector3(.23f,1.48f,0),new Vector3(.10f,.15f,.12f),skin,PrimitiveType.Sphere);
                Primitive("SmileTeeth",r,new Vector3(0,1.39f,-.233f),new Vector3(.075f,.017f,.014f),paper,PrimitiveType.Sphere);
                var sun=Mat("LobbySunlight",new Color32(255,240,197,255),0);
                for(int i=0;i<3;i++)
                {
                    var patch=Box("SunlightPatch"+i,p.lobby.transform,new Vector3(-2.4f+i*1.65f,.012f,.3f),new Vector3(.84f,.008f,3.8f),sun);
                    patch.transform.localRotation=Quaternion.Euler(0,-28,0);
                }
            }
            l.view.PrepareCompactPresentation();
            l.view.dialoguePanel.sizeDelta=new Vector2(530,150);l.view.dialogueText.rectTransform.sizeDelta=new Vector2(420,92);
            l.view.dialogueText.text=l.settings.lobbyDialogue;l.view.dialoguePanel.gameObject.SetActive(true);
            l.view.actionPanel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save M scene.");
        }
        static void DoorFrame(Transform parent,float z)
        {
            Box("FrameLeft",parent,new Vector3(-.9f,1.15f,z),new Vector3(.15f,2.4f,.16f),teal);
            Box("FrameRight",parent,new Vector3(.9f,1.15f,z),new Vector3(.15f,2.4f,.16f),teal);
            Box("FrameTop",parent,new Vector3(0,2.34f,z),new Vector3(1.95f,.18f,.16f),teal);
            Box("Threshold",parent,new Vector3(0,.015f,z-.05f),new Vector3(1.8f,.03f,.28f),paper);
        }
        static void BuildLobby(Transform root,bool court)
        {
            Box("LobbyFloor",root,new Vector3(0,-.09f,0),new Vector3(7,.16f,8),floor);
            Box("LobbyWall",root,new Vector3(0,1.5f,3.4f),new Vector3(7,3,.12f),cream);
            Box("LobbyMint",root,new Vector3(0,.48f,3.3f),new Vector3(7,.96f,.06f),mint);
            Box("LobbyCeiling",root,new Vector3(0,3.05f,.7f),new Vector3(7,.15f,7),cream);
            Box("LiftBacking",root,new Vector3(0,1.15f,2.9f),new Vector3(1.75f,2.3f,.12f),mint);
            Box("LiftFloor",root,new Vector3(0,.025f,2.2f),new Vector3(1.75f,.05f,1.3f),paper);
            Box("LiftSideLeft",root,new Vector3(-.88f,1.1f,2.3f),new Vector3(.09f,2.2f,1.3f),cream);
            Box("LiftSideRight",root,new Vector3(.88f,1.1f,2.3f),new Vector3(.09f,2.2f,1.3f),cream);
            DoorFrame(root,1.6f);
            Box("LiftRail",root,new Vector3(0,.9f,2.8f),new Vector3(1.55f,.055f,.07f),teal);
            var panel=Box("CallPanel",root,new Vector3(1.24f,1.25f,1.65f),new Vector3(.27f,.62f,.10f),ink);
            Text("CallArrow",root,new Vector3(1.24f,1.34f,1.57f),new Vector2(.2f,.2f),"▲",.9f,orange.color);
            Text("LobbyTitle",root,new Vector3(0,2.7f,1.6f),new Vector2(2.9f,.3f),"햇살아파트  ·  1층",.8f,ink.color);
            // Broad sky panes and simple outdoor silhouettes evoke the reference playground.
            Box("SkyWindow",root,new Vector3(-2.25f,1.65f,2.9f),new Vector3(2.3f,2.15f,.06f),sky);
            for(int i=0;i<3;i++)Box("WindowMullion"+i,root,new Vector3(-3.4f+i*1.15f,1.65f,2.78f),new Vector3(.06f,2.25f,.07f),teal);
            Box("WindowCrossbar",root,new Vector3(-2.25f,1.6f,2.78f),new Vector3(2.3f,.06f,.07f),teal);
            for(int i=0;i<4;i++)Primitive("GardenTree"+i,root,new Vector3(-3.2f+i*.5f,.76f,2.64f),new Vector3(.7f,1.0f,.15f),leaf,PrimitiveType.Sphere);
            Box("HoopPost",root,new Vector3(-2.1f,.65f,2.45f),new Vector3(.05f,1.2f,.07f),teal);
            Box("Backboard",root,new Vector3(-2.1f,1.25f,2.42f),new Vector3(.68f,.46f,.06f),paper);
            Box("HoopSquare",root,new Vector3(-2.1f,1.24f,2.38f),new Vector3(.29f,.23f,.025f),orange);
            Box("HoopInset",root,new Vector3(-2.1f,1.25f,2.36f),new Vector3(.22f,.16f,.02f),paper);
            Ring("BasketRim",root,new Vector3(-2.1f,1.07f,2.27f),.17f,orange,Quaternion.Euler(90,0,0),.015f);
            Plant(root,new Vector3(2.05f,0,1.6f));
            Plant(root,new Vector3(-2.95f,0,.9f));
            Tiles(root,6.8f,7.8f,0);
            for(int i=0;i<3;i++)Primitive("WarmDownlight"+i,root,new Vector3(-2+i*2,2.95f,1),new Vector3(.35f,.025f,.35f),paper,PrimitiveType.Cylinder);
        }
        static void BuildHall(Transform root)
        {
            Box("HallFloor",root,new Vector3(0,-.06f,4.5f),new Vector3(6,.12f,5.8f),floor);
            Box("HallBack",root,new Vector3(0,1.5f,7.2f),new Vector3(6,3,.12f),cream);
            Box("HallMint",root,new Vector3(0,.5f,7.1f),new Vector3(6,1,.08f),mint);
            Box("HallWindow",root,new Vector3(-1.4f,1.8f,7.0f),new Vector3(2,1.5f,.08f),sky);
            Box("HallWindowBar",root,new Vector3(-1.4f,1.8f,6.9f),new Vector3(.07f,1.5f,.08f),teal);
            Plant(root,new Vector3(1.8f,0,5));
        }
        static GameObject BuildRescuer(Transform parent)
        {
            var g=new GameObject("FriendlyRescuer");g.transform.SetParent(parent,false);
            var t=g.transform;
            Primitive("LeftShoe",t,new Vector3(-.14f,.09f,-.07f),new Vector3(.22f,.16f,.34f),ink,PrimitiveType.Sphere);
            Primitive("RightShoe",t,new Vector3(.14f,.09f,-.07f),new Vector3(.22f,.16f,.34f),ink,PrimitiveType.Sphere);
            Primitive("LeftTrouser",t,new Vector3(-.14f,.39f,0),new Vector3(.21f,.36f,.23f),teal,PrimitiveType.Capsule);
            Primitive("RightTrouser",t,new Vector3(.14f,.39f,0),new Vector3(.21f,.36f,.23f),teal,PrimitiveType.Capsule);
            Primitive("Jacket",t,new Vector3(0,.92f,0),new Vector3(.57f,.64f,.35f),teal,PrimitiveType.Sphere);
            Primitive("SafetyVest",t,new Vector3(0,.93f,-.07f),new Vector3(.50f,.56f,.32f),orange,PrimitiveType.Sphere);
            for(int i=0;i<2;i++)Box("ReflectiveStripe"+i,t,new Vector3(0,.84f+i*.16f,-.232f),new Vector3(.48f,.055f,.02f),paper);
            Box("VestZip",t,new Vector3(0,.95f,-.247f),new Vector3(.026f,.47f,.02f),ink);
            Primitive("LeftArm",t,new Vector3(-.36f,.92f,-.07f),new Vector3(.19f,.33f,.19f),teal,PrimitiveType.Capsule);
            Primitive("RightArm",t,new Vector3(.36f,.92f,-.07f),new Vector3(.19f,.33f,.19f),teal,PrimitiveType.Capsule);
            Primitive("LeftHand",t,new Vector3(-.36f,.61f,-.07f),new Vector3(.18f,.20f,.17f),skin,PrimitiveType.Sphere);
            Primitive("RightHand",t,new Vector3(.36f,.61f,-.07f),new Vector3(.18f,.20f,.17f),skin,PrimitiveType.Sphere);
            Primitive("Head",t,new Vector3(0,1.49f,-.015f),new Vector3(.46f,.48f,.42f),skin,PrimitiveType.Sphere);
            Primitive("Cap",t,new Vector3(0,1.69f,0),new Vector3(.5f,.24f,.45f),teal,PrimitiveType.Sphere);
            Primitive("CapBrim",t,new Vector3(0,1.64f,-.22f),new Vector3(.50f,.06f,.30f),teal,PrimitiveType.Sphere);
            for(int i=0;i<2;i++)Primitive("Eye"+i,t,new Vector3(-.085f+i*.17f,1.51f,-.216f),new Vector3(.044f,.067f,.025f),ink,PrimitiveType.Sphere);
            Primitive("Smile",t,new Vector3(0,1.38f,-.218f),new Vector3(.12f,.045f,.02f),ink,PrimitiveType.Sphere);
            Text("RescuerBadge",t,new Vector3(0,1.04f,-.253f),new Vector2(.34f,.12f),"구조",.45f,ink.color);
            return g;
        }
        static void Plant(Transform parent,Vector3 pos)
        {
            Primitive("PlantPot",parent,pos+Vector3.up*.23f,new Vector3(.47f,.24f,.47f),paper,PrimitiveType.Cylinder);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                var l=Primitive("BroadLeaf",parent,pos+new Vector3(Mathf.Cos(a)*.18f,.65f+i*.055f,Mathf.Sin(a)*.16f),new Vector3(.27f,.63f,.12f),i%2==0?leaf:teal,PrimitiveType.Sphere);
                l.transform.localRotation=Quaternion.Euler(20,60*i,30);
            }
        }
        static void Tiles(Transform parent,float width,float depth,float z)
        {
            for(float x=-width*.5f;x<width*.5f;x+=.72f)Box("TileJoint",parent,new Vector3(x,.003f,z),new Vector3(.009f,.006f,depth),cream);
            for(float y=-depth*.5f;y<depth*.5f;y+=.72f)Box("TileJoint",parent,new Vector3(0,.004f,y+z),new Vector3(width,.006f,.009f),cream);
        }
        static Transform Ball(string name,Transform parent,Vector3 pos)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);root.localPosition=pos;
            Primitive("OrangeBall",root,Vector3.zero,Vector3.one*.25f,orange,PrimitiveType.Sphere);
            Ring("SeamA",root,Vector3.zero,.126f,ink,Quaternion.identity,.004f);
            Ring("SeamB",root,Vector3.zero,.126f,ink,Quaternion.Euler(90,0,0),.004f);
            Ring("SeamC",root,Vector3.zero,.126f,ink,Quaternion.Euler(0,90,0),.004f);
            return root;
        }
        static void Ring(string name,Transform parent,Vector3 pos,float radius,Material material,Quaternion rotation,float width)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.transform.localPosition=pos;line.transform.localRotation=rotation;
            line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=width;line.sharedMaterial=material;line.numCornerVertices=3;
            for(int i=0;i<64;i++){float a=i*2*Mathf.PI/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
        }
        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material material)=>Primitive(name,parent,pos,size,material,PrimitiveType.Cube);
        static GameObject Primitive(string name,Transform parent,Vector3 pos,Vector3 size,Material material,PrimitiveType shape)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static void Text(string name,Transform parent,Vector3 pos,Vector2 size,string value,float point,Color color)
        {
            var text=new GameObject(name).AddComponent<TextMeshPro>();text.transform.SetParent(parent,false);text.transform.localPosition=pos;
            text.font=font;text.fontSize=point;text.fontStyle=FontStyles.Bold;text.color=color;text.text=value;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;
        }
        static Material Mat(string name,Color color,float outline)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Materials+name+".mat");
            if(m)return m;
            m=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/ElevatorStorybookToon.shader"));
            m.SetColor("_BaseColor",color);m.SetColor("_OutlineColor",new Color32(73,51,39,255));m.SetColor("_ShadeColor",new Color32(189,200,195,255));
            m.SetFloat("_MidTone",.9f);m.SetFloat("_ShadowThreshold",.02f);m.SetFloat("_LightThreshold",.62f);m.SetFloat("_BandSoftness",.012f);m.SetFloat("_OutlineWidth",outline);m.enableInstancing=true;
            AssetDatabase.CreateAsset(m,Materials+name+".mat");return m;
        }
    }
}
#endif
