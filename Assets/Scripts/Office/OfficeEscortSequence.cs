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
            // Office is a stationary lesson: no visible automatic camera translation.
            const float cover = .5f, hold = .3f, reveal = .5f;
            for (Elapsed = 0; Elapsed < cover + hold + reveal;)
            {
                yield return null;
                float delta = Mathf.Min(OfficeStoryTime.Delta, .05f);
                if (delta <= 0) continue;
                Elapsed += delta;
                cinematic.RelocationFade = Elapsed < cover ? Mathf.SmoothStep(0, 1, Elapsed / cover) :
                    1 - Mathf.SmoothStep(0, 1, (Elapsed - cover - hold) / reveal);
                if (!placed && Elapsed >= cover)
                {
                    cinematic.RelocationFade = 1;
                    yield return null; // actually render the opaque frame before teleporting
                    while (OfficeStoryTime.Frozen) yield return null;
                    friend.FinishRouteUnderCover(); teacher.FinishRouteUnderCover();
                    integration.movement.RelocateFeet(arrival.position);
                    // Place the speakers in the retained gaze direction, not at old
                    // waypoints behind the headset. No tracked pose is rotated.
                    Vector3 forward = Vector3.ProjectOnPlane(integration.viewer.transform.forward, Vector3.up).normalized;
                    if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    friend.transform.position = arrival.position + forward * 1.8f - right * .8f;
                    // Stay on the near side of the office doorway, so the
                    // teacher remains visible during the final conversation.
                    teacher.transform.position = arrival.position + forward * 2.5f + right * .65f;
                    friend.transform.rotation = teacher.transform.rotation = Quaternion.LookRotation(-forward);
                    integration.presentation.ClearLine(); placed = WasRelocated = true;
                }
            }
            cinematic.RelocationFade = 0; cinematic.HideLetterbox();
            integration.EscortMoving = false; integration.RequireFreshClick(); routine = null;
            integration.director.PlayDialogue(6);
        }
        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine); routine = null;
            if (cinematic) { cinematic.RelocationFade = 0; cinematic.HideLetterbox(); }
            if (integration) integration.EscortMoving = false;
        }
    }
}
