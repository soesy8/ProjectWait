// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorBillboardCanvas.cs. Original is not modified.
using UnityEngine;
namespace ProjectWait.M.Elevator.DialoguePreview
{
    [DefaultExecutionOrder(250)]
    public sealed class ElevatorBillboardCanvas : MonoBehaviour
    {
        public Camera playerCamera;
        void LateUpdate()
        {
            if(!playerCamera)return;var direction=transform.position-playerCamera.transform.position;
            if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
        }
    }
}
