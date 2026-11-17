using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ProjectWait.M.Crosswalk;
using ProjectWait.M.Elevator;
using ProjectWait.M.SceneTransition;
using ProjectWait.M.TitleScene;
using Y.Crosswalk;
using Object = UnityEngine.Object;

public static class ValidateCrosswalkIntegration
{
    const string Output = "Assets/M/CodexTemp/CrosswalkIntegration~";
    static readonly List<string> Checks = new List<string>();
    static void Check(bool ok,string name) { if(!ok)throw new InvalidOperationException(name);Checks.Add(name); }
    static T Find<T>() where T:Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    static T Field<T>(object owner,string name) => (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
    static void Save(string name,object result) => File.WriteAllText(Output+"/"+name+".json",JsonConvert.SerializeObject(result,Formatting.Indented));

    public static object EditMode()
    {
        Checks.Clear();Check(!EditorApplication.isPlaying,"Editor is stopped");
        var enabled=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        Check(enabled[0]==CrosswalkSceneIntegration.LobbyPath,"Build starts in lobby");
        Check(enabled.Contains(CrosswalkSceneIntegration.ScenePath)&&enabled.Contains("Assets/M/Scene/Wait_Elevator.unity"),"Both experiences registered without replacing elevator");
        var scene=EditorSceneManager.OpenScene(CrosswalkSceneIntegration.ScenePath);
        Check(!scene.isDirty,"Saved crosswalk opens cleanly");
        int missingScripts=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        Check(missingScripts==0,"No missing scripts, including XR sample dependencies");
        var missing=new List<string>();
        foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(!component)continue;var so=new SerializedObject(component);var p=so.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue==null && p.objectReferenceInstanceIDValue!=0)missing.Add(component.name+"."+p.propertyPath);
        }
        Check(missing.Count==0,"No broken serialized object references: "+string.Join(",",missing));
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/SoftCrosswalk/GameMeshes/SoftStreetSetStatic.asset");
        Check(mesh&&mesh.vertexCount>0,"190 MB LFS environment mesh restored");
        Check(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>(true)).Any(f=>f.sharedMesh==mesh),"Street environment mesh still connected after scene save");
        var integration=Find<CrosswalkSceneIntegration>();Check(integration&&integration.viewer&&integration.input&&integration.menuCanvas,"Pause bridge references complete");
        Check(new[]{integration.resumeButton,integration.restartButton,integration.lobbyButton,integration.quitButton}.All(b=>b&&b.targetGraphic&&b.GetComponentInChildren<TMP_Text>(true)),"Four pause buttons have artwork and Korean text");
        Check(integration.menuCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()&&integration.menuCanvas.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>(),"Pause canvas has PC and XR raycasters");
        var end=Field<UnityEngine.Events.UnityEvent>(Find<ThirdSituationController>(),"m_OnFinished");
        Check(Enumerable.Range(0,end.GetPersistentEventCount()).Any(i=>end.GetPersistentTarget(i)==integration&&end.GetPersistentMethodName(i)=="ReturnToLobby"),"Final confirmation returns to lobby");
        var pipeline=(UniversalRenderPipelineAsset)Find<ElevatorSceneRendering>().scenePipeline;
        Check(pipeline&&AssetDatabase.GetAssetPath(pipeline).StartsWith("Assets/M/Arts/Rendering/Crosswalk/"),"Scene owns its rendering profile");
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/M/Arts/Rendering/Crosswalk/CrosswalkRenderer.asset");
        Check(data.rendererFeatures.Any(f=>f is CrosswalkModelOutlineFeature)&&data.rendererFeatures.Any(f=>f is ElevatorOptimizedOutlineFeature),"Model outlines and observation halo both registered");
        var shaders=AssetDatabase.GetDependencies(CrosswalkSceneIntegration.ScenePath,true).Where(p=>p.EndsWith(".shader")).Select(AssetDatabase.LoadAssetAtPath<Shader>);
        Check(shaders.All(s=>s&&!ShaderUtil.ShaderHasError(s)),"Scene shaders compile without errors");
        var lobby=EditorSceneManager.OpenScene(CrosswalkSceneIntegration.LobbyPath);
        var portal=Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None).Single(p=>p.experienceTitle=="횡단보도");
        Check(portal.destinationScene==CrosswalkSceneIntegration.ScenePath&&portal.transitionContext==CrosswalkSceneIntegration.Context&&portal.CanEnter,"Lobby crosswalk portal points to saved scene and story context");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/CrosswalkIntegration/CrosswalkIntegrationKorean.asset");
        Check(font.atlasPopulationMode==AtlasPopulationMode.Static&&font.HasCharacters(CrosswalkSceneIntegration.Context.Replace("\n","")),"New loading context is fully baked in a static Korean atlas");
        Check(font.material.renderQueue>Find<StorybookSceneTransition>().overlayMaterial.renderQueue,"Korean text renders after the paper card, not behind it");
        var rt=RenderTexture.GetTemporary(1024,1024,0,RenderTextureFormat.ARGB32);
        var old=RenderTexture.active;var readback=new Texture2D(1024,1024,TextureFormat.RGBA32,false);
        try { Graphics.Blit(font.atlasTexture,rt);RenderTexture.active=rt;readback.ReadPixels(new Rect(0,0,1024,1024),0,0);readback.Apply();Check(readback.GetPixels32().Any(p=>p.a>100&&p.a<255),"Baked SDF glyphs are uploaded to the GPU, not only CPU memory"); }
        finally { RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(readback); }
        var result=new{checks=Checks.ToArray(),missing,meshVertices=mesh.vertexCount,enabledScenes=enabled};Save("edit-validation",result);return result;
    }

    static async Task Until(Func<bool> condition,string name,float timeout=35)
    {
        double end=UnityEditor.EditorApplication.timeSinceStartup+timeout;
        while(!condition()) { if(UnityEditor.EditorApplication.timeSinceStartup>end)throw new TimeoutException(name);await Task.Delay(40); }
        Check(true,name);
    }
    static async Task Frames(int count=4)
    {
        int start=Time.frameCount;await Until(()=>Time.frameCount>=start+count,"Player loop advances "+count+" frames",8);
    }
    static async Task Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Output+"/"+name+".png");await Frames(4);
    }

    public static async Task<object> PlayMode()
    {
        Checks.Clear();Check(EditorApplication.isPlaying,"Play Mode active");await Frames();
        Check(SceneManager.GetActiveScene().path==CrosswalkSceneIntegration.LobbyPath,"Actual lobby is active");
        var originalPipeline=QualitySettings.renderPipeline;
        var portal=Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None).Single(p=>p.experienceTitle=="횡단보도");
        portal.Activate();
        await Until(()=>StorybookSceneTransition.Current.Phase==StorybookSceneTransition.TransitionPhase.Holding,"Portal starts existing sphere/story-card transition");
        var transition=StorybookSceneTransition.Current;
        Check(transition.View.ContextText.text==CrosswalkSceneIntegration.Context,"Loading panel shows crosswalk-specific context, not elevator copy");
        await Capture("01-crosswalk-loading");
        // Face the world card using the PC player's own look API, not a camera teleport.
        var player=Find<TitlePlazaPlayer>();
        if(player&&!player.IsVR)
        {
            Vector3 wanted=(transition.View.PanelAnchor-player.viewCamera.transform.position).normalized;
            float yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(player.viewCamera.transform.forward,Vector3.up),Vector3.ProjectOnPlane(wanted,Vector3.up),Vector3.up);
            float pitch=Mathf.Asin(wanted.y)*Mathf.Rad2Deg;
            float oldPitch=-Mathf.DeltaAngle(0,player.viewCamera.transform.eulerAngles.x);
            player.AddLookDelta(new Vector2(yaw/player.lookSensitivity,(pitch-oldPitch)/player.lookSensitivity));
        }
        await Until(()=>SceneManager.GetActiveScene().path==CrosswalkSceneIntegration.ScenePath&&!StorybookSceneTransition.IsBusy,"Lobby actually loads and reveals crosswalk",60);
        await Frames(8);
        var integration=CrosswalkSceneIntegration.Current;
        Check(integration&&Find<FirstSituationController>().CurrentState==FirstSituationController.SituationState.Walking,"Original automatic story start preserved");
        Check(QualitySettings.renderPipeline==Find<ElevatorSceneRendering>().scenePipeline,"Crosswalk-local renderer applied");
        Check(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Exactly one EventSystem after load");
        Check(Time.timeScale>0&&!AudioListener.pause,"Loading releases scene clock and audio");
        await Task.Delay(1800);await Frames();
        await Capture("02-crosswalk-arrival");
        if(UnityEngine.InputSystem.Keyboard.current!=null)
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));await Frames(2);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());await Frames(2);
            Check(integration.IsPaused,"Actual Esc binding opens pause menu");
        }
        else integration.Pause();
        await Frames();
        Check(integration.IsPaused&&Time.timeScale==0&&AudioListener.pause,"Pause stops game time and audio");
        float before=CrosswalkStoryTime.unscaledTime;
        var wait=new CrosswalkStoryWait(.05f);
        await Task.Delay(600);await Frames();
        Check(Mathf.Approximately(before,CrosswalkStoryTime.unscaledTime)&&wait.keepWaiting,"Unscaled lesson deadlines and dialogue waits do not elapse in pause");
        Check(integration.menuCanvas.gameObject.activeInHierarchy&&integration.menuCanvas.enabled,"Pause card is visible");
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(integration.viewer,integration.resumeButton.transform.position)};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()==integration.resumeButton,"PC UI raycast reaches the visible resume button before background lesson UI");
        await Capture("03-crosswalk-pause");
        integration.resumeButton.onClick.Invoke();await Frames();
        Check(!integration.IsPaused&&Time.timeScale>0&&!AudioListener.pause,"Real resume button restores time, input and audio");
        // The source scene auto-starts (m_WaitForStartScreen=0); don't change its authored flow.
        await Task.Delay(1200);await Frames();integration.Pause();
        var state=Find<FirstSituationController>().CurrentState;
        var text=Find<FirstSituationUI>().GetComponentsInChildren<TMP_Text>(true).Select(t=>t.text).ToArray();
        before=CrosswalkStoryTime.unscaledTime;await Task.Delay(1000);await Frames();
        Check(Mathf.Approximately(before,CrosswalkStoryTime.unscaledTime)&&Find<FirstSituationController>().CurrentState==state,"Pause preserves a running story stage");
        integration.resumeButton.onClick.Invoke();await Frames();
        await Capture("04-world-dialogue");
        // Restart uses the same transition and starts a fresh scene instead of keeping stale coroutines.
        integration.Pause();integration.restartButton.onClick.Invoke();
        await Until(()=>StorybookSceneTransition.IsBusy,"Restart button starts a transition");
        await Until(()=>CrosswalkSceneIntegration.Current&&CrosswalkSceneIntegration.Current!=integration&&!StorybookSceneTransition.IsBusy,"Restart reloads the same scene",60);
        integration=CrosswalkSceneIntegration.Current;
        Check(Find<FirstSituationController>().CurrentState==FirstSituationController.SituationState.Walking&&Time.timeScale>0&&!AudioListener.pause,"Restart resets story and releases pause state");
        integration.Pause();integration.lobbyButton.onClick.Invoke();
        await Until(()=>StorybookSceneTransition.IsBusy,"Lobby button starts return transition");
        await Until(()=>SceneManager.GetActiveScene().path==CrosswalkSceneIntegration.LobbyPath&&!StorybookSceneTransition.IsBusy,"Return actually loads the hub",60);
        await Frames();Check(QualitySettings.renderPipeline==originalPipeline,"Crosswalk rendering override restored on exit");
        Check(Time.timeScale>0&&!AudioListener.pause,"Time and audio restored in lobby");
        await Capture("05-lobby-return");
        var result=new{checks=Checks.ToArray(),finalScene=SceneManager.GetActiveScene().path,frame=Time.frameCount};Save("play-validation",result);return result;
    }

    public static async Task<object> ProbeText()
    {
        if(SceneManager.GetActiveScene().path==CrosswalkSceneIntegration.LobbyPath)
        {
            Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None).Single(p=>p.experienceTitle=="횡단보도").Activate();
            await Until(()=>CrosswalkSceneIntegration.Current&&!StorybookSceneTransition.IsBusy,"Probe loads scene",60);
        }
        var integration=CrosswalkSceneIntegration.Current;
        integration.Pause();await Frames(4);
        var text=integration.resumeButton.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate();
        var font=text.font;
        return new {label=text.text,active=text.isActiveAndEnabled,alpha=text.canvasRenderer.GetAlpha(),culled=text.canvasRenderer.cull,vertices=text.textInfo.meshInfo[0].vertexCount,material=text.fontSharedMaterial.name,shader=text.fontSharedMaterial.shader.name,textureMatches=font.material.mainTexture==font.atlasTexture,texturePath=AssetDatabase.GetAssetPath(font.material.mainTexture),pointSize=font.faceInfo.pointSize,scale=font.faceInfo.scale,ascent=font.faceInfo.ascentLine,descent=font.faceInfo.descentLine,glyphScale=font.characterTable[0].glyph.scale,charScale=font.characterTable[0].scale,color=text.color.ToString(),vertexAlpha=text.textInfo.meshInfo[0].colors32.Take(4).Select(c=>c.a).ToArray(),uv=text.textInfo.meshInfo[0].uvs0.Take(4).Select(v=>v.ToString()).ToArray(),keyword=string.Join(",",font.material.shaderKeywords),renderMaterial=text.canvasRenderer.GetMaterial(0).name};
    }

    public static async Task<object> ElevatorRegression()
    {
        Checks.Clear();Check(EditorApplication.isPlaying&&SceneManager.GetActiveScene().path==CrosswalkSceneIntegration.LobbyPath,"Regression starts in live lobby");
        var portal=Object.FindObjectsByType<TitlePortal>(FindObjectsSortMode.None).Single(p=>p.experienceTitle=="엘리베이터");
        portal.Activate();
        await Until(()=>StorybookSceneTransition.Current.Phase==StorybookSceneTransition.TransitionPhase.Holding,"Existing elevator portal still starts transition");
        Check(StorybookSceneTransition.Current.View.ContextText.text==StorybookSceneTransition.ElevatorContext,"Elevator keeps its own original loading story");
        var player=Find<TitlePlazaPlayer>();
        if(player&&!player.IsVR)
        {
            var transition=StorybookSceneTransition.Current;
            Vector3 wanted=(transition.View.PanelAnchor-player.viewCamera.transform.position).normalized;
            float yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(player.viewCamera.transform.forward,Vector3.up),Vector3.ProjectOnPlane(wanted,Vector3.up),Vector3.up);
            float pitch=Mathf.Asin(wanted.y)*Mathf.Rad2Deg;
            float oldPitch=-Mathf.DeltaAngle(0,player.viewCamera.transform.eulerAngles.x);
            player.AddLookDelta(new Vector2(yaw/player.lookSensitivity,(pitch-oldPitch)/player.lookSensitivity));
        }
        await Until(()=>SceneManager.GetActiveScene().path=="Assets/M/Scene/Wait_Elevator.unity"&&!StorybookSceneTransition.IsBusy,"Existing elevator scene still loads and reveals",60);
        var pause=Find<ProjectWait.M.Elevator.DialoguePreview.ElevatorPauseMenu>();
        Check(pause&&pause.controls&&pause.lesson,"Original elevator pause and lesson references intact");
        pause.Pause();await Frames();Check(pause.IsPaused&&Time.timeScale==0,"Original elevator pause still works");
        pause.Resume();await Frames();Check(!pause.IsPaused&&Time.timeScale>0&&!AudioListener.pause,"Original elevator resume restores clock and audio");
        Check(!CrosswalkSceneIntegration.Current,"Crosswalk-only pause bridge does not leak into elevator");
        var result=new{checks=Checks.ToArray(),scene=SceneManager.GetActiveScene().path};Save("elevator-regression",result);return result;
    }
}
