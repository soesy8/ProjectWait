#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectWait.M.Elevator.DialoguePreview;
using ProjectWait.M.TitleScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Object = UnityEngine.Object;

namespace ProjectWait.M.EditorTools
{
    // Opt-in, no scene saves and no project/XR settings edits. All output stays in M.
    [InitializeOnLoad]
    public static class VRImprovementValidation
    {
        const string Output = "Assets/M/CodexTemp/VRImprovement_20261113~";
        const string Request = Output + "/validate.request";
        static readonly List<string> results = new List<string>();
        static readonly List<Object> temporary = new List<Object>();

        static VRImprovementValidation()
        {
            EditorApplication.delayCall += Requested;
#if M_VR_HARNESS
            EditorApplication.playModeStateChanged += HarnessPlayState;
            EditorApplication.update += RunQueuedHarness;
#endif
        }
#if M_VR_HARNESS
        public static void RunHarnessInPlayMode()
        {
            Debug.Log("[M VR harness] Entering isolated Play Mode");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool("MVRHarnessQueued",true);
            EditorApplication.EnterPlaymode();
        }
        static void HarnessPlayState(PlayModeStateChange state)
        {
            Debug.Log("[M VR harness] "+state);
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("MVRHarnessQueued",false))return;
            EditorApplication.update -= RunQueuedHarness;
            EditorApplication.update += RunQueuedHarness;
        }
        static void RunQueuedHarness()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||!SessionState.GetBool("MVRHarnessQueued",false))return;
            EditorApplication.update -= RunQueuedHarness;
            SessionState.SetBool("MVRHarnessQueued",false);
            try { Run(); EditorApplication.Exit(results.Any(line=>line.StartsWith("FAIL:"))?1:0); }
            catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
#endif
        static void Requested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Requested; return; }
            // Never stop or switch the user's running lesson.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request); Run();
        }

        [MenuItem("Tools/Wait/M/Validate VR Improvement")]
        public static void Run()
        {
#if !M_VR_HARNESS
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run these checks outside Play Mode.");
#endif
            Directory.CreateDirectory(Output); results.Clear();
            results.Add("UTC: " + DateTime.UtcNow.ToString("O"));
            Check("Symmetric binocular safe area", () => Geometry(0, false));
            Check("Asymmetric binocular safe area", () => Geometry(.22f, false));
            Check("Canted-eye conservative safe area", () => Geometry(.12f, true));
            Check("Recorded Quest frusta / guide / status / black-band coverage", RecordedFrusta);
            Check("Tilted/behind/near/far rejection", RejectGeometry);
            if(Application.isPlaying)Check("Aim binding / tracking-loss / held-trigger recovery", Aim);
            else results.Add("NOT RUN: aim InputActions require the isolated Play Mode harness; Editor updates intentionally ignore gameplay actions.");
            Check("8m range / nearest obstruction / ray endpoint", Raycast);
#if !M_VR_HARNESS
            Check("Saved lobby scene wiring and panel distances", LobbyScene);
            Check("Saved elevator scene wiring / missing references", ElevatorScene);
            Check("Desktop letterbox geometry", DesktopOverlay);
#else
            results.Add("HARNESS: production safe-area/lobby source; scene-transition collaborators are inert test doubles. Scene wiring and desktop overlay are not exercised here.");
#endif
            results.Add("NOT TESTED: native headset rendering, lens distortion, Korean readability/comfort, full story playthrough.");
            File.WriteAllLines(Output + "/validation.txt", results);
            Debug.Log("[M VR validation]\n" + string.Join("\n", results));
        }

        static void Check(string name, Action test)
        {
            try { test(); results.Add("PASS: " + name); }
            catch (Exception ex) { results.Add("FAIL: " + name + " -- " + ex); }
            finally
            {
                for (int i=temporary.Count-1;i>=0;i--) if (temporary[i]) Object.DestroyImmediate(temporary[i]);
                temporary.Clear();
            }
        }
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static GameObject Go(string name, bool rect = false)
        {
            var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            go.hideFlags = HideFlags.HideAndDontSave; temporary.Add(go); return go;
        }
        static StereoUISafeArea.Frame Frame(float asymmetry, bool canted = false)
        {
            var flip = Matrix4x4.Scale(new Vector3(1,1,-1));
            return new StereoUISafeArea.Frame {
                leftView = flip * Matrix4x4.TRS(new Vector3(-.032f,0,0),Quaternion.Euler(0,canted?-5:0,0),Vector3.one).inverse,
                rightView = flip * Matrix4x4.TRS(new Vector3(.032f,0,0),Quaternion.Euler(0,canted?5:0,0),Vector3.one).inverse,
                leftProjection = Matrix4x4.Frustum(-.05f+asymmetry*.05f,.05f+asymmetry*.05f,-.05f,.05f,.05f,100),
                rightProjection = Matrix4x4.Frustum(-.05f-asymmetry*.05f,.05f-asymmetry*.05f,-.05f,.05f,.05f,100),
                near = .05f, far = 100
            };
        }
        static void Geometry(float asymmetry, bool canted)
        {
            var frame = Frame(asymmetry,canted);
            var root = (RectTransform)Go("M validation plane",true).transform;
            root.position = Vector3.forward;
            root.localScale = Vector3.one/720;
            Require(frame.Plane(root,out var safe,out var coverage),"No safe area");
            Require(coverage.width>=safe.width&&coverage.height>=safe.height,"Background coverage must contain safe text area");
            for (int i=0;i<4;i++)
            {
                var p = new Vector2((i&1)==0?safe.xMin:safe.xMax,(i&2)==0?safe.yMin:safe.yMax);
                Require(frame.Contains(root.TransformPoint(p)),"Safe corner not binocular");
            }
            root.sizeDelta = coverage.size;
            var panel = (RectTransform)Go("M validation panel",true).transform;
            panel.SetParent(root,false); panel.sizeDelta=new Vector2(320,158);
            panel.pivot=Vector2.one;panel.anchorMin=panel.anchorMax=Vector2.one;
            panel.localScale=Vector3.one*StereoUISafeArea.VisualScale;
            StereoUISafeArea.Place(panel,safe,Vector2.one);
            var corners = new Vector3[4]; panel.GetWorldCorners(corners);
            Require(corners.All(p=>frame.Contains(p)),"Guide placement was clipped");
            panel.pivot=new Vector2(1,0);panel.anchorMin=panel.anchorMax=panel.pivot;
            StereoUISafeArea.Place(panel,safe,new Vector2(1,0));panel.GetWorldCorners(corners);
            Require(corners.All(p=>frame.Contains(p)),"Status placement was clipped");
            // Repeat absolute placement: 90% must never compound into 81%.
            StereoUISafeArea.Place(panel,safe,new Vector2(1,0));
            Require(Mathf.Abs(panel.localScale.x-.9f)<.00001f,"Visual scale compounded");
            results.Add($"  safe={safe}, coverage={coverage}, scale={panel.localScale.x}");
        }
        static void RejectGeometry()
        {
            var frame=Frame(.2f);
            Require(!frame.Contains(Vector3.back),"Accepted behind-camera point");
            Require(!frame.Contains(Vector3.forward*.01f),"Accepted near-clipped point");
            Require(!frame.Contains(Vector3.forward*101),"Accepted far-clipped point");
            Require(!frame.Contains(new Vector3(float.NaN,0,1)),"Accepted NaN");
            Require(!frame.ProjectQuad(new Vector3(.9f,0,1),Quaternion.Euler(0,50,0),new Vector2(.6f,.3f),out _),"Accepted tilted offscreen card");
            Require(frame.ProjectQuad(Vector3.forward*1.4f,Quaternion.identity,new Vector2(.5f,.4f),out _),"Rejected readable central card");
            var stamp=new StereoUISafeArea.LayoutStamp();
            var camera=Go("M validation camera").AddComponent<Camera>();
            Require(stamp.Update(camera,frame)&&!stamp.Update(camera,frame),"Stable projection caused repeated layout");
            camera.transform.SetPositionAndRotation(new Vector3(2,1,3),Quaternion.Euler(10,30,5));
            frame.leftView=frame.leftView*camera.transform.worldToLocalMatrix;
            frame.rightView=frame.rightView*camera.transform.worldToLocalMatrix;
            Require(!stamp.Update(camera,frame),"Head motion reanchored world card");
            frame.leftProjection.m02+=.02f;
            Require(stamp.Update(camera,frame),"Projection change did not invalidate layout");
        }
        static void RecordedFrusta()
        {
            // 2026-11-12 native-eye measurements, not a hard-coded runtime headset profile.
            var frame=Frame(0);
            frame.leftProjection.m00=frame.rightProjection.m00=.877215445f;
            frame.leftProjection.m02=-.122784555f;frame.rightProjection.m02=.122784555f;
            frame.leftProjection.m11=frame.rightProjection.m11=.8686716f;
            frame.leftProjection.m12=frame.rightProjection.m12=-.0352425165f;
            var root=(RectTransform)Go("M recorded projection plane",true).transform;
            foreach(float distance in new[]{.45f,.75f,1f})
            {
                root.position=Vector3.forward*distance;
                root.localScale=Vector3.one*(2*distance*Mathf.Tan(98.04016f*.5f*Mathf.Deg2Rad)/720);
                Require(frame.Plane(root,out var safe,out var coverage),"Recorded frusta had no safe plane");
                var panel=(RectTransform)Go("M recorded panel",true).transform;
                panel.SetParent(root,false);panel.pivot=Vector2.one;panel.anchorMin=panel.anchorMax=Vector2.one;
                panel.localScale=Vector3.one*.9f;panel.sizeDelta=new Vector2(400,158);
                StereoUISafeArea.Place(panel,safe,Vector2.one);
                var corners=new Vector3[4];panel.GetWorldCorners(corners);
                Require(corners.All(p=>frame.Contains(p)),"Recorded-frustum guide clipped");
                panel.sizeDelta=new Vector2(430,143);
                StereoUISafeArea.Place(panel,safe,new Vector2(1,0));panel.GetWorldCorners(corners);
                Require(corners.All(p=>frame.Contains(p)),"Recorded-frustum status clipped");
                if(distance==.45f)
                {
                    var lowerLeft=root.TransformPoint(new Vector2(coverage.xMin-20,coverage.yMin-20));
                    var lowerRight=root.TransformPoint(new Vector2(coverage.xMax+20,coverage.yMin-20));
                    for(int eye=0;eye<2;eye++)
                    {
                        Require(frame.Project(lowerLeft,eye,out var l)&&frame.Project(lowerRight,eye,out var r)&&l.x<0&&r.x>1&&l.y<0&&r.y<0,"Black-band edge gap");
                    }
                }
                results.Add($"  recorded frusta at {distance:F2}m: safe={safe}, UI scale=0.9");
            }
            Require(frame.ProjectQuad(Vector3.forward*1.4f,Quaternion.identity,new Vector2(.55f,.7f),out _,.19f),"Recorded-frustum central dialogue/choice bundle clipped");
        }
        static void Aim()
        {
            const string layout="M_LobbyAimValidation";
            InputSystem.RegisterLayout("{\"name\":\""+layout+"\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"pointer\",\"layout\":\"Pose\",\"offset\":64,\"format\":\"Pose\",\"sizeInBits\":480},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"offset\":124,\"format\":\"FLT\",\"sizeInBits\":32}]}");
            var device=InputSystem.AddDevice(layout);
            TitlePlazaPlayer player=null;
            try
            {
                InputSystem.SetDeviceUsage(device,UnityEngine.InputSystem.CommonUsages.RightHand);
                player=Go("M validation player").AddComponent<TitlePlazaPlayer>();
                // Edit-mode AddComponent does not run this runtime-only MonoBehaviour's OnEnable.
                if(typeof(TitlePlazaPlayer).GetField("aimPose",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)==null)
                    typeof(TitlePlazaPlayer).GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
                player.viewCamera=Go("M aim camera").AddComponent<Camera>();
                var pointer=device.TryGetChildControl<PoseControl>("pointer");
                Require(pointer!=null,"Missing pointer PoseControl");
                var pose=new PoseState { isTracked=true,trackingState=InputTrackingState.Position|InputTrackingState.Rotation,
                    position=new Vector3(.2f,1.1f,.3f),rotation=Quaternion.Euler(18,27,0) };
                // Normal gameplay actions intentionally ignore Editor input updates.
                typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
                ChangePose(pointer,pose);
                var action=(InputAction)typeof(TitlePlazaPlayer).GetField("aimPose",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);
                var read=pointer.ReadValue();var actionRead=action.ReadValue<PoseState>();
                results.Add($"  aim diagnostic: enabled={device.enabled}, added={device.added}, controls={action.controls.Count}, active={action.activeControl?.path}, phase={action.phase}, direct tracked={read.isTracked}, direct state={read.trackingState}, action tracked={actionRead.isTracked}, action state={actionRead.trackingState}, q={actionRead.rotation}, update={InputState.currentUpdateType}");
                results.Add($"  pose layout: parent={pointer.stateBlock.byteOffset}, tracked={pointer.isTracked.stateBlock.byteOffset}:{pointer.isTracked.stateBlock.bitOffset}:{pointer.isTracked.stateBlock.format}, child tracked={pointer.isTracked.ReadValue()}, flags={pointer.trackingState.stateBlock.byteOffset}");
                Require(player.TryGetAimRay(out var ray),"Aim pose action did not bind");
                Require(Vector3.Distance(ray.origin,pose.position)<.0001f&&Vector3.Angle(ray.direction,pose.rotation*Vector3.forward)<.01f,"Ray did not use the aim origin/rotation");
                // Real provider layouts can encode a boolean as byte 1, not normalized byte 255.
                InputState.Change(pointer.isTracked,(byte)1,InputUpdateType.Dynamic);
                Require(player.TryGetAimRay(out ray),"Byte-1 tracking flag incorrectly hid a valid aim ray");
                var leftPrefab=Go("Left visual fixture");var rightPrefab=Go("Right visual fixture");
                leftPrefab.transform.localPosition=rightPrefab.transform.localPosition=new Vector3(0,0,-.05f);
                leftPrefab.transform.localRotation=rightPrefab.transform.localRotation=Quaternion.Euler(0,180,0);
                rightPrefab.transform.localScale=new Vector3(-1,1,1);
                var line=Go("Controller ray").AddComponent<LineRenderer>();line.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Hidden/Internal-Colored"));temporary.Add(line.sharedMaterial);
                using(var visuals=new TitleLobbyControllerVisuals(player.transform,line,leftPrefab,rightPrefab,action))
                {
                    visuals.Update(true);
                    var root=player.transform.Find("Lobby tracked controller visuals");var right=root.Find("Right controller");var model=right.GetChild(0);
                    Require(right.gameObject.activeSelf&&Vector3.Distance(right.localPosition,pose.position)<.0001f&&Quaternion.Angle(right.localRotation,pose.rotation)<.01f,"Controller model does not use the ray's Aim pose");
                    Require(model.localPosition==rightPrefab.transform.localPosition&&Quaternion.Angle(model.localRotation,rightPrefab.transform.localRotation)<.01f&&model.localScale==rightPrefab.transform.localScale,"Model axis correction or right-hand mirroring lost");
                    InputState.Change(pointer.isTracked,(byte)0,InputUpdateType.Dynamic);visuals.Update(true);Require(!right.gameObject.activeSelf,"Untracked controller model remained visible");
                    InputState.Change(pointer.isTracked,(byte)1,InputUpdateType.Dynamic);visuals.Update(true);Require(right.gameObject.activeSelf,"Controller did not recover byte-1 tracking");
                    visuals.Update(false);Require(!root.gameObject.activeSelf,"Transition did not hide controller visuals");
                }
                results.Add("  PASS: controller Aim/model basis, local axis correction, mirrored right hand, tracking loss/recovery and transition hiding");
                typeof(TitlePlazaPlayer).GetProperty("IsVR").SetValue(player,true);
                var hovered=Go("M stale hover").AddComponent<TitlePortal>();
                typeof(TitlePlazaPlayer).GetField("hovered",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,hovered);
                pose.isTracked=false;pose.trackingState=InputTrackingState.None;ChangePose(pointer,pose);
                Require(!player.TryGetAimRay(out _),"Tracking loss kept a stale aim");
                typeof(TitlePlazaPlayer).GetMethod("UpdateInteraction",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
                Require(player.HoveredExperience==""&&typeof(TitlePlazaPlayer).GetField("hovered",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)==null,"Tracking loss kept hover");
                var update=typeof(TitlePlazaPlayer).GetMethod("UpdateInteraction",BindingFlags.NonPublic|BindingFlags.Instance);
                var trigger=device.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed");
                player.transform.position=new Vector3(11000,11000,11000);
                var target=Go("M recovered aim portal");target.transform.position=player.transform.position+Vector3.forward*4;
                target.AddComponent<BoxCollider>();var portal=target.AddComponent<TitlePortal>();portal.experienceTitle="Validation";portal.player=player;
                Physics.SyncTransforms();InputState.Change(trigger,1f,InputUpdateType.Dynamic);
                pose.isTracked=true;pose.trackingState=InputTrackingState.Position|InputTrackingState.Rotation;pose.position=Vector3.zero;pose.rotation=Quaternion.identity;
                ChangePose(pointer,pose);update.Invoke(player,null);
                Require(player.HoveredExperience=="Validation"&&player.LastMessage=="","Held trigger activated on tracking recovery");
                InputState.Change(trigger,0f,InputUpdateType.Dynamic);update.Invoke(player,null);
                InputState.Change(trigger,1f,InputUpdateType.Dynamic);update.Invoke(player,null);
                Require(player.LastMessage!="","Fresh trigger did not select the aim/hover target");
            }
            finally
            {
                if(player)typeof(TitlePlazaPlayer).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
                InputSystem.RemoveDevice(device); InputSystem.RemoveLayout(layout);
            }
        }
        static void Raycast()
        {
            var player=Go("M ray player").AddComponent<TitlePlazaPlayer>();
            var target=Go("M ray portal"); target.transform.position=new Vector3(10000,10000,10007.8f);
            var box=target.AddComponent<BoxCollider>();box.size=Vector3.one*.1f;
            var portal=target.AddComponent<TitlePortal>();
            var ray=new Ray(new Vector3(10000,10000,10000),Vector3.forward);
            Physics.SyncTransforms();
            Require(player.TraceInteraction(ray,out var hit)==portal&&Mathf.Abs(hit.z-10007.75f)<.01f,"In-range panel/endpoint failed");
            target.transform.position=new Vector3(10000,10000,10008.2f);Physics.SyncTransforms();
            Require(!player.TraceInteraction(ray,out var end)&&Mathf.Abs(Vector3.Distance(ray.origin,end)-8)<.01f,"Beyond-8m panel was selected");
            target.transform.position=new Vector3(10000,10000,10007.8f);
            var wall=Go("M ray obstruction");wall.transform.position=new Vector3(10000,10000,10004);
            wall.AddComponent<BoxCollider>();Physics.SyncTransforms();
            Require(!player.TraceInteraction(ray,out hit)&&hit.z<10004,"Obstacle behind-selection/endpoint failed");
        }
        static void ChangePose(PoseControl pointer,PoseState pose)
        {
            // PoseState is the raw fixed-layout state, including its one-byte tracked flag.
            InputState.Change(pointer,pose,InputUpdateType.Dynamic);
            // The generic Pose layout's eight-bit BIT button is normalized 0..255;
            // unlike OpenXR's provider-specific layout it is not a one-bit boolean.
            InputState.Change(pointer.isTracked,pose.isTracked?(byte)255:(byte)0,InputUpdateType.Dynamic);
        }
        static IEnumerable<T> Components<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
        static void Missing(Component component)
        {
            Require(component,"Missing component");
            var serialized=new SerializedObject(component);var property=serialized.GetIterator();
            while(property.NextVisible(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceInstanceIDValue!=0)
                    Require(property.objectReferenceValue,component.name+" missing "+property.propertyPath);
        }
        static void LobbyScene()
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/M/Scene/Wait_Robby.unity");
            try
            {
                var player=Components<TitlePlazaPlayer>(scene).Single();Missing(player);
                Require(player.interactionDistance==8&&player.viewCamera&&player.controllerRay&&player.leftControllerVisual&&player.rightControllerVisual,"Lobby scene range/wiring mismatch");
                foreach(var prefab in new[]{player.leftControllerVisual,player.rightControllerVisual})
                {
                    Require(AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/M/Prefabs/Controllers/"),"Controller prefab is not M-owned");
                    Require(prefab.GetComponentsInChildren<MeshFilter>(true).Length==9,"Elevator controller mesh hierarchy changed");
                    Require(prefab.GetComponentsInChildren<Collider>(true).Length==0,"Controller model can intercept its selection ray");
                    foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))Require(renderer.sharedMaterials.All(m=>m),"Controller material missing");
                }
                foreach(var portal in Components<TitlePortal>(scene))
                {
                    var collider=portal.GetComponentInChildren<Collider>();
                    Require(collider,"Portal collider missing");
                    float distance=Vector3.Distance(player.viewCamera.transform.position,collider.bounds.ClosestPoint(player.viewCamera.transform.position));
                    Require(distance<8,portal.experienceTitle+" beyond start range");
                    results.Add("  lobby panel "+portal.experienceTitle+": "+distance.ToString("F3")+"m (saved start-camera distance, not tracked aim)");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        static void ElevatorScene()
        {
#if !M_VR_HARNESS
            var scene=EditorSceneManager.OpenPreviewScene("Assets/M/Scene/Wait_Elevator.unity");
            try
            {
                var view=Components<ElevatorView>(scene).Single();Missing(view);Missing(view.spatial);
                Require(view.playerCamera&&view.spatial.hud&&view.spatial.speakerTab&&view.actionPanel&&view.dialoguePanel,"Elevator UI wiring missing");
                foreach(var c in Components<ElevatorCinematicOverlay>(scene))Missing(c);
                foreach(var c in Components<ElevatorTutorialPreview>(scene))Missing(c);
                var controls=Components<ElevatorDesktopControls>(scene).Single();Missing(controls);Missing(controls.movement);
                var origin=controls.xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
                Require(origin&&origin.RequestedTrackingOriginMode==Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor,"Elevator Floor tracking mode changed");
                Require(Mathf.Abs(origin.CameraFloorOffsetObject.transform.localPosition.y)<.0001f&&Mathf.Abs(view.playerCamera.transform.localPosition.y-1.1064756f)<.0001f,"Saved camera offsets are not startup-safe");
                var map=Components<ElevatorSportsCentreMap>(scene).Single();
                Require(map.gymStart==new Vector3(0,7.92f,-13)&&map.lobby.y==7.92f,"Authored opening/floor coordinates changed");
                Require(Components<Transform>(scene).Any(t=>t.name=="PC_Test_Controls")&&Components<Transform>(scene).Any(t=>t.name=="VR_Preview_Test"&&!t.gameObject.activeSelf),"Deferred test objects removed or activated");
                foreach(var go in Components<Transform>(scene))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject)==0,"Missing script on "+go.name);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
#endif
        }
        static void DesktopOverlay()
        {
#if !M_VR_HARNESS
            Require(!XRSettings.isDeviceActive,"Physical XR is active; do not alter it to run a desktop check");
            var camera=Go("M desktop camera").AddComponent<Camera>();camera.aspect=16f/9;camera.fieldOfView=60;
            var overlay=Go("M desktop overlay",true).AddComponent<ElevatorCinematicOverlay>();overlay.playerCamera=camera;overlay.Build();
            typeof(ElevatorCinematicOverlay).GetMethod("Position",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(overlay,null);
            var top=overlay.transform.Find("TopLetterbox") as RectTransform;
            var bottom=overlay.transform.Find("BottomLetterbox") as RectTransform;
            Require(Mathf.Abs(top.rect.width-1300)<.01f&&top.rect.height==82&&bottom.rect.height==82,"PC letterbox geometry changed");
            Require(bottom.Find("CinematicSubtitle").localScale==Vector3.one,"PC subtitle was scaled");
#endif
        }
    }
}
#endif
