using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ProjectWait.M.Crosswalk;
using ProjectWait.M.Elevator;
using ProjectWait.M.Elevator.DialoguePreview;
using ProjectWait.M.SceneTransition;
using ProjectWait.M.TitleScene;
using Y.Crosswalk;
using Object = UnityEngine.Object;

public static class BuildCrosswalkIntegration
{
    const string Output = "Assets/M/CodexTemp/CrosswalkIntegration~";
    const string Rendering = "Assets/M/Arts/Rendering/Crosswalk";
    static T Asset<T>(string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    public static string Apply()
    {
        Require(!EditorApplication.isPlaying, "Stop Play Mode before building");
        for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "An open scene has unsaved user changes");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var elevator = EditorSceneManager.OpenScene("Assets/M/Scene/Wait_Elevator.unity", OpenSceneMode.Single);
        var templateFade = Find<LobbyDissolveTest>(elevator);
        var templateTransition = Find<StorybookSceneTransition>(elevator);
        var templatePause = Find<ElevatorPauseMenu>(elevator);
        Require(templateFade && templateTransition && templatePause, "Main scene integration templates are missing");
        var pauseText = templatePause.resumeButton.GetComponentInChildren<TMP_Text>(true);
        var font = pauseText.font;
        var textMaterial = pauseText.fontSharedMaterial;
        var uiMaterial = templateTransition.overlayMaterial;
        var frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/M/UI/Storybook/Art/Cartoon/DialogueFrame.png");
        var teal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/M/UI/Storybook/Art/Cartoon/ButtonTeal.png");
        var cream = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/M/UI/Storybook/Art/Cartoon/ButtonCream.png");

        // Keep templates alive only until their values are copied into the new scene.
        var crosswalk = EditorSceneManager.OpenScene(CrosswalkSceneIntegration.ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(crosswalk);
        var integration = Find<CrosswalkSceneIntegration>(crosswalk);
        Require(!integration, "Integration already exists; inspect it instead of duplicating the hierarchy");
        var input = Find<CrosswalkDesktopInput>(crosswalk);
        var origin = Find<Unity.XR.CoreUtils.XROrigin>(crosswalk);
        Require(input && origin && origin.Camera, "Existing crosswalk player is missing");
        var go = new GameObject("Crosswalk Main Integration");
        integration = go.AddComponent<CrosswalkSceneIntegration>();
        integration.viewer = origin.Camera; integration.input = input;
        var fade = go.AddComponent<LobbyDissolveTest>();
        EditorUtility.CopySerialized(templateFade, fade);
        fade.viewCamera = origin.Camera;
        fade.targetRenderers = Array.Empty<Renderer>();
        fade.panels = Array.Empty<LobbyDissolveTest.Panel>();
        fade.useSceneMaterials = true; fade.enableKShortcut = false;
        var transition = new GameObject("Crosswalk Story Transition").AddComponent<StorybookSceneTransition>();
        EditorUtility.CopySerialized(templateTransition, transition);
        transition.lobbyFade = fade; transition.previewTitle = "횡단보도";
        transition.previewContext = CrosswalkSceneIntegration.Context;
        transition.enablePreviewShortcut = false;
        transition.revealOnStandaloneStart = false;

        // Own a renderer/pipeline; never mutate the lobby/elevator or global quality assets.
        Directory.CreateDirectory(Rendering);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string rendererPath = Rendering + "/CrosswalkRenderer.asset";
        string pipelinePath = Rendering + "/CrosswalkPipeline.asset";
        Require(!File.Exists(rendererPath) && !File.Exists(pipelinePath), "Rendering outputs already exist");
        Require(AssetDatabase.CopyAsset("Assets/Settings/Project Configuration/Android Preset.asset", rendererPath), "Renderer copy failed");
        Require(AssetDatabase.CopyAsset("Assets/Settings/Project Configuration/Quality URP Config.asset", pipelinePath), "Pipeline copy failed");
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        renderer.rendererFeatures.Clear();
        var modelOutline = ScriptableObject.CreateInstance<CrosswalkModelOutlineFeature>();
        modelOutline.name = "Crosswalk model outlines";
        AssetDatabase.AddObjectToAsset(modelOutline, renderer); renderer.rendererFeatures.Add(modelOutline);
        var highlight = ScriptableObject.CreateInstance<ElevatorOptimizedOutlineFeature>();
        highlight.name = "Shared observation outlines and halo";
        highlight.maskShader = Shader.Find("ProjectWait/M/Scene Outline Coverage");
        highlight.distanceShader = Shader.Find("ProjectWait/M/Outline Distance");
        Require(highlight.maskShader && highlight.distanceShader, "Outline shaders missing");
        AssetDatabase.AddObjectToAsset(highlight, renderer); renderer.rendererFeatures.Add(highlight);
        var rendererData = new SerializedObject(renderer);
        rendererData.FindProperty("m_IntermediateTextureMode").intValue = 1;
        rendererData.ApplyModifiedPropertiesWithoutUndo(); renderer.SetDirty(); EditorUtility.SetDirty(renderer);
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        pipeline.supportsCameraDepthTexture = true; pipeline.supportsHDR = true;
        var pipelineData = new SerializedObject(pipeline);
        var rendererList = pipelineData.FindProperty("m_RendererDataList");
        rendererList.arraySize = 1; rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        pipelineData.FindProperty("m_DefaultRendererIndex").intValue = 0;
        pipelineData.FindProperty("m_ReflectionProbeAtlas").boolValue = true;
        pipelineData.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
        go.AddComponent<ElevatorSceneRendering>().scenePipeline = pipeline;

        var menuRoot = Rect("Crosswalk Pause Menu", go.transform, new Vector2(1280,720), Vector2.zero);
        var canvas = menuRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = origin.Camera;
        canvas.sortingOrder = 1000; canvas.overrideSorting = true;
        menuRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        menuRoot.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        integration.menuCanvas = canvas;
        var card = Rect("Paper Pause Card", menuRoot, new Vector2(620,650), Vector2.zero);
        Image(card, frame, uiMaterial, false);
        Text("Heading",card,"잠깐 쉬어가요",new Vector2(520,60),new Vector2(0,252),40,SceneTransitionView.Ink,font,textMaterial);
        integration.statusText = Text("Status",card,"이야기를 잠시 멈췄어요.\n준비되면 계속해요.",new Vector2(520,90),new Vector2(0,175),25,SceneTransitionView.Ink,font,textMaterial);
        integration.resumeButton = Button("Resume", "계속하기", 75, true);
        integration.restartButton = Button("Restart", "처음부터 다시", -22, false);
        integration.lobbyButton = Button("Lobby", "로비로 돌아가기", -119, false);
        integration.quitButton = Button("Quit", "끝내기", -216, false);
        Text("Controls",card,"PC: Esc   ·   VR: 메뉴 버튼",new Vector2(530,38),new Vector2(0,-290),22,SceneTransitionView.Teal,font,textMaterial);
        menuRoot.localScale = Vector3.one*.0016f;
        menuRoot.gameObject.SetActive(false);

        // The last existing confirmation returns to the hub; no forced scene jump mid-dialogue.
        var third = Find<ThirdSituationController>(crosswalk);
        var thirdData = new SerializedObject(third);
        var finish = thirdData.FindProperty("m_OnFinished");
        Require(finish != null, "Crosswalk completion event missing");
        var completionEvent = (UnityEngine.Events.UnityEvent)typeof(ThirdSituationController).GetField("m_OnFinished", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(third);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(completionEvent, integration.ReturnToLobby);
        EditorUtility.SetDirty(third);
        EditorSceneManager.MarkSceneDirty(crosswalk);
        Require(EditorSceneManager.SaveScene(crosswalk), "Crosswalk save failed");
        EditorSceneManager.CloseScene(elevator, true);

        var lobby = EditorSceneManager.OpenScene(CrosswalkSceneIntegration.LobbyPath, OpenSceneMode.Single);
        var portal = lobby.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TitlePortal>(true)).Single(p=>p.experienceTitle=="횡단보도");
        portal.destinationScene = CrosswalkSceneIntegration.ScenePath;
        portal.transitionContext = CrosswalkSceneIntegration.Context;
        EditorUtility.SetDirty(portal);
        // Keep existing order/settings, but make the lobby the runnable entry point.
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s=>s.path==CrosswalkSceneIntegration.LobbyPath);
        scenes.Insert(0,new EditorBuildSettingsScene(CrosswalkSceneIntegration.LobbyPath,true));
        var entry = scenes.FirstOrDefault(s=>s.path==CrosswalkSceneIntegration.ScenePath);
        if(entry==null)scenes.Add(new EditorBuildSettingsScene(CrosswalkSceneIntegration.ScenePath,true));else entry.enabled=true;
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(lobby); Require(EditorSceneManager.SaveScene(lobby), "Lobby save failed");
        AssetDatabase.SaveAssets();
        return "Connected lobby, crosswalk loading, renderer, completion and PC/VR pause menu.";

        UnityEngine.UI.Button Button(string name,string line,float y,bool primary)
        {
            var rect=Rect(name,card,new Vector2(500,84),new Vector2(0,y));
            var image=Image(rect,primary?teal:cream,uiMaterial,true);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(1,1,.94f);colors.pressedColor=new Color(.78f,.84f,.77f);colors.fadeDuration=.1f;button.colors=colors;
            Text("Label",rect,line,new Vector2(454,65),Vector2.zero,32,primary?new Color32(255,249,231,255):SceneTransitionView.Ink,font,textMaterial);
            rect.gameObject.AddComponent<FirstSituationButtonFeedback>();
            return button;
        }
    }

    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.gameObject.layer=5;
        rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    static UnityEngine.UI.Image Image(RectTransform rect,Sprite sprite,Material material,bool interactive)
    {
        var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.material=material;
        image.type=sprite&&sprite.border!=Vector4.zero?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
        image.pixelsPerUnitMultiplier=interactive?6:4;image.color=Color.white;image.raycastTarget=interactive;return image;
    }
    static TMP_Text Text(string name,Transform parent,string line,Vector2 size,Vector2 position,float points,Color color,TMP_FontAsset font,Material material)
    {
        var rect=Rect(name,parent,size,position);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=font;text.fontSharedMaterial=material;text.text=line;text.color=color;text.alignment=TextAlignmentOptions.Center;
        ElevatorTypography.Apply(text,points);return text;
    }
}
