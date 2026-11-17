using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    [DefaultExecutionOrder(220)]
    public sealed class OfficeCharacterPerformance : MonoBehaviour
    {
        public Transform body, head, leftArm, rightArm, leftLeg, rightLeg;
        public OfficeBlackoutWaypointMover mover;
        public Camera viewer;
        Vector3 restBody, previous;
        Quaternion[] rests;
        Transform[] joints;
        float clock, stride;

        void Awake()
        {
            joints = new[] { head, leftArm, rightArm, leftLeg, rightLeg };
            rests = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) if (joints[i]) rests[i] = joints[i].localRotation;
            if (body) restBody = body.localPosition;
            previous = transform.position;
        }
        void LateUpdate()
        {
            float delta = OfficeStoryTime.Delta;
            if (delta <= 0) { previous = transform.position; return; }
            clock += delta;
            float speed = Vector3.Distance(previous, transform.position) / Mathf.Max(.001f, delta);
            previous = transform.position;
            stride = Mathf.MoveTowards(stride, mover && mover.IsMoving ? Mathf.Clamp01(speed / 1.1f) : 0, delta * 5);
            float wave = Mathf.Sin(clock * 11) * stride;
            if (body) body.localPosition = restBody + Vector3.up * (.004f * Mathf.Sin(clock * 2) + .012f * Mathf.Abs(wave));
            for (int i = 0; i < joints.Length; i++)
            {
                if (!joints[i]) continue;
                float swing = i == 0 ? Mathf.Sin(clock * 1.7f) * 2 :
                    wave * (i % 2 == 0 ? 1 : -1) * (i < 3 ? 17 : 24);
                // A quiet open-hand gesture when the teacher is explaining.
                float gesture = i == 2 && (!mover || !mover.IsMoving) ? -12 : 0;
                joints[i].localRotation = rests[i] * Quaternion.Euler(swing + gesture, 0, 0);
            }
            if (viewer && (!mover || !mover.IsMoving))
            {
                Vector3 direction = Vector3.ProjectOnPlane(viewer.transform.position - transform.position, Vector3.up);
                if (direction.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), delta * 3);
            }
        }
    }
}
