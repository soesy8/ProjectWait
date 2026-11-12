using UnityEngine;

namespace ProjectWait.M.SceneTransition
{
    // Store the eye, not the origin: the two scenes use different rig hierarchies/heights.
    public readonly struct SceneTransitionHeadPose
    {
        public float EyeHeight { get; }
        public Quaternion EyeRotation { get; }
        public float TrackingYaw { get; }
        public bool Tracked { get; }
        public bool IsValid => EyeHeight > .1f && EyeHeight < 3.5f;

        public SceneTransitionHeadPose(Transform eye, float floorHeight, bool tracked)
        {
            EyeHeight = eye.position.y - floorHeight;
            EyeRotation = eye.rotation;
            TrackingYaw = Yaw(eye.parent ? eye.parent.rotation : Quaternion.identity);
            Tracked = tracked;
        }

        // Caller disables the capsule and recalibrates its feet after this one-off move.
        // Never write a tracked camera's local pose or keep pinning a moving head each frame.
        public void Apply(Transform origin, Transform eye, float floorHeight, bool tracked, Vector3 footprint)
        {
            if (tracked)
            {
                float parentYaw = Yaw(eye.parent ? eye.parent.rotation : Quaternion.identity);
                float desiredYaw = Tracked ? TrackingYaw : Yaw(EyeRotation) - Yaw(eye.localRotation);
                origin.RotateAround(eye.position, Vector3.up, Mathf.DeltaAngle(parentYaw, desiredYaw));
            }
            else eye.rotation = EyeRotation;
            origin.position += new Vector3(footprint.x, floorHeight + EyeHeight, footprint.z) - eye.position;
        }

        public static float Yaw(Quaternion rotation) => rotation.eulerAngles.y;
    }
}
