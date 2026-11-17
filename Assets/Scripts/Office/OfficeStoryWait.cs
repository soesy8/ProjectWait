using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    // Only story clocks opt in. Menus, tracking and the shared scene loader keep
    // using real time, so pausing never strands an overlay or a loading operation.
    public static class OfficeStoryTime
    {
        public static bool Frozen => OfficeSceneIntegration.Current && OfficeSceneIntegration.Current.IsPaused ||
            ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy;
        public static float Delta => Frozen ? 0 : Time.deltaTime;
    }

    public sealed class OfficeStoryWait : CustomYieldInstruction
    {
        float remaining;
        int frame = -1;
        public OfficeStoryWait(float seconds) { remaining = Mathf.Max(0, seconds); }
        public override bool keepWaiting
        {
            get
            {
                if (frame != Time.frameCount) { frame = Time.frameCount; remaining -= OfficeStoryTime.Delta; }
                return OfficeStoryTime.Frozen || remaining > 0;
            }
        }
    }
}
