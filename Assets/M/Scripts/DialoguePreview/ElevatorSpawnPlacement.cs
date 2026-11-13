using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // One-off arrival placement. Never write the tracked eye's local pose.
    public static class ElevatorSpawnPlacement
    {
        public static bool TryApply(Transform origin,Transform eye,Vector3 footprint,float heading,
            bool tracked,bool originReady,bool floorTracking,float deviceEyeHeight,out float eyeHeight)
        {
            eyeHeight=0;
            if(!origin||!eye||!tracked||!originReady)return false;
            var relative=eye.position-origin.position;
            if(!StereoUISafeArea.Finite(relative.x)||!StereoUISafeArea.Finite(relative.y)||!StereoUISafeArea.Finite(relative.z))return false;
            var rotation=eye.rotation;
            if(!StereoUISafeArea.Finite(heading)||!StereoUISafeArea.Finite(rotation.x)||!StereoUISafeArea.Finite(rotation.y)||
                !StereoUISafeArea.Finite(rotation.z)||!StereoUISafeArea.Finite(rotation.w))return false;
            eyeHeight=floorTracking?relative.y:deviceEyeHeight;
            if(!StereoUISafeArea.Finite(eyeHeight)||eyeHeight<.35f||eyeHeight>3.5f)return false;
            origin.RotateAround(eye.position,Vector3.up,Mathf.DeltaAngle(eye.eulerAngles.y,heading));
            origin.position+=new Vector3(footprint.x,footprint.y+eyeHeight,footprint.z)-eye.position;
            return true;
        }
    }
}
