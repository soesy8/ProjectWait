using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace ProjectWait.M.TitleScene
{
    public static class TitleControllerPose
    {
        // Read the same OpenXR aim basis for both the model and selection ray.
        public static bool TryRead(InputAction action,out Vector3 position,out Quaternion rotation)
        {
            position=default;rotation=Quaternion.identity;
            if(action==null||!action.enabled)return false;
            const InputTrackingState required=InputTrackingState.Position|InputTrackingState.Rotation;
            foreach(var control in action.controls)
            {
                var pose=control as PoseControl;
                if(pose==null||!pose.device.added||!pose.device.enabled)continue;
                // This provider's byte boolean can be 1/255, not a .5-threshold button.
                if(pose.isTracked.ReadValue()<=0||((InputTrackingState)pose.trackingState.ReadValue()&required)!=required)continue;
                var p=pose.position.ReadValue();var q=pose.rotation.ReadValue();
                if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z)||!Finite(q.x)||!Finite(q.y)||!Finite(q.z)||!Finite(q.w)||Quaternion.Dot(q,q)<.5f)continue;
                position=p;rotation=q.normalized;return true;
            }
            return false;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
