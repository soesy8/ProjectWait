using System;
using System.Collections;
using UnityEngine;

namespace ProjectWait.M.Shared
{
    // One timing policy for both lessons. Never scale it by walking/dialogue speed.
    public static class ComfortBlackout
    {
        public const float Visible=1.3f, Cover=.5f, Hold=.3f, Reveal=.5f;
        public const float PlacementAt=Visible+Cover;
        public const float Duration=Visible+Cover+Hold+Reveal;
        public static float Alpha(float elapsed)
        {
            if(elapsed<Visible)return 0;
            if(elapsed<PlacementAt)return Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-Visible)/Cover));
            return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-PlacementAt-Hold)/Reveal));
        }
        public static IEnumerator Relocate(Y.Crosswalk.FirstSituationUI ui,Action placement,Action<float> walk=null)
        {
            ui.SetMovementLetterbox(true);
            try
            {
                // Keep dialogue and the real locomotion/phone animation for the
                // first steps. The route skip and heading change stay fully covered.
                for(float t=0;t<Visible;)
                {
                    yield return null;
                    if(ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)continue;
                    float dt=Mathf.Min(UnityEngine.Time.deltaTime,.05f);walk?.Invoke(dt);t+=dt;
                }
                ui.Transition(true,Cover);
                while(!ui.IsFullyCovered)
                {
                    yield return null;
                    if(!ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)walk?.Invoke(Mathf.Min(UnityEngine.Time.deltaTime,.05f));
                }
                while(!ui.IsFullyCovered||ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                // Keep a rendered opaque frame before any location or heading changes.
                yield return null;
                while(ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                ui.HideAll();placement();
                for(float t=0;t<Hold;t+=UnityEngine.Time.deltaTime)yield return null;
                ui.Transition(false,Reveal);
                while(ui.IsTransitioning||ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                ExperienceSceneSettings.RequireFreshClick();
            }
            finally{if(ui){ui.Transition(false,Reveal);ui.SetMovementLetterbox(false);}}
        }
    }
}
