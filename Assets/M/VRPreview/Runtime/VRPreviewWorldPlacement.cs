#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using ProjectWait.M.Elevator.DialoguePreview;

namespace ProjectWait.M.VRPreview
{
    // Event-time placement only. No head-follow interpolation and no unchecked
    // forward fallback when the room cannot contain the requested reading area.
    public static class VRPreviewWorldPlacement
    {
        public struct Placement
        {
            public Vector3 position;
            public Quaternion rotation;
            public string anchor;
        }
        static readonly Collider[] hits = new Collider[128];
        static readonly List<Bounds> protectedBounds = new List<Bounds>();
        static readonly List<ElevatorUIPlacement.Region> protectedScreen = new List<ElevatorUIPlacement.Region>();
        public static Vector3 LevelForward(Vector3 forward, Vector3 previous)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (forward.sqrMagnitude < .01f) forward = Vector3.ProjectOnPlane(previous, Vector3.up);
            return forward.sqrMagnitude > .01f ? forward.normalized : Vector3.forward;
        }
        static void Collect(ElevatorLesson lesson)
        {
            protectedBounds.Clear();
            ElevatorUIPlacement.Collect(lesson, protectedScreen);
            if (lesson.friend && lesson.friend.gameObject.activeInHierarchy)
                protectedBounds.Add(new Bounds(lesson.friend.position + Vector3.up * 1.24f, new Vector3(.66f, .78f, .66f)));
            foreach (var observation in lesson.CollectObservations())
            {
                if (!observation || !observation.gameObject.activeInHierarchy) continue;
                var collider = observation.GetComponent<Collider>();
                if (!collider || !collider.enabled) continue;
                switch (observation.clue)
                {
                    case ElevatorClue.Intercom: case ElevatorClue.FloorButtons:
                    case ElevatorClue.FrontIntercom: case ElevatorClue.FrontFloorButtons:
                    case ElevatorClue.Identity: case ElevatorClue.Door:
                        var b = collider.bounds; b.Expand(.04f); protectedBounds.Add(b); break;
                    case ElevatorClue.Rescuer:
                        protectedBounds.Add(new Bounds(observation.transform.position + Vector3.up * 1.52f, new Vector3(.48f, .55f, .45f))); break;
                }
            }
        }
        static Vector3 Corner(Vector3 position, Quaternion rotation, Vector2 size, int i)
            => position + rotation * new Vector3((i & 1) == 0 ? -size.x * .5f : size.x * .5f, (i & 2) == 0 ? -size.y * .5f : size.y * .5f, 0);
        public static bool Clear(Vector3 eye, Placement pose, Vector2 size)
        {
            if (!float.IsFinite(pose.position.x) || !float.IsFinite(pose.position.y) || !float.IsFinite(pose.position.z)) return false;
            int count = Physics.OverlapBoxNonAlloc(pose.position, new Vector3(size.x * .5f + .015f, size.y * .5f + .015f, .025f), hits,
                pose.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count != 0) return false;
            var aabb = new Bounds(pose.position, Vector3.zero);
            for (int i = 0; i < 4; i++) aabb.Encapsulate(Corner(pose.position, pose.rotation, size, i));
            foreach (var area in protectedBounds) if (aabb.Intersects(area)) return false;
            if ((pose.position - eye).sqrMagnitude < .04f) return false;
            // Each interval box contains its two endpoint cross-sections and
            // their convex hull, covering every eye-to-card segment without gaps.
            // Narrow near-eye boxes avoid the full-card box's excess volume.
            var localEye = Quaternion.Inverse(pose.rotation) * (eye - pose.position);
            for (int interval = 0; interval < 8; interval++)
            {
                float t0 = interval / 8f, t1 = (interval + 1) / 8f;
                var min = Vector3.one * float.MaxValue;
                var max = Vector3.one * float.MinValue;
                for (int i = 0; i < 4; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? -size.x * .5f : size.x * .5f, (i & 2) == 0 ? -size.y * .5f : size.y * .5f, 0);
                    var start = Vector3.Lerp(localEye, corner, t0);
                    var end = Vector3.Lerp(localEye, corner, t1);
                    min = Vector3.Min(min, Vector3.Min(start, end));
                    max = Vector3.Max(max, Vector3.Max(start, end));
                }
                count = Physics.OverlapBoxNonAlloc(pose.position + pose.rotation * ((min + max) * .5f),
                    (max - min) * .5f + Vector3.one * .005f, hits, pose.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (count != 0) return false;
            }
            return true;
        }
        public static bool Validate(ElevatorLesson lesson, Placement pose, Vector2 size)
        {
            Physics.SyncTransforms(); Collect(lesson);
            return Clear(lesson.playerCamera.transform.position, pose, size) && ScreenClear(lesson.playerCamera, pose, size);
        }
        public static bool TryPlace(ElevatorLesson lesson, Vector2 size, Vector3 forward, float preferredDistance, bool manual, out Placement result)
        {
            result = default;
            var eye = lesson.playerCamera.transform.position;
            forward = LevelForward(forward, lesson.playerCamera.transform.parent.forward);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            Physics.SyncTransforms(); Collect(lesson);
            float sign = lesson.friend && Vector3.Dot(lesson.friend.position - eye, right) >= 0 ? -1 : 1;
            // A crowded front must not invalidate the safe side reading space.
            // Manual requests prefer front candidates but retain checked sides.
            var angles = manual ? new[] { 0f, -12, 12, -24, 24, -36, 36, -46, 46, -58, 58, -66, 66 } : new[] { sign * 34, -sign * 34, sign * 46, -sign * 46, sign * 58, -sign * 58, sign * 66, -sign * 66 };
            var distances = new[] { Mathf.Clamp(preferredDistance, .9f, 1.6f), 1.1f, .9f };
            var heights = new[] { -.24f, -.38f, -.50f, -.60f, -.08f, .08f };
            float best = float.PositiveInfinity;
            foreach (float distance in distances) foreach (float angle in angles) foreach (float height in heights)
            {
                var direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                var p = new Placement { position = eye + direction * distance + Vector3.up * height, rotation = Quaternion.LookRotation(direction, Vector3.up), anchor = manual ? "수동 읽기 위치" : "검사된 측면 앵커" };
                if (!Clear(eye, p, size) || !ScreenClear(lesson.playerCamera, p, size)) continue;
                float score = Mathf.Abs(angle - (manual ? 0 : sign * 34)) * .02f + Mathf.Abs(distance - preferredDistance) + Mathf.Abs(height + .24f);
                if (score >= best) continue;
                best = score; result = p;
            }
            // Speaker-side candidates belong to the scene, not the current head pitch.
            if (!manual && lesson.friend && lesson.friend.gameObject.activeInHierarchy)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var point = lesson.friend.position + Vector3.up * 1.05f + right * (side * .72f);
                    var delta = point - eye; var horizontal = LevelForward(delta, forward);
                    if (delta.magnitude < .8f || delta.magnitude > 1.9f || Vector3.Dot(horizontal, forward) < .25f) continue;
                    var p = new Placement { position = point, rotation = Quaternion.LookRotation(horizontal, Vector3.up), anchor = "친구 옆 앵커" };
                    if (!Clear(eye, p, size) || !ScreenClear(lesson.playerCamera, p, size)) continue;
                    float score = .2f + Mathf.Abs(delta.magnitude - preferredDistance);
                    if (score < best) { best = score; result = p; }
                }
            }
            return best < float.PositiveInfinity;
        }
        static bool ScreenClear(Camera camera, Placement pose, Vector2 size)
        {
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var p = camera.WorldToViewportPoint(Corner(pose.position, pose.rotation, size, i));
                if (p.z <= camera.nearClipPlane) return false;
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            var rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            foreach (var region in protectedScreen)
                if (region.weight >= 60 && ElevatorUIPlacement.Overlap(rect, region.rect) > .0001f) return false;
            return true;
        }
    }
}
#endif
