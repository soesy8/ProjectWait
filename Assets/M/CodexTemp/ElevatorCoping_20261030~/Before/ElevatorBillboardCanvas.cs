using UnityEngine;
namespace ProjectWait.M.Elevator
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
