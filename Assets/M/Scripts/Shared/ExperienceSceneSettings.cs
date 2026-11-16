using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using ProjectWait.M.Elevator.DialoguePreview;
using ProjectWait.M.Crosswalk;

namespace ProjectWait.M.Shared
{
    [DefaultExecutionOrder(32000)]
    public sealed class ExperienceSceneSettings : MonoBehaviour
    {
        sealed class Voice{public AudioSource source;public float baseline,last;}
        readonly List<Voice> voices=new List<Voice>();
        float scanAt;
        ElevatorLesson lesson;
        Y.Crosswalk.FirstSituationUI crosswalkUI;
        Y.Crosswalk.FirstSituationController first;
        Y.Crosswalk.SecondSituationController second;
        Y.Crosswalk.ThirdSituationController third;
        void Start(){lesson=FindFirstObjectByType<ElevatorLesson>();crosswalkUI=FindFirstObjectByType<Y.Crosswalk.FirstSituationUI>();first=FindFirstObjectByType<Y.Crosswalk.FirstSituationController>();second=FindFirstObjectByType<Y.Crosswalk.SecondSituationController>();third=FindFirstObjectByType<Y.Crosswalk.ThirdSituationController>();ExperiencePreferences.Changed+=Refresh;Refresh();}
        void Refresh()
        {
            if(lesson&&lesson.view)lesson.view.Refresh();
            var ui=FindFirstObjectByType<Y.Crosswalk.FirstSituationUI>();if(ui)ui.ApplySharedSettings();
        }
        void LateUpdate()
        {
            ApplyCrosswalkMotion();
            if(Time.unscaledTime>=scanAt)
            {
                scanAt=Time.unscaledTime+.25f;
                foreach(var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(source.gameObject.scene!=gameObject.scene||source.GetComponent<StoryMusic>())continue;
                    if(!voices.Exists(v=>v.source==source))voices.Add(new Voice{source=source,baseline=source.volume,last=source.volume});
                }
                foreach(var haptic in FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(haptic.gameObject.scene==gameObject.scene)haptic.amplitudeMultiplier=ExperiencePreferences.Current.haptics?1:0;
            }
            for(int i=voices.Count-1;i>=0;i--)
            {
                var voice=voices[i];if(!voice.source){voices.RemoveAt(i);continue;}
                if(!Mathf.Approximately(voice.source.volume,voice.last))voice.baseline=voice.source.volume;
                voice.last=voice.baseline*ExperiencePreferences.Current.effects;voice.source.volume=voice.last;
            }
        }
        void ApplyCrosswalkMotion()
        {
            if(!crosswalkUI||!crosswalkUI.DialogueSubject){CrosswalkStoryTime.SetSafeWalkRate(1);return;}
            var friend=crosswalkUI.DialogueSubject.GetComponent<Y.Crosswalk.FriendCharacterMovement>();if(!friend||friend.IsScenarioRunning){CrosswalkStoryTime.SetSafeWalkRate(1);return;}
            // Never disturb the synchronized near-miss slow motion or unsafe runs.
            if(second&&(second.CurrentState==Y.Crosswalk.SecondSituationController.StageState.UnsafeResult||second.CurrentState==Y.Crosswalk.SecondSituationController.StageState.Retry)||
               third&&(third.CurrentState==Y.Crosswalk.ThirdSituationController.StageState.Danger||third.CurrentState==Y.Crosswalk.ThirdSituationController.StageState.Retry)){CrosswalkStoryTime.SetSafeWalkRate(1);return;}
            float rate=1;
            if(third&&third.CurrentState==Y.Crosswalk.ThirdSituationController.StageState.Crossing)rate=ExperiencePreferences.MoveSpeed/1.35f;
            else if(second&&second.CurrentState==Y.Crosswalk.SecondSituationController.StageState.Walking)rate=ExperiencePreferences.MoveSpeed/1.55f;
            else if(first&&first.CurrentState==Y.Crosswalk.FirstSituationController.SituationState.Walking)rate=ExperiencePreferences.Current.movement;
            friend.SetScenarioPlaybackRate(rate);
            CrosswalkStoryTime.SetSafeWalkRate(rate);
        }
        public static void CalibrateHeight()
        {
            var controls=FindFirstObjectByType<ElevatorDesktopControls>();if(controls){controls.RequestXRHeightCalibration();return;}
            if(CrosswalkSceneIntegration.Current)CrosswalkSceneIntegration.Current.input.RequestHeightCalibration();
            else {var lobby=FindFirstObjectByType<ProjectWait.M.TitleScene.TitlePlazaPlayer>();if(lobby)lobby.RequestHeightCalibration();}
        }
        public static void RequireFreshClick()
        {
            var controls=FindFirstObjectByType<ElevatorDesktopControls>();if(controls)controls.RequireFreshChoiceClick();
            if(CrosswalkSceneIntegration.Current)CrosswalkSceneIntegration.Current.input.RequireFreshChoiceClick();
        }
        void OnDestroy(){ExperiencePreferences.Changed-=Refresh;CrosswalkStoryTime.SetSafeWalkRate(1);}
    }
}
