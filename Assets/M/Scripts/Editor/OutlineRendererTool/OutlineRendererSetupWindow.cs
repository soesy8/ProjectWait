#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace ProjectWait.M.Editor
{
    public sealed class OutlineRendererSetupWindow : EditorWindow
    {
        const string Layout = "Assets/M/Scripts/Editor/OutlineRendererTool/OutlineRendererSetupWindow.uxml";
        [SerializeField] UniversalRenderPipelineAsset pipeline;
        [SerializeField] bool connectScene = true;
        ObjectField pipelineField;
        Label status;
        Button install;
        public OutlineRendererInstaller.Result LastResult { get; private set; }

        [MenuItem("Tools/Wait/강조 세팅")]
        public static void Open()
        {
            var window = GetWindow<OutlineRendererSetupWindow>();
            window.titleContent = new GUIContent("강조 세팅");
            window.minSize = new Vector2(440, 360);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Layout);
            if (!tree) { rootVisualElement.Add(new HelpBox("설정 창의 UXML 파일을 함께 가져와 주세요.", HelpBoxMessageType.Error)); return; }
            tree.CloneTree(rootVisualElement);
            pipelineField = rootVisualElement.Q<ObjectField>("pipeline");
            pipelineField.objectType = typeof(UniversalRenderPipelineAsset);
            pipelineField.allowSceneObjects = false;
            pipelineField.SetValueWithoutNotify(pipeline);
            pipelineField.RegisterValueChangedCallback(e => { pipeline = e.newValue as UniversalRenderPipelineAsset; RefreshStatus(); });
            var toggle = rootVisualElement.Q<Toggle>("connectScene");
            toggle.SetValueWithoutNotify(connectScene);
            toggle.RegisterValueChangedCallback(e => { connectScene = e.newValue; RefreshStatus(); });
            status = rootVisualElement.Q<Label>("status");
            install = rootVisualElement.Q<Button>("install");
            install.clicked += Apply;
            rootVisualElement.Q<Button>("detect").clicked += Detect;
            rootVisualElement.Q<Button>("inspect").clicked += RefreshStatus;
            if (!pipeline) Detect(); else RefreshStatus();
        }

        void Detect()
        {
            try
            {
                pipeline = OutlineRendererInstaller.DetectPipeline(EditorSceneManager.GetActiveScene());
                pipelineField.SetValueWithoutNotify(pipeline);
                RefreshStatus();
            }
            catch (Exception e) { status.text = e.Message; install.SetEnabled(false); }
        }

        void RefreshStatus()
        {
            if (status == null) return;
            status.text = OutlineRendererInstaller.Inspect(pipeline);
            bool sceneReady = !connectScene || OutlineRendererInstaller.IsOwned(EditorSceneManager.GetActiveScene().path);
            if (!sceneReady) status.text += "\n씬 연결은 Assets/M 안에 저장된 씬에서 사용할 수 있습니다.";
            install.SetEnabled(pipeline && sceneReady && !EditorApplication.isPlayingOrWillChangePlaymode);
        }

        void Apply()
        {
            try
            {
                LastResult = OutlineRendererInstaller.Install(pipeline, EditorSceneManager.GetActiveScene(), connectScene);
                pipeline = LastResult.pipeline;
                pipelineField.SetValueWithoutNotify(pipeline);
                status.text = LastResult.Summary + "\n" + AssetDatabase.GetAssetPath(pipeline);
                EditorGUIUtility.PingObject(pipeline);
            }
            catch (Exception e) { status.text = "설정하지 못했습니다.\n" + e.Message; }
        }
        void OnFocus() { if (status != null) RefreshStatus(); }
        void OnEnable() { EditorApplication.playModeStateChanged += PlayStateChanged; }
        void OnDisable() { EditorApplication.playModeStateChanged -= PlayStateChanged; }
        void PlayStateChanged(PlayModeStateChange state) { RefreshStatus(); }
    }
}
#endif
