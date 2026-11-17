using UnityEngine;
using ProjectWait.TeamShare;
using ProjectWait.M.Shared;

namespace ProjectWait.YB.OfficeBlackout
{
    public sealed class OfficeBlackoutFeedback : MonoBehaviour
    {
        public OfficeBlackoutDirector director;
        public SharedFriendExpressions friend;
        public AudioSource source;
        public AudioClip paper, powerOff, confirm, arrival, complete;

        void OnEnable() { director.StepChanged += OnStep; }
        void OnDisable() { if (director) director.StepChanged -= OnStep; }
        void OnStep(OfficeBlackoutFlow.Step step)
        {
            if (friend) friend.expression = step switch
            {
                OfficeBlackoutFlow.Step.BlackoutReaction => SharedFriendExpressions.Expression.Surprised,
                OfficeBlackoutFlow.Step.CalmFriend => SharedFriendExpressions.Expression.Worried,
                OfficeBlackoutFlow.Step.KeepFriendInPlace or OfficeBlackoutFlow.Step.WaitForTeacher => SharedFriendExpressions.Expression.Neutral,
                _ => SharedFriendExpressions.Expression.Happy
            };
            AudioClip clip = step switch
            {
                OfficeBlackoutFlow.Step.BlackoutReaction => powerOff,
                OfficeBlackoutFlow.Step.KeepFriendInPlace or OfficeBlackoutFlow.Step.WaitForTeacher => confirm,
                OfficeBlackoutFlow.Step.TeacherArrives => arrival,
                OfficeBlackoutFlow.Step.Complete => complete,
                _ => null
            };
            if (step == OfficeBlackoutFlow.Step.BlackoutReaction && paper && source)
                source.PlayOneShot(paper, .5f);
            // The shared scene settings apply the effects slider to the source itself.
            if (clip && source) source.PlayOneShot(clip, .6f);
        }
    }
}
