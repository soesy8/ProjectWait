using UnityEngine;
using ProjectWait.M.Shared;
using ProjectWait.M.SceneTransition;
using Y.Crosswalk;

namespace ProjectWait.M.Crosswalk
{
    // Preserve the lesson's failure pause without owning/restarting the shared music.
    public sealed class CrosswalkMusicState : MonoBehaviour
    {
        public FirstSituationController first;
        public SecondSituationController second;
        public ThirdSituationController third;
        void Update()
        {
            bool failed=first&&first.isActiveAndEnabled&&(first.CurrentState==FirstSituationController.SituationState.Accident||first.CurrentState==FirstSituationController.SituationState.Retry)||
                second&&second.isActiveAndEnabled&&(second.CurrentState==SecondSituationController.StageState.UnsafeResult||second.CurrentState==SecondSituationController.StageState.Retry)||
                third&&third.isActiveAndEnabled&&(third.CurrentState==ThirdSituationController.StageState.Danger||third.CurrentState==ThirdSituationController.StageState.Retry);
            StoryMusic.SetPaused(this,failed&&!StorybookSceneTransition.IsBusy);
        }
        void OnDisable(){StoryMusic.SetPaused(this,false);}
    }
}
