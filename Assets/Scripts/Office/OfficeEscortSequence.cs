using System.Collections;
using UnityEngine;
using ProjectWait.M.Shared;

namespace ProjectWait.YB.OfficeBlackout
{
    public sealed class OfficeEscortSequence : MonoBehaviour
    {
        public OfficeSceneIntegration integration;
        public OfficeBlackoutCinematicOverlay cinematic;
        public OfficeBlackoutWaypointMover friend, teacher;
        public Transform arrival;
        public bool WasRelocated { get; private set; }
        public float Elapsed { get; private set; }
        Coroutine routine;
        public void Begin() { if (routine == null) routine = StartCoroutine(Travel()); }
        IEnumerator Travel()
        {
            integration.EscortMoving = true; cinematic.ShowLetterbox();
            integration.presentation.ClearLine();
            bool placed = false;
            // Identical visible-walk / cover / opaque hold / reveal policy to the
            // other lessons. Never apply scene placement in a merely fading frame.
            for (Elapsed = 0; Elapsed < ComfortBlackout.Duration;)
            {
                yield return null;
                float delta = Mathf.Min(OfficeStoryTime.Delta, .05f);
                if (delta <= 0) continue;
                Elapsed += delta; cinematic.RelocationFade = ComfortBlackout.Alpha(Elapsed);
                if (Elapsed < ComfortBlackout.PlacementAt)
                {
                    Vector3 forward = Vector3.ProjectOnPlane(teacher.transform.position - integration.movement.transform.position, Vector3.up).normalized;
                    integration.movement.MoveGuided(forward * ExperiencePreferences.MoveSpeed * delta);
                }
                if (!placed && Elapsed >= ComfortBlackout.PlacementAt)
                {
                    cinematic.RelocationFade = 1;
                    yield return null; // actually render the opaque frame before teleporting
                    while (OfficeStoryTime.Frozen) yield return null;
                    friend.FinishRouteUnderCover(); teacher.FinishRouteUnderCover();
                    integration.movement.RelocateFeet(arrival.position);
                    integration.presentation.ClearLine(); placed = WasRelocated = true;
                }
            }
            cinematic.RelocationFade = 0; cinematic.HideLetterbox();
            integration.EscortMoving = false; integration.RequireFreshClick(); routine = null;
        }
        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine); routine = null;
            if (cinematic) { cinematic.RelocationFade = 0; cinematic.HideLetterbox(); }
            if (integration) integration.EscortMoving = false;
        }
    }
}
