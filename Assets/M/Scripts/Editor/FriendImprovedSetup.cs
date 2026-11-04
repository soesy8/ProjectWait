#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class FriendImprovedSetup
    {
        const string Model = "Assets/M/Arts/Models/FriendImproved/Wait_Friend_Improved.fbx";
        const string MaterialPath = "Assets/M/Arts/Materials/FriendImproved/Friend_Atlas_Toon.mat";
        const string PrefabPath = "Assets/M/Prefabs/FriendImproved/Wait_Friend_Improved.prefab";
        const string QA = "Assets/M/CodexTemp/FriendImprovement_20261104~/";
        static readonly string[] GameScenes = { "Assets/M/Scene/Elevator_Safety_Prototype.unity", "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity" };

        [MenuItem("Wait/M/Friend Improved/Build Assets")]
        public static void BuildAssets()
        {
            EnsureEditMode();
            Directory.CreateDirectory("Assets/M/Arts/Materials/FriendImproved");
            Directory.CreateDirectory("Assets/M/Prefabs/FriendImproved");
            Directory.CreateDirectory("Assets/M/Arts/Models/FriendImproved/Animations");
            AssetDatabase.Refresh();
            string texturePath = "Assets/M/Arts/Textures/FriendImprovedPalette.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true; ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Point; ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            var mi = (ModelImporter)AssetImporter.GetAtPath(Model);
            mi.globalScale = 1; mi.useFileScale = true;
            mi.bakeAxisConversion = true;
            mi.importBlendShapes = true; mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importBlendShapeNormals = ModelImporterNormals.Calculate;
            mi.optimizeGameObjects = false;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
            mi.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(Shader.Find("ProjectWait/M/Friend Atlas Toon"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = Shader.Find("ProjectWait/M/Friend Atlas Toon");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ShadeColor", Hex("BDC8C3"));
            material.SetColor("_OutlineColor", Hex("493327"));
            material.SetFloat("_MidTone", .90f);
            material.SetFloat("_ShadowThreshold", .02f);
            material.SetFloat("_LightThreshold", .62f);
            material.SetFloat("_BandSoftness", .012f);
            material.SetFloat("_OutlineWidth", 2.4f);
            material.SetFloat("_EmissionStrength", 0);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            foreach (var renderer in imported.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var slot in renderer.sharedMaterials.Where(m => m))
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), slot.name), material);
            mi.SaveAndReimport();
            var root = new GameObject("Wait_Friend_Improved");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var bones = root.GetComponentsInChildren<Transform>(true);
            foreach (var suffix in new[] { "L", "R" })
            {
                var upper = bones.First(t => t.name == "upper_arm." + suffix);
                var lower = bones.First(t => t.name == "forearm." + suffix);
                float side = Mathf.Sign(lower.position.x - upper.position.x);
                upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, new Vector3(side * .38f, -.93f, 0).normalized) * upper.rotation;
            }
            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinned)
            {
                renderer.sharedMaterials = new[] { material };
                renderer.updateWhenOffscreen = true;
                renderer.localBounds = new Bounds(new Vector3(0, .75f, 0), new Vector3(2.0f, 2, 1.6f));
            }
            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var clip = new AnimationClip { name = "Friend_Improved_Idle", frameRate = 30 };
            foreach (var bone in bones.Where(t => t != root.transform && !t.GetComponent<Renderer>()))
            {
                var path = AnimationUtility.CalculateTransformPath(bone, model.transform);
                if (string.IsNullOrEmpty(path) || !bone.IsChildOf(model.transform)) continue;
                var q = bone.localRotation;
                for (int axis = 0; axis < 4; axis++)
                    clip.SetCurve(path, typeof(Transform), "localRotation." + "xyzw"[axis], AnimationCurve.Linear(0, q[axis], 2, q[axis]));
            }
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string clipPath = "Assets/M/Arts/Models/FriendImproved/Animations/Friend_Improved_Idle.anim";
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (savedClip) { EditorUtility.CopySerialized(clip, savedClip); Object.DestroyImmediate(clip); }
            else { AssetDatabase.CreateAsset(clip, clipPath); savedClip = clip; }
            string controllerPath = "Assets/M/Arts/Models/FriendImproved/Animations/Friend_Improved.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.FirstOrDefault().state;
            if (!state) state = machine.AddState("Idle");
            state.motion = savedClip; machine.defaultState = state;
            animator.runtimeAnimatorController = controller;
            var expressions = root.AddComponent<FriendImprovedExpressions>(); expressions.face = skinned.Single();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            File.WriteAllText(QA + "unity_asset_validation.json", InspectPrefab());
        }

        [MenuItem("Wait/M/Friend Improved/Apply To M Elevator Scenes")]
        public static void ApplyToScenes()
        {
            EnsureEditMode();
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Preserve unsaved scene edits before applying the Friend model.");
            var original = EditorSceneManager.GetSceneManagerSetup();
            foreach (var path in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lesson = Object.FindFirstObjectByType<ElevatorLesson>();
                var oldModel = lesson.friend.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.transform).First();
                Transform actor = oldModel;
                while (actor.parent != lesson.friend) actor = actor.parent;
                var position = actor.localPosition; var rotation = actor.localRotation;
                var phone = actor.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Cabin_Optional119Phone");
                if (phone) phone.SetParent(lesson.friend, true);
                Object.DestroyImmediate(actor.gameObject);
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                replacement.transform.SetParent(lesson.friend, false);
                replacement.transform.localPosition = position;
                replacement.transform.localRotation = rotation;
                // The new source is authored at the game's 1.48m height.
                replacement.transform.localScale = Vector3.one;
                var pelvis = replacement.GetComponentsInChildren<Transform>(true).First(t => t.name == "pelvis");
                if (phone) phone.SetParent(pelvis, true);
                var outline = lesson.friend.GetComponent<ElevatorOutlineTarget>();
                if (outline)
                {
                    outline.geometry = new[] { new ElevatorOutlineGeometry { sourceRenderer = replacement.GetComponentInChildren<SkinnedMeshRenderer>() } };
                    EditorUtility.SetDirty(outline);
                }
                var performance = lesson.GetComponent<ElevatorCharacterPerformance>();
                if (performance)
                {
                    performance.faceRenderer = replacement.GetComponentInChildren<SkinnedMeshRenderer>();
                    performance.normalFaceMesh = null; performance.panicFaceMesh = null;
                    performance.panicFace = null; performance.spiralEyes = Array.Empty<Transform>();
                    EditorUtility.SetDirty(performance);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed: " + path);
            }
            EditorSceneManager.RestoreSceneManagerSetup(original);
            File.WriteAllText(QA + "scene_integration.txt", string.Join("\n", GameScenes));
        }

        [MenuItem("Wait/M/Friend Improved/Build Showcase")]
        public static void BuildShowcase()
        {
            EnsureEditMode();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Friend_Improved_Showcase";
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            actor.GetComponent<FriendImprovedExpressions>().followElevatorLesson = false;
            var light = new GameObject("Warm Key").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(35, -35, 0); light.intensity = 1.1f;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Hex("FFF0C5") * .65f;
            var camera = new GameObject("Showcase Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = .88f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex("F0DFC0");
            camera.nearClipPlane = .03f; camera.farClipPlane = 50;
            camera.transform.position = new Vector3(2.1f, 1.25f, 3.4f);
            camera.transform.LookAt(new Vector3(0, .75f, 0));
            EditorSceneManager.SaveScene(scene, "Assets/M/Scene/Friend_Improved_Showcase.unity");
            Capture(camera, QA + "unity_threequarter.png");
            camera.transform.position = new Vector3(0, 1.0f, 4); camera.transform.LookAt(new Vector3(0, .75f, 0));
            Capture(camera, QA + "unity_front.png");
            camera.transform.position = new Vector3(-2.1f, 1.25f, -3.4f); camera.transform.LookAt(new Vector3(0, .75f, 0));
            Capture(camera, QA + "unity_back.png");
            camera.transform.position = new Vector3(2.1f, 1.25f, 3.4f); camera.transform.LookAt(new Vector3(0, .75f, 0));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        public static string InspectPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mesh = renderers.Single().sharedMesh;
            return JsonUtility.ToJson(new Validation {
                skinnedRenderers = renderers.Length, subMeshes = mesh.subMeshCount,
                materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().Count(),
                vertices = mesh.vertexCount, triangles = (int)(mesh.GetIndexCount(0) / 3),
                bones = renderers.Single().bones.Length,
                blendshapes = Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray(),
                missingBoneReferences = renderers.Single().bones.Count(b => !b),
                shaderErrors = ShaderUtil.ShaderHasError(renderers.Single().sharedMaterial.shader)
            }, true);
        }
        [Serializable] class Validation { public int skinnedRenderers, subMeshes, materials, vertices, triangles, bones, missingBoneReferences; public string[] blendshapes; public bool shaderErrors; }
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static void EnsureEditMode() { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
        public static void Capture(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(1000, 1000, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active; var oldTarget = camera.targetTexture;
            try {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var texture = new Texture2D(1000, 1000, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            } finally { camera.targetTexture = oldTarget; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
    }
}
#endif
