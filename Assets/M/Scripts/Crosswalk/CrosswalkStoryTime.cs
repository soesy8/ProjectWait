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
        static double clock,lastUnityTime;
        static float rate=1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){clock=lastUnityTime=0;rate=1;}
        static double Now
        {
            get{double now=UnityEngine.Time.timeAsDouble;clock+=System.Math.Max(0,now-lastUnityTime)*rate;lastUnityTime=now;return clock;}
        }
        // Safe authored walks use this clock for their arrival budgets as well as
        // their playback rate. Slower comfort settings must not trigger a timeout.
        public static void SetSafeWalkRate(float value){_ = Now;rate=Mathf.Clamp(value,.08f,1);}
        public static float deltaTime => UnityEngine.Time.deltaTime;
        public static double timeAsDouble => Now;
        public static float unscaledDeltaTime => UnityEngine.Time.deltaTime;
        public static float unscaledTime => (float)Now;
        public static double unscaledTimeAsDouble => Now;
    }

    public sealed class CrosswalkStoryWait : CustomYieldInstruction
    {
        readonly float deadline;
        public CrosswalkStoryWait(float seconds) { deadline = CrosswalkStoryTime.unscaledTime + Mathf.Max(0, seconds); }
        public override bool keepWaiting => CrosswalkSceneIntegration.GameplayBlocked || CrosswalkStoryTime.unscaledTime < deadline;
    }
}
