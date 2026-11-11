using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectWait.M.VRPreview.Editor
{
    public static class VRPreviewTools
    {
        public const string Folder = "Assets/M/VRPreview";
        public const string Work = "Assets/M/VRPreviewSupport~/";
        public const string ScenePath = "Assets/M/Scene/Wait_Elevator.unity";
        public static string Hash(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        static void Gate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Play를 중지하고 컴파일이 끝난 후 실행해 주세요.");
            if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != ScenePath || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Wait_Elevator 씬만 열고, 미저장 변경을 먼저 저장해 주세요.");
        }
        public static GameObject Root() => SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(g => g.name == "VR_Preview_Test");
        static IEnumerable<string> SafeFiles(string directory)
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("링크 폴더는 처리하지 않습니다: " + directory);
            foreach (string entry in Directory.GetFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("링크 자산은 처리하지 않습니다: " + entry);
                if ((attributes & FileAttributes.Directory) != 0) { foreach (var p in SafeFiles(entry)) yield return p; }
                else yield return entry.Replace('\\', '/');
            }
        }
        public static object RemoveRoot()
        {
            Gate(); var root = Root();
            if (root) { Undo.DestroyObjectImmediate(root); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); }
            return new { removed = !Root(), packageKept = true };
        }
        static JObject Plan()
        {
            Gate();
            var ready = JObject.Parse(File.ReadAllText(Work + "prepared-state.json"));
            var baseline = JObject.Parse(File.ReadAllText(Work + "Baseline~/manifest.json"));
            var sceneBackup = baseline["files"][ScenePath];
            if (Hash((string)sceneBackup["backup"]) != (string)sceneBackup["sha256"]) throw new IOException("메인 씬 백업이 손상되었습니다.");
            if (Hash(Folder + ".meta") != (string)ready["previewRootMetaHash"]) throw new IOException("테스트 폴더 메타가 변경되었습니다.");
            if (!File.Exists((string)ready["python"]) || !File.Exists((string)ready["unityCli"]) || Hash(Work + "finish_rollback.py") != (string)ready["finalizerHash"])
                throw new IOException("이 PC의 원복 마무리 도구(Python/Unity CLI)가 없거나 변경되었습니다.");
            var files = (JObject)ready["previewFiles"];
            if (!new HashSet<string>(SafeFiles(Folder)).SetEquals(files.Properties().Select(p => p.Name)))
                throw new IOException("테스트 폴더에 파일이 추가/삭제되었습니다. 사용자 파일을 보존하기 위해 완전 원복을 중지합니다.");
            foreach (var file in files.Properties())
                if (Hash(file.Name) != (string)file.Value) throw new IOException("테스트 파일이 수정되었습니다. 별도로 보관 후 원복해 주세요: " + file.Name);
            foreach (var entry in ((JObject)ready["guarded"]).Properties())
            {
                string path = entry.Name;
                if (path.Contains("..") || !(path == "Packages/manifest.json" || path == "Packages/packages-lock.json" || path.StartsWith("Assets/XR/")))
                    throw new IOException("허용 범위 밖의 복원 경로: " + path);
                string backup = (string)baseline["files"][path]["backup"], original = (string)baseline["files"][path]["sha256"];
                if (!backup.StartsWith(Work + "Baseline~/Files/") || Hash(backup) != original) throw new IOException("백업 손상: " + path);
                if (Hash(path) != (string)entry.Value && Hash(path) != original)
                    throw new IOException("테스트 이후 설정이 추가로 바뀌었습니다. 사용자 변경을 덮어쓰지 않습니다: " + path);
            }
            foreach (var entry in ((JObject)ready["createdXR"]).Properties())
                if (!entry.Name.StartsWith("Assets/XR/") || entry.Name.Contains("..") || Hash(entry.Name) != (string)entry.Value)
                    throw new IOException("새 XR 설정이 변경되어 삭제를 중지합니다: " + entry.Name);
            return new JObject { ["ready"] = ready, ["baseline"] = baseline, ["exactSceneRestore"] = Hash(ScenePath) == (string)ready["sceneHash"] };
        }
        public static object Rollback(bool apply = false)
        {
            var plan = Plan(); var ready = (JObject)plan["ready"]; var baseline = (JObject)plan["baseline"];
            var paths = ((JObject)ready["guarded"]).Properties().Select(p => p.Name).ToArray();
            if (!apply) return new { safe = true, writes = paths, deleteFolder = Folder, exactSceneRestore = (bool)plan["exactSceneRestore"], previewFiles = ((JObject)ready["previewFiles"]).Count };
            // A one-shot companion survives the domain reload that deletes this Editor code.
            // It restores OpenXR's auto-rewritten references only after package AND script reloads.
            File.WriteAllText(Work + "rollback-finalize.json", plan.ToString(Formatting.Indented));
            var start = new System.Diagnostics.ProcessStartInfo {
                FileName = (string)ready["python"], Arguments = "\"" + Path.GetFullPath(Work + "finish_rollback.py") + "\"",
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName, UseShellExecute = false, CreateNoWindow = true };
            var companion = System.Diagnostics.Process.Start(start);
            if (companion == null) throw new IOException("원복 마무리 프로세스를 시작하지 못했습니다.");
            companion.Dispose();
            if ((bool)plan["exactSceneRestore"])
            {
                File.Copy((string)baseline["files"][ScenePath]["backup"], ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else if (Root())
            {
                UnityEngine.Object.DestroyImmediate(Root());
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            EditorApplication.LockReloadAssemblies(); AssetDatabase.StartAssetEditing();
            try
            {
                if (!AssetDatabase.DeleteAsset(Folder)) throw new IOException("테스트 폴더 삭제 실패.");
                foreach (string path in paths) File.Copy((string)baseline["files"][path]["backup"], path, true);
                foreach (var entry in ((JObject)ready["createdXR"]).Properties())
                    if (!entry.Name.EndsWith(".meta") && File.Exists(entry.Name)) AssetDatabase.DeleteAsset(entry.Name);
            }
            finally { AssetDatabase.StopAssetEditing(); EditorApplication.UnlockReloadAssemblies(); }
            var result = new { phase = "waiting-for-editor-reload", rootRemoved = !Root(), previewFolderRemoved = !Directory.Exists(Folder), exactSceneRestore = (bool)plan["exactSceneRestore"], packageResolveRequested = true };
            File.WriteAllText(Work + "full-rollback.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            UnityEditor.PackageManager.Client.Resolve(); AssetDatabase.Refresh();
            return result;
        }
        [MenuItem("Tools/Wait/VR Preview/Select Test Object")]
        static void SelectRoot() { var root = Root(); if (root) { Selection.activeGameObject = root; EditorGUIUtility.PingObject(root); } }
        [MenuItem("Tools/Wait/VR Preview/Remove Test Object Only")]
        static void RemoveMenu() { Try(() => RemoveRoot()); }
        [MenuItem("Tools/Wait/VR Preview/Full Rollback (Remove Package)")]
        public static void RollbackMenu()
        {
            if (EditorUtility.DisplayDialog("임시 VR 테스트 완전 원복", "테스트 오브젝트·전용 폴더와 Mock HMD 패키지를 제거하고 설정을 백업으로 복원합니다. 이 PC의 Python/Unity CLI가 재컴파일 이후 원복 마무리를 수행합니다. 완료까지 Unity를 닫지 마세요. 이후 사용자 변경이 감지되면 중지합니다. 필수 백업·원복 도구는 Assets/M/VRPreviewSupport~에 남습니다.", "원복", "취소")) Try(() => Rollback(true));
        }
        static void Try(Func<object> operation)
        {
            try { operation(); }
            catch (Exception ex) { Debug.LogWarning("[Wait VR Preview] " + ex.Message); EditorUtility.DisplayDialog("원복 중지", ex.Message, "확인"); }
        }
    }
    [CustomEditor(typeof(VRPreviewTest))]
    public sealed class VRPreviewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var preview = (VRPreviewTest)target;
            EditorGUILayout.HelpBox("EditorOnly 임시 VR 테스트입니다. 오브젝트를 활성화하고 Play한 뒤 Game 뷰를 클릭하세요.\n\n마우스: 머리/손 회전 · WASD: 이동 · Q/E: 높이\nTab: 머리→왼손→오른손 · [ / ]: 손 선택\nT: 트리거 선택 · G: 그립(선택 아님)\nIJKL: 왼손 스틱 · Esc: 체험 메뉴 / 대화 다시 보기 · R: 모의 포즈 초기화\n대화는 단계에 따라 자동 표시 · 읽기 확인 후 관찰로 전환\n컨트롤러가 대화를 가리키거나 누르는 동안에는 재배치를 보류합니다.\n\n모의 착용 상태입니다. 실제 착용감·멀미·기기 성능은 검증하지 못합니다.", MessageType.Info);
            EditorGUILayout.LabelField("상태", preview.Status);
            if (EditorApplication.isPlaying) { if (GUILayout.Button("임시 VR 끄기 → PC 모드")) preview.gameObject.SetActive(false); }
            else
            {
                if (GUILayout.Button("Play로 임시 VR 보기")) EditorApplication.isPlaying = true;
                if (GUILayout.Button("패키지까지 완전 원복")) VRPreviewTools.RollbackMenu();
            }
        }
    }
}
