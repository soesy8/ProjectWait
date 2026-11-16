using UnityEngine;

namespace ProjectWait.M.Crosswalk
{
    /// <summary>
    /// Clock adapter for the imported lesson. Original 'unscaled' calls keep
    /// playing during per-character observation freezes, but must stop for the
    /// global pause menu and loading. Only the lesson opts in through aliases;
    /// transition UI, menu feedback and headset tracking still use Unity time.
    /// </summary>
    public static class CrosswalkStoryTime
    {
        public static float deltaTime => UnityEngine.Time.deltaTime;
        public static double timeAsDouble => UnityEngine.Time.timeAsDouble;
        public static float unscaledDeltaTime => UnityEngine.Time.deltaTime;
        public static float unscaledTime => UnityEngine.Time.time;
        public static double unscaledTimeAsDouble => UnityEngine.Time.timeAsDouble;
    }

    public sealed class CrosswalkStoryWait : CustomYieldInstruction
    {
        readonly float deadline;
        public CrosswalkStoryWait(float seconds) { deadline = UnityEngine.Time.time + Mathf.Max(0, seconds); }
        public override bool keepWaiting => CrosswalkSceneIntegration.GameplayBlocked || UnityEngine.Time.time < deadline;
    }
}
