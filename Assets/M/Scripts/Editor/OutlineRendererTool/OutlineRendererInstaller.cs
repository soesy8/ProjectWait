#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class OutlineRendererInstaller
    {
        public const string OutputFolder = "Assets/M/Arts/Rendering/AutoOutline";

        [Serializable]
        public sealed class Result
        {
            public UniversalRenderPipelineAsset pipeline;
            public string[] rendererPaths;
            public string[] createdAssets;
            public int addedFeatures;
            public bool sceneLinked;
            public string Summary => "설정 완료 · 렌더러 " + rendererPaths.Length + "개 / 새 피처 " + addedFeatures +
                "개 / 새 자산 " + createdAssets.Length + "개" + (sceneLinked ? "\n현재 씬 연결 및 저장 완료" : "\n렌더러 자산 설정 완료");
        }

        public static bool IsOwned(string path) => !string.IsNullOrEmpty(path) && path.StartsWith("Assets/M/", StringComparison.Ordinal);

        public static UniversalRenderPipelineAsset DetectPipeline(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                var owners = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ElevatorSceneRendering>(true))
                    .Where(o => o.enabled && o.gameObject.activeInHierarchy && o.scenePipeline is UniversalRenderPipelineAsset).ToArray();
                if (owners.Length > 1) throw new InvalidOperationException("씬에 렌더링 설정이 여러 개 있습니다. 사용할 URP 자산을 고급 설정에서 지정해 주세요.");
                if (owners.Length == 1) return (UniversalRenderPipelineAsset)owners[0].scenePipeline;
            }
            return (QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
        }

        static SerializedProperty Renderers(SerializedObject pipeline) => pipeline.FindProperty("m_RendererDataList");

        public static string Inspect(UniversalRenderPipelineAsset pipeline)
        {
            if (!pipeline) return "사용할 URP 자산을 찾지 못했습니다. 고급 설정에서 지정해 주세요.";
            var so = new SerializedObject(pipeline);
            var list = Renderers(so);
            int count = 0, ready = 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                var data = list.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;
                if (!data) continue;
                count++;
                if (data.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().Any(f => f.isActive && f.maskShader && f.distanceShader)) ready++;
            }
            return AssetDatabase.GetAssetPath(pipeline) + "\n3D 렌더러 " + count + "개 · 강조 피처 준비 " + ready + "개";
        }

        static void EnsureFolder(string folder)
        {
            if (folder != "Assets/M" && !IsOwned(folder + "/")) throw new InvalidOperationException("출력은 Assets/M 안으로 제한됩니다.");
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static T OwnCopy<T>(T source, string suffix, List<string> created) where T : Object
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            if (IsOwned(sourcePath)) return source;
            if (string.IsNullOrEmpty(sourcePath)) throw new InvalidOperationException("저장된 URP 자산을 선택해 주세요.");
            EnsureFolder(OutputFolder);
            var name = string.Concat(source.name.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
            var guid = AssetDatabase.AssetPathToGUID(sourcePath);
            var destination = OutputFolder + "/" + name + "_" + guid.Substring(0, 8) + "_" + suffix + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(destination);
            if (existing) return existing;
            if (AssetDatabase.LoadMainAssetAtPath(destination)) throw new InvalidOperationException("출력 경로에 다른 종류의 자산이 있습니다: " + destination);
            if (!AssetDatabase.CopyAsset(sourcePath, destination)) throw new InvalidOperationException("자산을 복사하지 못했습니다: " + sourcePath);
            created.Add(destination);
            return AssetDatabase.LoadAssetAtPath<T>(destination);
        }

        static ElevatorSilhouetteOutlineFeature ConfigureRenderer(UniversalRendererData renderer, Shader coverage, Shader distance, out bool added)
        {
            var path = AssetDatabase.GetAssetPath(renderer);
            if (!IsOwned(path)) throw new InvalidOperationException("공용 렌더러는 직접 변경하지 않습니다.");
            Undo.RecordObject(renderer, "Configure outline renderer");
            var feature = renderer.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().FirstOrDefault();
            added = !feature;
            if (!feature)
            {
                feature = AssetDatabase.LoadAllAssetsAtPath(path).OfType<ElevatorSilhouetteOutlineFeature>().FirstOrDefault();
                if (!feature)
                {
                    feature = ScriptableObject.CreateInstance<ElevatorSilhouetteOutlineFeature>();
                    feature.name = "Mask silhouette outline";
                    Undo.RegisterCreatedObjectUndo(feature, "Add outline renderer feature");
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                }
                renderer.rendererFeatures.Add(feature);
            }
            Undo.RecordObject(feature, "Configure outline shaders");
            feature.maskShader = coverage;
            feature.distanceShader = distance;
            feature.SetActive(true);
            foreach (var duplicate in renderer.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().Where(f => f != feature))
            {
                Undo.RecordObject(duplicate, "Disable duplicate outline feature");
                duplicate.SetActive(false);
                EditorUtility.SetDirty(duplicate);
            }
            var so = new SerializedObject(renderer);
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                long id = 0;
                if (renderer.rendererFeatures[i]) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string _, out id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
            return feature;
        }

        public static Result Install(UniversalRenderPipelineAsset source, Scene scene, bool connectScene = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Mode를 종료한 뒤 설정해 주세요.");
            if (!source) throw new InvalidOperationException("URP 자산을 먼저 지정해 주세요.");
            if (connectScene && (!scene.IsValid() || !scene.isLoaded || !IsOwned(scene.path)))
                throw new InvalidOperationException("연결할 씬을 Assets/M 안에 먼저 저장해 주세요. 자산만 설정하려면 ‘현재 씬 연결’을 해제하세요.");
            var graph = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            if (graph == null || graph.enableRenderCompatibilityMode)
                throw new InvalidOperationException("이 효과는 URP Render Graph가 필요합니다. Graphics 설정에서 Compatibility Mode를 해제해야 합니다.");
            var coverage = Shader.Find("ProjectWait/M/Outline Coverage");
            var distance = Shader.Find("ProjectWait/M/Outline Distance");
            if (!coverage || !distance) throw new InvalidOperationException("강조 셰이더 2개를 함께 가져와 주세요.");
            if (ShaderUtil.ShaderHasError(coverage) || ShaderUtil.ShaderHasError(distance)) throw new InvalidOperationException("강조 셰이더 컴파일 오류를 먼저 해결해 주세요.");
            var original = new SerializedObject(source);
            var originals = Renderers(original);
            int defaultIndex = original.FindProperty("m_DefaultRendererIndex").intValue;
            if (originals == null || defaultIndex < 0 || defaultIndex >= originals.arraySize || !(originals.GetArrayElementAtIndex(defaultIndex).objectReferenceValue is UniversalRendererData))
                throw new InvalidOperationException("기본 렌더러가 Universal 3D Renderer인 URP 자산을 선택해 주세요.");
            var owners = connectScene ? scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ElevatorSceneRendering>(true)).ToArray() : Array.Empty<ElevatorSceneRendering>();
            if (owners.Length > 1) throw new InvalidOperationException("씬에 렌더링 설정 컴포넌트가 여러 개 있습니다. 하나만 사용하도록 정리해 주세요.");
            if (owners.Length == 1 && (!owners[0].enabled || !owners[0].gameObject.activeInHierarchy)) throw new InvalidOperationException("씬의 렌더링 설정 오브젝트를 활성화해 주세요.");

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install mask silhouette renderer");
            var created = new List<string>();
            var pipeline = OwnCopy(source, "OutlinePipeline", created);
            var so = new SerializedObject(pipeline);
            var renderers = Renderers(so);
            var configured = new Dictionary<UniversalRendererData, UniversalRendererData>();
            int addedFeatures = 0;
            for (int i = 0; i < renderers.arraySize; i++)
            {
                var data = renderers.GetArrayElementAtIndex(i).objectReferenceValue as UniversalRendererData;
                if (!data) continue;
                if (!configured.TryGetValue(data, out var owned))
                {
                    owned = OwnCopy(data, "OutlineRenderer", created);
                    ConfigureRenderer(owned, coverage, distance, out bool added);
                    if (added) addedFeatures++;
                    configured.Add(data, owned);
                }
                renderers.GetArrayElementAtIndex(i).objectReferenceValue = owned;
            }
            Undo.RecordObject(pipeline, "Connect outline renderers");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssetIfDirty(pipeline);
            if (connectScene)
            {
                ElevatorSceneRendering owner;
                if (owners.Length == 1) owner = owners[0];
                else
                {
                    var go = new GameObject("Highlight Rendering");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    Undo.RegisterCreatedObjectUndo(go, "Create scene rendering settings");
                    owner = Undo.AddComponent<ElevatorSceneRendering>(go);
                }
                Undo.RecordObject(owner, "Connect scene outline pipeline");
                owner.scenePipeline = pipeline;
                EditorUtility.SetDirty(owner);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("씬을 저장하지 못했습니다: " + scene.path);
            }
            else Undo.CollapseUndoOperations(undoGroup);
            return new Result
            {
                pipeline = pipeline,
                rendererPaths = configured.Values.Distinct().Select(AssetDatabase.GetAssetPath).ToArray(),
                createdAssets = created.ToArray(),
                addedFeatures = addedFeatures,
                sceneLinked = connectScene
            };
        }
    }
}
#endif
