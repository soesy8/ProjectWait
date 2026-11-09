#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectWait.M.Tests
{
    // No compile-time dependency on Assembly-CSharp or a duplicate runtime model.
    public static class RuntimeAccess
    {
        public const string Trial = "ProjectWait.M.Elevator.ObservationTrial.";
        public const string ScenePath = "Assets/M/Scene/Elevator_Obsevation_Choice_Test.unity";
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static;

        public static Type Type(string name)
        {
            var matches = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name, false)).Where(t => t != null).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one real runtime type: " + name);
            return matches[0];
        }

        public static object Get(object target, string name)
        {
            var type = target as Type ?? target.GetType();
            var instance = target is Type ? null : target;
            var field = type.GetField(name, Flags);
            if (field != null) return field.GetValue(instance);
            var property = type.GetProperty(name, Flags);
            if (property != null) return property.GetValue(instance);
            throw new MissingMemberException(type.FullName, name);
        }

        public static T Get<T>(object target, string name) => (T)Get(target, name);
        public static void Set(object target, string name, object value)
        {
            var type = target as Type ?? target.GetType();
            var instance = target is Type ? null : target;
            var field = type.GetField(name, Flags);
            if (field != null) { field.SetValue(instance, value); return; }
            var property = type.GetProperty(name, Flags);
            if (property == null) throw new MissingMemberException(type.FullName, name);
            property.SetValue(instance, value);
        }

        public static object Call(object target, string name, params object[] arguments)
        {
            var type = target as Type ?? target.GetType();
            var candidates = type.GetMethods(Flags).Where(m => m.Name == name &&
                m.GetParameters().Length >= arguments.Length &&
                m.GetParameters().Skip(arguments.Length).All(p => p.IsOptional) &&
                m.GetParameters().Take(arguments.Length).Select((p, i) =>
                    arguments[i] == null || p.ParameterType.IsInstanceOfType(arguments[i])).All(v => v)).ToArray();
            if (candidates.Length != 1)
                throw new MissingMethodException(type.FullName, name + " (unambiguous overload)");
            var method = candidates[0];
            var values = method.GetParameters().Select((p, i) =>
                i < arguments.Length ? arguments[i] : p.DefaultValue).ToArray();
            try { return method.Invoke(target is Type ? null : target, values); }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public static object Clue(string name) => Enum.Parse(Type(Trial + "ElevatorClue"), name);
        public static float Seconds(object settings, string field) => Get<float>(settings, field) + .01f;

        public static void RequireCleanEditorScenes()
        {
            if (Application.isPlaying) return;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                RequireSavedEditorScene(scene);
            }
        }
        static void RequireSavedEditorScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("Save or close the dirty/unsaved scene before regressions: " + scene.name);
        }
    }

    public sealed class GlobalState : IDisposable
    {
        readonly int fps = Application.targetFrameRate, sync = QualitySettings.vSyncCount;
        readonly RenderPipelineAsset pipeline = QualitySettings.renderPipeline;
        readonly float scale = Time.timeScale, fixedDelta = Time.fixedDeltaTime, volume = AudioListener.volume;
        readonly bool audioPause = AudioListener.pause, visible = Cursor.visible, background = Application.runInBackground;
        readonly CursorLockMode cursor = Cursor.lockState;

        public void Dispose()
        {
            QualitySettings.renderPipeline = pipeline;
            Application.targetFrameRate = fps;
            QualitySettings.vSyncCount = sync;
            Time.timeScale = scale;
            Time.fixedDeltaTime = fixedDelta;
            AudioListener.pause = audioPause;
            AudioListener.volume = volume;
            Application.runInBackground = background;
            Cursor.lockState = cursor;
            Cursor.visible = visible;
        }
    }
}
#endif
