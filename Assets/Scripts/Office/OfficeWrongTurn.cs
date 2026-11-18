using System;
using System.Collections;
using ProjectWait.TeamShare;
using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    // A short, staged mistake. The desk marker's forward points OUT of its contact face.
    public sealed class OfficeWrongTurn : MonoBehaviour
    {
        [SerializeField] Transform deskEdge;
        [SerializeField] OfficeCharacterPerformance friend;
        [SerializeField] SharedFriendExpressions face;
        [SerializeField] OfficeBlackoutCinematicOverlay cover;
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip impact;
        Vector3 start;
        Quaternion facing;
        SharedFriendExpressions.Expression expression;
        float strength;
        bool checkpoint;
        public bool HasCheckpoint => checkpoint;

        public IEnumerator Play()
        {
            var actor = friend.transform;
            actor.GetPositionAndRotation(out start, out facing);
            checkpoint = true;
            friend.StagedMotion = true;
            if (face) { expression = face.expression; strength = face.strength; }
            var outward = deskEdge.forward;
            var contact = deskEdge.position + outward * .17f;
            contact.y = start.y;
            var turn = contact + outward * .94f;
            var impactFacing = Quaternion.LookRotation(-outward);
            yield return Animate(1.05f, t =>
            {
                var a = Vector3.Lerp(start, turn, t);
                var b = Vector3.Lerp(turn, contact, t);
                var heading = Quaternion.LookRotation((b - a).normalized);
                actor.SetPositionAndRotation(Vector3.Lerp(a, b, t), Quaternion.Slerp(facing, heading, Mathf.Clamp01(t * 5)));
            });
            if (impact && source) source.PlayOneShot(impact, .65f);
            if (face) face.SetExpression(SharedFriendExpressions.Expression.Surprised);
            var back = contact + outward * .20f;
            yield return Animate(.38f, t =>
            {
                float lean = Mathf.Sin(t * Mathf.PI);
                actor.SetPositionAndRotation(Vector3.Lerp(contact, back, Mathf.SmoothStep(0, 1, t)),
                    impactFacing * Quaternion.Euler(-11 * lean, 0, -5 * lean));
            });
            yield return new OfficeStoryWait(.35f);
        }

        public IEnumerator Rewind(Action showChoices)
        {
            yield return Animate(.35f, t => cover.RelocationFade = Mathf.SmoothStep(0, 1, t));
            yield return null; // present at least one fully black frame before resetting the actor
            while (OfficeStoryTime.Frozen) yield return null;
            RestoreActor();
            showChoices();
            yield return new OfficeStoryWait(.15f);
            yield return Animate(.4f, t => cover.RelocationFade = 1 - Mathf.SmoothStep(0, 1, t));
        }

        static IEnumerator Animate(float seconds, Action<float> sample)
        {
            for (float elapsed = 0; elapsed < seconds;)
            {
                yield return null;
                float dt = Mathf.Min(OfficeStoryTime.Delta, .05f);
                if (dt <= 0) continue;
                elapsed = Mathf.Min(seconds, elapsed + dt);
                sample(elapsed / seconds);
            }
        }

        void RestoreActor()
        {
            if (!checkpoint) return;
            if (friend)
            {
                friend.transform.SetPositionAndRotation(start, facing);
                friend.StagedMotion = false;
            }
            if (face) face.SetExpression(expression, strength);
            checkpoint = false;
        }
        public void Cancel()
        {
            RestoreActor();
            if (cover) cover.RelocationFade = 0;
        }
    }
}
