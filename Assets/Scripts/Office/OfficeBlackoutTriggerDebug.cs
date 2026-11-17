using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Logs the first player entry into a trigger for scene diagnostics.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeBlackoutTriggerDebug : MonoBehaviour
    {
        bool hasLoggedPlayerDetection;

        void OnTriggerEnter(Collider other)
        {
            if (hasLoggedPlayerDetection || !IsPlayer(other)) return;

            hasLoggedPlayerDetection = true;
            Debug.Log($"[OfficeBlackout] Player detected by {name} trigger.", this);
        }

        static bool IsPlayer(Collider other)
        {
            Transform candidate = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            return candidate.CompareTag("Player") || candidate.GetComponentInParent<CharacterController>() != null;
        }
    }
}
