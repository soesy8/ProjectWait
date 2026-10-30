#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;
namespace ProjectWait.M.Editor
{
 public static class ElevatorSpaceSetup
 {
  const string Root="Assets/M/", Mats=Root+"Arts/Materials/Elevator/";
  static ElevatorLesson l;static ElevatorStoryPresentation p;static TMP_FontAsset font;
  static Material cream,mint,teal,paper,ink,floor,orange,leaf,sky;
  static Mesh rounded;
  static readonly List<Mesh> generated=new List<Mesh>();
  static void ConnectSpatialUI()
  {
   if(!l.view.spatial)return;
   var spatial=l.view.spatial;if(!spatial.intercomAnchor)spatial.intercomAnchor=l.GetComponentsInChildren<ElevatorObservation>(true).First(o=>o.clue==ElevatorClue.Intercom).transform;if(!spatial.rescuerAnchor)spatial.rescuerAnchor=p.rescuer.transform;
  }
  public static string RepairSpaceConnections()
  {
   var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open the M scene in Edit Mode.");
   l=Object.FindFirstObjectByType<ElevatorLesson>();p=l.presentation;ConnectSpatialUI();
   if(l.view.spatial&&l.view.spatial.notebook&&!l.view.spatial.notebook.GetComponent<MeshFilter>())ElevatorPaletteOptimizer.CombineStatic(l.view.spatial.notebook,"NotebookPalette");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Spatial UI connected to current cabin, notebook geometry consolidated.";
  }
  public static string RefineArt()
  {
   var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Open the saved M scene in Edit Mode.");
   l=Object.FindFirstObjectByType<ElevatorLesson>();p=l.presentation;if(!p.space)throw new Exception("Install the spatial upgrade first.");
   font=l.view.font;cream=Mat("Cream");mint=Mat("Mint");teal=Mat("Teal");paper=Mat("Paper");ink=Mat("Ink");floor=Mat("Floor");orange=Mat("BasketballOrange");leaf=Mat("LobbyLeaf");sky=Mat("LobbySky");rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Arts/Models/Elevator/IntercomPanelRounded.asset");
   var cabin=l.transform.Find("Cabin");Clear(cabin);Clear(p.lobby.transform);BuildCabin(cabin);BuildLobby(p.lobby.transform);
   var overlay=p.space.overlay;Clear(overlay.transform);const string path=Mats+"CinematicCaption.mat";var captionMat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!captionMat){captionMat=new Material(font.material){name="CinematicCaption"};AssetDatabase.CreateAsset(captionMat,path);}captionMat.renderQueue=4100;captionMat.SetFloat("unity_GUIZTestMode",8);EditorUtility.SetDirty(captionMat);AssetDatabase.SaveAssetIfDirty(captionMat);overlay.captionMaterial=captionMat;overlay.Build();
   l.cabinLight.transform.rotation=Quaternion.Euler(35,150,0);ConnectSpatialUI();RefreshGlyphs();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Door radii, recessed controls, identity plaque, cabin trim, and cinematic subtitles refined.";
  }
  [MenuItem("Wait/M/Upgrade Elevator Space")]
  public static object Apply()
  {
   var scene=SceneManager.GetActiveScene();
   if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ElevatorPrototypeBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Save the M scene and stop Play Mode before applying the spatial upgrade.");
   l=Object.FindFirstObjectByType<ElevatorLesson>();p=l.presentation;
   if(p.space)throw new InvalidOperationException("The spatial upgrade already exists. Refine the current objects instead.");
   var before=l.GetComponentsInChildren<Renderer>(true);int slotsBefore=before.Sum(r=>r.sharedMaterials.Length);
   font=l.view.font;cream=Mat("Cream");mint=Mat("Mint");teal=Mat("Teal");paper=Mat("Paper");ink=Mat("Ink");floor=Mat("Floor");orange=Mat("BasketballOrange");leaf=Mat("LobbyLeaf");sky=Mat("LobbySky");
   rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Arts/Models/Elevator/IntercomPanelRounded.asset");
   var characters=ElevatorPaletteOptimizer.OptimizeCharacters(l.transform);
   var cabin=l.transform.Find("Cabin");Clear(cabin);Clear(p.lobby.transform);Object.DestroyImmediate(p.exitHall);p.exitHall=null;
   var space=l.gameObject.AddComponent<ElevatorSpacePresentation>();p.space=space;space.lesson=l;
   var controls=Object.FindFirstObjectByType<ElevatorDesktopControls>();space.playerRig=controls.xrOrigin;
   space.doorZ=2.88f;space.doorY=1.16f;
   BuildCabin(cabin);BuildLobby(p.lobby.transform);
   ElevatorPaletteOptimizer.CombineStatic(p.rescuer.transform,"RescuerAccessories");
   UpgradeBall(p.friendBall,"FriendBasketballPalette");UpgradeBall(p.playerBall,"HeldBasketballPalette");
   var overlayObject=new GameObject("CinematicLetterbox",typeof(RectTransform),typeof(Canvas));overlayObject.transform.SetParent(l.transform,false);
   var overlay=overlayObject.AddComponent<ElevatorCinematicOverlay>();space.overlay=overlay;overlay.lesson=l;overlay.playerCamera=l.playerCamera;overlay.font=font;
   const string overlayPath=Mats+"CinematicOverlay.mat";var overlayMat=AssetDatabase.LoadAssetAtPath<Material>(overlayPath);
   if(!overlayMat){overlayMat=new Material(Shader.Find("ProjectWait/M/Cinematic Overlay")){name="CinematicOverlay"};AssetDatabase.CreateAsset(overlayMat,overlayPath);}overlay.overlayMaterial=overlayMat;overlay.Build();
   l.settings.boardingSeconds=6.2f;l.settings.exitSeconds=4.6f;EditorUtility.SetDirty(l.settings);AssetDatabase.SaveAssetIfDirty(l.settings);
   l.playerCamera.fieldOfView=68;space.playerRig.position=space.lobbyPosition;space.playerRig.rotation=Quaternion.identity;
   l.playerCamera.transform.position=space.lobbyPosition+Vector3.up*controls.eyeHeight;l.playerCamera.transform.rotation=Quaternion.Euler(0,180,0);
   l.friend.localPosition=space.friendOutside;l.friend.localRotation=Quaternion.Euler(0,180,0);
   p.friendShadow.position=new Vector3(space.friendOutside.x,.012f,space.friendOutside.z);
   p.rescuer.SetActive(false);p.friendBall.gameObject.SetActive(true);p.playerBall.gameObject.SetActive(false);cabin.gameObject.SetActive(true);p.lobby.SetActive(true);
   RefreshGlyphs();l.view.Reanchor(l.playerCamera.transform.position);l.view.ApplyCompactLayout();
   foreach(var mesh in generated)Object.DestroyImmediate(mesh);generated.Clear();
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("M scene save failed.");
   var after=l.GetComponentsInChildren<Renderer>(true);return new{characters,renderersBefore=before.Length,renderersAfter=after.Length,materialSlotsBefore=slotsBefore,materialSlotsAfter=after.Sum(r=>r.sharedMaterials.Length),uniqueMaterialsAfter=after.SelectMany(r=>r.sharedMaterials).Distinct().Count(),scene=scene.path};
  }
  static void BuildCabin(Transform cabin)
  {
   var art=Group("CabinArchitecture",cabin);
   Box("Floor",art,new Vector3(0,-.08f,.1f),new Vector3(5.12f,.16f,5.4f),floor);
   Box("Ceiling",art,new Vector3(0,2.98f,.1f),new Vector3(5.12f,.12f,5.4f),cream);
   for(int side=-1;side<=1;side+=2)
   {
    Box("SideWall",art,new Vector3(side*2.55f,1.46f,.1f),new Vector3(.12f,2.92f,5.4f),cream);
    Box("MintWainscot",art,new Vector3(side*2.48f,.50f,.1f),new Vector3(.035f,1,5.32f),mint);
    Box("WainscotTrim",art,new Vector3(side*2.454f,1.04f,.1f),new Vector3(.025f,.055f,5.32f),teal);
    Box("Skirting",art,new Vector3(side*2.45f,.07f,.1f),new Vector3(.045f,.14f,5.32f),teal);
    Rounded("Handrail",art,new Vector3(side*2.35f,.94f,0),new Vector3(.065f,.065f,4.35f),paper);
    for(int z=-1;z<=1;z++)Box("RailMount",art,new Vector3(side*2.41f,.92f,z*1.5f),new Vector3(.13f,.08f,.09f),teal);
   }
   Box("RearWall",art,new Vector3(0,1.46f,-2.62f),new Vector3(5.12f,2.92f,.12f),cream);
   Box("RearMint",art,new Vector3(0,.50f,-2.55f),new Vector3(5,1,.035f),mint);
   Rounded("RearHandrail",art,new Vector3(0,.94f,-2.40f),new Vector3(4.65f,.065f,.065f),paper);
   // Thin inner and outer wall skins leave a genuine sliding-door pocket between them.
   RectWall(art,"InnerLeftSkin",-2.5f,-.96f,0,2.92f,2.79f,.08f,cream);
   RectWall(art,"RightInnerJamb",.96f,1.18f,0,2.92f,2.79f,.08f,cream);
   RectWall(art,"RightOuterJamb",2.20f,2.5f,0,2.92f,2.79f,.08f,cream);
   RectWall(art,"RightPanelFoot",1.18f,2.20f,0,.45f,2.79f,.08f,mint);
   RectWall(art,"RightPanelHead",1.18f,2.20f,2.35f,2.92f,2.79f,.08f,cream);
   RectWall(art,"Lintel",-.96f,.96f,2.37f,2.92f,2.95f,.40f,cream);
   RectWall(art,"LeftMint",-2.5f,-.96f,0,1.0f,2.742f,.012f,mint);
   RectWall(art,"RightMintInner",.96f,1.18f,0,1.0f,2.742f,.012f,mint);
   RectWall(art,"RightMintOuter",2.20f,2.5f,0,1.0f,2.742f,.012f,mint);
   // Outside skin: the call button lives in a cutout, below the wall's outer surface.
   RectWall(art,"OuterLeftEdge",-2.5f,-1.85f,0,2.92f,3.10f,.10f,cream);
   RectWall(art,"OuterLeftJamb",-1.48f,-.96f,0,2.92f,3.10f,.10f,cream);
   RectWall(art,"CallPocketBelow",-1.85f,-1.48f,0,1.05f,3.10f,.10f,cream);
   RectWall(art,"CallPocketAbove",-1.85f,-1.48f,1.80f,2.92f,3.10f,.10f,cream);
   RectWall(art,"OuterRightSkin",.96f,2.5f,0,2.92f,3.10f,.10f,cream);
   foreach(float z in new[]{2.80f,3.09f})
   {
    for(int side=-1;side<=1;side+=2)Rounded("InsetDoorJamb",art,new Vector3(side*.90f,1.19f,z),new Vector3(.16f,2.38f,.10f),teal);
    Rounded("InsetDoorHeader",art,new Vector3(0,2.40f,z),new Vector3(1.96f,.16f,.10f),teal);
   }
   Box("Threshold",art,new Vector3(0,.005f,2.96f),new Vector3(1.78f,.018f,.50f),paper);
   for(int i=0;i<2;i++)Box("DoorGuideTrack",art,new Vector3(0,.016f,2.88f+i*.11f),new Vector3(1.72f,.006f,.012f),teal);
   p.leftDoor=Rounded("LeftDoor",cabin,new Vector3(-.415f,1.16f,2.88f),new Vector3(.82f,2.30f,.08f),Mat("DoorIvory")).transform;
   p.rightDoor=Rounded("RightDoor",cabin,new Vector3(.415f,1.16f,2.88f),new Vector3(.82f,2.30f,.08f),Mat("DoorIvory")).transform;
   foreach(var door in new[]{p.leftDoor,p.rightDoor}){door.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Arts/Models/Elevator/LeftDoorRounded.asset");door.localScale=new Vector3(.82f/.81f,2.30f/2.18f,1);}
   ElevatorPaletteOptimizer.OptimizeSingle(p.leftDoor.GetComponent<Renderer>(),"LeftDoorPalette");ElevatorPaletteOptimizer.OptimizeSingle(p.rightDoor.GetComponent<Renderer>(),"RightDoorPalette");
   p.closedDoorCollider=Observe("DoorInteraction",cabin,new Vector3(0,1.16f,2.82f),new Vector3(1.65f,2.27f,.035f),ElevatorClue.Door).GetComponent<Collider>();
   Rounded("FloorDisplay",art,new Vector3(0,2.67f,2.73f),new Vector3(.96f,.28f,.035f),ink);
   l.floorLabel=Text("FloorNumber",cabin,new Vector3(0,2.67f,2.705f),new Vector2(.91f,.25f),"01",1.05f,Paper);
   Observe("FloorDisplayInteraction",cabin,new Vector3(0,2.67f,2.69f),new Vector3(.96f,.28f,.025f),ElevatorClue.Display);
   // Recessed continuous operating panel; button faces remain behind z=2.75, the wall face.
   Rounded("RecessedControlPanel",art,new Vector3(1.69f,1.40f,2.80f),new Vector3(1.0f,1.90f,.035f),teal);
   Rounded("FloorButtonArea",art,new Vector3(1.69f,1.58f,2.779f),new Vector3(.83f,1.05f,.008f),paper);
   for(int row=0;row<3;row++)for(int col=0;col<2;col++)
   {
    float x=1.49f+col*.40f,y=1.90f-row*.25f;
    Primitive("FloorButton",art,new Vector3(x,y,2.771f),new Vector3(.17f,.17f,.018f),mint,PrimitiveType.Sphere);
    Text("FloorDigit",cabin,new Vector3(x,y,2.756f),new Vector2(.15f,.16f),(5-row*2+col).ToString(),.84f,Ink);
   }
   for(int i=0;i<2;i++){float x=1.49f+i*.40f;Primitive("DoorControlButton",art,new Vector3(x,1.17f,2.771f),new Vector3(.17f,.17f,.018f),mint,PrimitiveType.Sphere);Text("DoorControlSymbol",cabin,new Vector3(x,1.17f,2.756f),new Vector2(.16f,.16f),i==0?"< >":"> <",.64f,Ink);}
   Text("DoorButtonLabel",cabin,new Vector3(1.69f,1.065f,2.755f),new Vector2(.76f,.10f),"열림          닫힘",.34f,Ink);
   Observe("FloorButtons",cabin,new Vector3(1.69f,1.60f,2.755f),new Vector3(.86f,1.04f,.015f),ElevatorClue.FloorButtons);
   Text("IntercomTitle",cabin,new Vector3(1.69f,.998f,2.753f),new Vector2(.84f,.14f),"비상통화",.65f,Paper);
   p.intercomButton=Primitive("IntercomBell",cabin,new Vector3(1.48f,.78f,2.77f),new Vector3(.21f,.21f,.020f),orange,PrimitiveType.Sphere).transform;
   Text("BellIcon",cabin,new Vector3(1.48f,.78f,2.752f),new Vector2(.20f,.20f),"☎",1.04f,Ink);
   for(int row=0;row<3;row++)for(int col=0;col<4;col++)Primitive("SpeakerDot",art,new Vector3(1.78f+col*.047f,.84f-row*.055f,2.773f),new Vector3(.022f,.022f,.008f),ink,PrimitiveType.Sphere);
   Observe("EmergencyIntercom",cabin,new Vector3(1.69f,.82f,2.749f),new Vector3(.87f,.41f,.012f),ElevatorClue.Intercom);
   ElevatorPaletteOptimizer.OptimizeSingle(p.intercomButton.GetComponent<Renderer>(),"IntercomBellPalette");
   Rounded("LiftIdentityPlate",art,new Vector3(1.69f,2.245f,2.779f),new Vector3(.91f,.20f,.008f),paper);
   Text("LiftIdentityText",cabin,new Vector3(1.69f,2.245f,2.765f),new Vector2(.87f,.19f),"햇살아파트 · M-01\n정원 8명  |  600 kg",.48f,Ink);
   Observe("LiftIdentity",cabin,new Vector3(1.69f,2.245f,2.753f),new Vector3(.93f,.21f,.012f),ElevatorClue.Identity);
   Rounded("AdvertisementFrame",art,new Vector3(-1.72f,1.99f,2.71f),new Vector3(1.10f,1.20f,.065f),teal);
   Rounded("AdvertisementPaper",art,new Vector3(-1.72f,1.99f,2.669f),new Vector3(.99f,1.09f,.018f),paper);
   Text("AdEyebrow",cabin,new Vector3(-1.72f,2.40f,2.653f),new Vector2(.90f,.13f),"우리 동네 주말 소식",.46f,Teal);
   Text("AdTitle",cabin,new Vector3(-1.72f,2.18f,2.651f),new Vector2(.91f,.27f),"함께하는\n농구 교실",.83f,Ink);
   Primitive("AdBasketball",art,new Vector3(-1.72f,1.81f,2.648f),new Vector3(.29f,.29f,.016f),orange,PrimitiveType.Sphere);
   Box("PrintedBallSeam",art,new Vector3(-1.72f,1.81f,2.635f),new Vector3(.012f,.27f,.004f),ink);
   Text("AdDate",cabin,new Vector3(-1.72f,1.53f,2.65f),new Vector2(.91f,.14f),"토요일 10시 · 야외 운동장",.41f,Teal);
   Observe("Advertisement",cabin,new Vector3(-1.72f,1.99f,2.63f),new Vector3(1.1f,1.20f,.02f),ElevatorClue.Advertisement);
   Rounded("SafetyFlyer",art,new Vector3(-1.72f,1.04f,2.71f),new Vector3(.96f,.48f,.025f),paper);
   Box("FlyerClip",art,new Vector3(-1.72f,1.28f,2.69f),new Vector3(.16f,.045f,.015f),teal);
   Text("FlyerText",cabin,new Vector3(-1.72f,1.04f,2.688f),new Vector2(.88f,.42f),"안전하게 함께\n멈추면 비상통화\n문에서 떨어져 기다리기",.49f,Ink);
   Observe("SafetyFlyerInteraction",cabin,new Vector3(-1.72f,1.04f,2.677f),new Vector3(.97f,.49f,.016f),ElevatorClue.Notice);
   for(int x=-1;x<=1;x++)for(int z=0;z<2;z++)Primitive("CeilingLight",art,new Vector3(x*1.5f,2.88f,-1.15f+z*2.75f),new Vector3(.29f,.028f,.29f),paper,PrimitiveType.Cylinder);
   ElevatorPaletteOptimizer.CombineStatic(art,"CabinArchitecturePalette");
  }
  static void BuildLobby(Transform lobby)
  {
   var art=Group("LobbyArchitecture",lobby);
   Box("LobbyFloor",art,new Vector3(0,-.085f,6),new Vector3(10,.16f,6.4f),floor);
   Box("LobbyCeiling",art,new Vector3(0,3.14f,6),new Vector3(10,.12f,6.4f),cream);
   foreach(int side in new[]{-1,1})
   {
    Box("LobbySide",art,new Vector3(side*5,1.55f,6),new Vector3(.15f,3.1f,6.4f),cream);
    Box("LobbyFacade",art,new Vector3(side*3.75f,1.55f,3.1f),new Vector3(2.5f,3.1f,.10f),cream);
    Box("LobbyMint",art,new Vector3(side*3.75f,.48f,3.16f),new Vector3(2.5f,.96f,.02f),mint);
    Box("FacadeSkirting",art,new Vector3(side*3.75f,.08f,3.18f),new Vector3(2.5f,.16f,.03f),teal);
    Box("LobbyFloorBorder",art,new Vector3(side*2.7f,-.002f,6),new Vector3(.035f,.004f,6.0f),teal);
   }
   // Call plate and switches sit 4–9 cm inside the surrounding z=3.15 wall plane.
   Rounded("RecessedCallPlate",art,new Vector3(-1.665f,1.425f,3.057f),new Vector3(.31f,.69f,.024f),teal);
   Text("CallIndicator",lobby,new Vector3(-1.665f,1.66f,3.082f),new Vector2(.24f,.16f),"↑",.95f,Paper,true);
   foreach(float y in new[]{1.39f,1.17f})Primitive("RecessedCallButton",art,new Vector3(-1.665f,y,3.10f),new Vector3(.145f,.145f,.022f),paper,PrimitiveType.Sphere);
   Text("CallUp",lobby,new Vector3(-1.665f,1.39f,3.119f),new Vector2(.14f,.14f),"↑",.74f,Teal,true);
   Text("CallDown",lobby,new Vector3(-1.665f,1.17f,3.119f),new Vector2(.14f,.14f),"↓",.74f,Teal,true);
   Observe("LobbyCallButton",lobby,new Vector3(-1.665f,1.425f,3.128f),new Vector3(.33f,.71f,.012f),ElevatorClue.LobbyCall,true);
   Text("LobbyFloor",lobby,new Vector3(-1.665f,2.13f,3.165f),new Vector2(.65f,.40f),"1F",1.65f,Teal,true);
   Text("LobbyTitle",lobby,new Vector3(0,2.78f,3.165f),new Vector2(2.3f,.24f),"햇살아파트",1.06f,Ink,true);
   Rounded("WindowFrame",art,new Vector3(3.55f,1.93f,3.17f),new Vector3(2.0f,1.65f,.055f),teal);
   Box("DaylightWindow",art,new Vector3(3.55f,1.93f,3.208f),new Vector3(1.84f,1.49f,.018f),sky);
   Primitive("WindowSun",art,new Vector3(3.12f,2.38f,3.23f),new Vector3(.35f,.35f,.016f),paper,PrimitiveType.Sphere);
   for(int i=0;i<3;i++)Primitive("DistantFoliage",art,new Vector3(2.94f+i*.58f,1.47f+i*.06f,3.23f),new Vector3(.73f,.72f,.016f),leaf,PrimitiveType.Sphere);
   Box("WindowMullion",art,new Vector3(3.55f,1.93f,3.25f),new Vector3(.045f,1.50f,.028f),teal);
   Plant(art,new Vector3(-3.1f,0,4.0f));Plant(art,new Vector3(3.05f,0,4.4f));
   for(int i=-1;i<=1;i++)Primitive("LobbyDownlight",art,new Vector3(i*2.5f,3.05f,4.8f),new Vector3(.32f,.025f,.32f),paper,PrimitiveType.Cylinder);
   ElevatorPaletteOptimizer.CombineStatic(art,"LobbyArchitecturePalette");
  }
  static void Plant(Transform parent,Vector3 pos)
  {
   Rounded("Planter",parent,pos+Vector3.up*.28f,new Vector3(.60f,.56f,.60f),paper);
   for(int i=0;i<5;i++){float a=i*72*Mathf.Deg2Rad;var g=Primitive("Leaf",parent,pos+new Vector3(Mathf.Sin(a)*.20f,.80f+(i%2)*.20f,Mathf.Cos(a)*.20f),new Vector3(.32f,.58f,.15f),leaf,PrimitiveType.Sphere);g.transform.localRotation=Quaternion.Euler(0,i*72,20);}
  }
  static void UpgradeBall(Transform ball,string name)
  {
   foreach(var line in ball.GetComponentsInChildren<LineRenderer>(true).Where(r=>r.name.StartsWith("Seam")))Object.DestroyImmediate(line.gameObject);
   for(int i=0;i<3;i++)
   {
    var mesh=Torus(.126f,.0025f);generated.Add(mesh);var g=new GameObject("BallSeam",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(ball,false);g.transform.localRotation=i==0?Quaternion.identity:i==1?Quaternion.Euler(90,0,0):Quaternion.Euler(0,90,0);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=ink;
   }
   ElevatorPaletteOptimizer.CombineStatic(ball,name);
  }
  static Mesh Torus(float radius,float tube)
  {
   var verts=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();const int n=64,m=6;
   for(int a=0;a<n;a++)for(int b=0;b<m;b++){float u=a*2*Mathf.PI/n,v=b*2*Mathf.PI/m;var radial=new Vector3(Mathf.Cos(u),Mathf.Sin(u),0);var normal=radial*Mathf.Cos(v)+Vector3.forward*Mathf.Sin(v);verts.Add(radial*radius+normal*tube);normals.Add(normal);}
   for(int a=0;a<n;a++)for(int b=0;b<m;b++){int x=a*m+b,y=((a+1)%n)*m+b,z=a*m+(b+1)%m,w=((a+1)%n)*m+(b+1)%m;indices.AddRange(new[]{x,y,z,y,w,z});}
   var mesh=new Mesh{name="BasketballSeam"};mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
  }
  static GameObject Observe(string name,Transform parent,Vector3 pos,Vector3 size,ElevatorClue clue,bool outside=false)
  {
   var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,outside?180:0,0);
   var collider=g.AddComponent<BoxCollider>();collider.size=size;var o=g.AddComponent<ElevatorObservation>();o.lesson=l;o.clue=clue;var xr=g.GetComponent<XRSimpleInteractable>();xr.colliders.Clear();xr.colliders.Add(collider);
   return g;
  }
  static void RefreshGlyphs()
  {
   var chars=new string((string.Join("",Directory.GetFiles(Root+"Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText))+JsonUtility.ToJson(l.settings)).Where(c=>!char.IsControl(c)).Distinct().ToArray());
   font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.TryAddCharacters(chars,out string missing);font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssetIfDirty(font);
  }
  static Color Ink=>new Color32(71,48,36,255);static Color Paper=>new Color32(255,246,219,255);static Color Teal=>new Color32(41,122,112,255);
  static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Mats+name+".mat");
  static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
  static void Clear(Transform root){for(int i=root.childCount-1;i>=0;i--)Object.DestroyImmediate(root.GetChild(i).gameObject);}
  static void RectWall(Transform parent,string name,float left,float right,float bottom,float top,float z,float depth,Material m)=>Box(name,parent,new Vector3((left+right)/2,(bottom+top)/2,z),new Vector3(right-left,top-bottom,depth),m);
  static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)=>Primitive(name,parent,pos,scale,mat,PrimitiveType.Cube);
  static GameObject Rounded(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat){var g=Box(name,parent,pos,scale,mat);g.GetComponent<MeshFilter>().sharedMesh=rounded;return g;}
  static GameObject Primitive(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type)
  {
   if(!mat)throw new InvalidOperationException("Missing material for "+name);var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;Object.DestroyImmediate(g.GetComponent<Collider>());return g;
  }
  static TMP_Text Text(string name,Transform parent,Vector3 pos,Vector2 size,string value,float point,Color color,bool outside=false)
  {
   var t=new GameObject(name).AddComponent<TextMeshPro>();t.transform.SetParent(parent,false);t.transform.localPosition=pos;t.transform.localRotation=Quaternion.Euler(0,outside?180:0,0);t.font=font;t.fontSharedMaterial=font.material;t.fontSize=point;t.fontStyle=FontStyles.Normal;t.color=color;t.text=value;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=size;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;return t;
  }
 }
}
#endif
