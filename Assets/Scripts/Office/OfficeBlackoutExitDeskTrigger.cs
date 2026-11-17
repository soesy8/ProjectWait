using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Starts the friend-calming beat when the player leaves the desk and reaches ExitDesk.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeBlackoutExitDeskTrigger : MonoBehaviour
    {
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] Transform playerRoot;

        void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger) trigger.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (OfficeSceneIntegration.GameplayBlocked) return;
            if (!IsPlayer(other) || !director) return;
            if (director.PlayerReachedExitDesk())
                Debug.Log("[OfficeBlackout] ExitDesk reached; friend-calmed event started.", this);
        }
        void OnTriggerStay(Collider other)
        {
            if (!OfficeSceneIntegration.GameplayBlocked && IsPlayer(other) && director) director.PlayerReachedExitDesk();
        }

        bool IsPlayer(Collider other)
        {
            if (playerRoot)
                return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);

            Transform candidate = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            return candidate.CompareTag("Player") || candidate.GetComponentInParent<CharacterController>() != null;
        }
    }
}
