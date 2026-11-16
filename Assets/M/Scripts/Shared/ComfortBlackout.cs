using System;
using System.Collections;
using UnityEngine;

namespace ProjectWait.M.Shared
{
    // One timing policy for both lessons. Never scale it by walking/dialogue speed.
    public static class ComfortBlackout
    {
        public const float Cover=.35f, Hold=.25f, Reveal=.45f;
        public const float Duration=Cover+Hold+Reveal;
        public static float Alpha(float elapsed)
        {
            if(elapsed<Cover)return Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Cover));
            return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-Cover-Hold)/Reveal));
        }
        public static IEnumerator Relocate(Y.Crosswalk.FirstSituationUI ui,Action placement)
        {
            ui.HideAll();ui.SetMovementLetterbox(false);ui.Transition(true,Cover);
            try
            {
                while(!ui.IsFullyCovered||ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                // Keep a rendered opaque frame before any location or heading changes.
                yield return null;
                while(ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                placement();
                for(float t=0;t<Hold;t+=UnityEngine.Time.deltaTime)yield return null;
                ui.Transition(false,Reveal);
                while(ui.IsTransitioning||ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.GameplayBlocked)yield return null;
                ExperienceSceneSettings.RequireFreshClick();
            }
            finally{if(ui)ui.Transition(false,Reveal);}
        }
    }
}
