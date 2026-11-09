using UnityEngine;

namespace ProjectWait.M.Elevator.ObservationTrial
{
    // Reveal and departure share one clock, so motion continues as the black panel clears.
    public static class ElevatorOpeningSequence
    {
        public const float Duration = 7.7f;
        public const float FadeHold = .3f, FadeSeconds = 1.5f;
        public const float FadeDuration = FadeHold + FadeSeconds;
        public const float DepartureSeconds = Duration;
        public const float TeleportAt = 4.2f;
        public const string FriendLine = "오늘 농구 재밌었지? 이제 집에 가자!";
        public const string DepartureLine = "농구를 마치고 친구와 함께 3층 로비로 나가요.";

        public static float FadeAlpha(float elapsed) =>
            1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(FadeHold, FadeDuration, elapsed));

        // The later HMD travel fade shares the same departure clock as the startup reveal.
        public static float ComfortFadeAlpha(float elapsed, bool xr) => xr ? 1 - Mathf.SmoothStep(0, 1,
            Mathf.InverseLerp(.12f, .65f, Mathf.Abs(elapsed - TeleportAt))) : 0;

        public static string Caption(float elapsed) =>
            elapsed < 5 ? ElevatorDialogue.Caption("친구", FriendLine) : DepartureLine;

        public static Vector3 WalkPath(Vector3 start, Vector3 threshold, Vector3 end, float elapsed, float begins, float finishes)
        {
            return WalkPath(start, threshold, end, elapsed, begins, finishes, out _);
        }

        public static Vector3 WalkPath(Vector3 start, Vector3 threshold, Vector3 end, float elapsed, float begins, float finishes, out Vector3 direction)
        {
            float progress = WalkProgress(elapsed, begins, finishes);
            Vector3 incoming = threshold - start, outgoing = end - threshold;
            float first = incoming.magnitude, second = outgoing.magnitude;
            if (first < .001f || second < .001f)
            {
                direction = (end - start).normalized;
                return Vector3.Lerp(start, end, progress);
            }
            incoming /= first; outgoing /= second;
            // Keep the doorway approach straight; round only the former lobby corner.
            float radius = Mathf.Min(1.25f, Mathf.Min(first, second) * .4f);
            Vector3 entry = threshold - incoming * radius, exit = threshold + outgoing * radius;
            const int samples = 32;
            float curveLength = 0;
            Vector3 previous = entry;
            for (int i = 1; i <= samples; i++)
            {
                Vector3 point = Corner(entry, threshold, exit, i / (float)samples);
                curveLength += Vector3.Distance(previous, point); previous = point;
            }
            float straightIn = first - radius, straightOut = second - radius;
            float distance = progress * (straightIn + curveLength + straightOut);
            if (distance <= straightIn)
            {
                direction = incoming;
                return start + incoming * distance;
            }
            if (distance >= straightIn + curveLength)
            {
                direction = outgoing;
                return progress >= 1 ? end : exit + outgoing * (distance - straightIn - curveLength);
            }
            // Invert a small arc-length table without allocations, avoiding a speed change on the bend.
            float remaining = distance - straightIn;
            previous = entry;
            for (int i = 1; i <= samples; i++)
            {
                float upper = i / (float)samples;
                Vector3 point = Corner(entry, threshold, exit, upper);
                float length = Vector3.Distance(previous, point);
                if (remaining <= length || i == samples)
                {
                    float u = (i - 1 + Mathf.Clamp01(remaining / Mathf.Max(.00001f, length))) / samples;
                    direction = ((threshold - entry) * (1 - u) + (exit - threshold) * u).normalized;
                    return Corner(entry, threshold, exit, u);
                }
                remaining -= length; previous = point;
            }
            direction = outgoing;
            return end;
        }

        static Vector3 Corner(Vector3 entry, Vector3 control, Vector3 exit, float t) =>
            (1 - t) * (1 - t) * entry + 2 * (1 - t) * t * control + t * t * exit;

        static float WalkProgress(float elapsed, float begins, float finishes)
        {
            // Integrating a smooth velocity ramp gives zero acceleration at both joins and endpoints.
            float t = Mathf.InverseLerp(begins, finishes, elapsed);
            const float ramp = .18f;
            if (t >= ramp && t <= 1 - ramp) return (t - ramp * .5f) / (1 - ramp);
            float u = Mathf.Min(t, 1 - t) / ramp;
            float edge = ramp / (1 - ramp) * (u * u * u - .5f * u * u * u * u);
            return t < ramp ? edge : 1 - edge;
        }

        public static float ViewYaw(Vector3 start, Vector3 threshold, Vector3 end, float elapsed, float begins, float finishes, bool xr)
        {
            // Headsets retain their live gaze and faded teleport; no visible forced HMD rotation.
            if (xr) return 0;
            var current = WalkPath(start, threshold, end, elapsed, begins, finishes);
            var ahead = WalkPath(start, threshold, end, elapsed + .85f, begins, finishes);
            var direction = Vector3.ProjectOnPlane(ahead - current, Vector3.up);
            if (direction.sqrMagnitude < .000001f) direction = end - threshold;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float settle = Mathf.InverseLerp(finishes - 1.8f, finishes, elapsed);
            settle = settle * settle * settle * (settle * (settle * 6 - 15) + 10);
            // Anticipate the curve gently, then face the lift before control returns to the player.
            return Mathf.LerpAngle(yaw * .72f, 0, settle);
        }
    }
}
