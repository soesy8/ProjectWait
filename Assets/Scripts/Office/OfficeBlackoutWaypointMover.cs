using System.Collections;
using UnityEngine;
using WaitForSecondsRealtime = ProjectWait.YB.OfficeBlackout.OfficeStoryWait;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>Moves an NPC through scene waypoint transforms in array order.</summary>
    public sealed class OfficeBlackoutWaypointMover : MonoBehaviour
    {
        [SerializeField] Transform[] waypoints = System.Array.Empty<Transform>();
        [SerializeField, Min(0.1f)] float movementSpeed = 2.2f;
        [SerializeField, Min(0.1f)] float turnSpeed = 8f;
        [SerializeField, Min(0.01f)] float arrivalDistance = 0.12f;
        [SerializeField] bool startOnSceneStart = true;
        [SerializeField, Min(0f)] float startDelaySeconds = 1.5f;
        [SerializeField] int[] pauseAtWaypointIndices = { 1 };

        int currentWaypointIndex;
        bool isMoving;
        bool continueWithoutPausing;
        Coroutine delayedStartRoutine;

        public int CurrentWaypointIndex => currentWaypointIndex;
        public bool IsMoving => isMoving;
        public bool IsRouteComplete => waypoints != null && currentWaypointIndex >= waypoints.Length && !isMoving;

        void Start()
        {
            if (startOnSceneStart)
                delayedStartRoutine = StartCoroutine(StartRouteAfterDelay());
        }

        IEnumerator StartRouteAfterDelay()
        {
            if (startDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(startDelaySeconds);

            delayedStartRoutine = null;
            StartRoute();
        }

        void Update()
        {
            if (OfficeStoryTime.Frozen || !isMoving || waypoints == null) return;

            while (currentWaypointIndex < waypoints.Length && !waypoints[currentWaypointIndex])
                currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                isMoving = false;
                return;
            }

            Transform destination = waypoints[currentWaypointIndex];
            Vector3 destinationPosition = destination.position;
            destinationPosition.y = transform.position.y;
            Vector3 toDestination = destinationPosition - transform.position;
            if (toDestination.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                transform.position = destinationPosition;
                int reachedWaypointIndex = currentWaypointIndex;
                currentWaypointIndex++;
                if ((!continueWithoutPausing && ShouldPauseAt(reachedWaypointIndex)) || currentWaypointIndex >= waypoints.Length)
                    isMoving = false;
                return;
            }

            Vector3 direction = toDestination.normalized;
            transform.position = Vector3.MoveTowards(
                transform.position,
                destinationPosition,
                movementSpeed * Time.deltaTime * ProjectWait.M.Shared.ExperiencePreferences.Current.movement);

            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }
        }

        /// <summary>Starts again from the first configured waypoint.</summary>
        public void StartRoute()
        {
            if (delayedStartRoutine != null)
            {
                StopCoroutine(delayedStartRoutine);
                delayedStartRoutine = null;
            }
            currentWaypointIndex = 0;
            continueWithoutPausing = false;
            isMoving = waypoints != null && waypoints.Length > 0;
        }

        /// <summary>Moves to the next point, then stops there when configured to stop at each point.</summary>
        public void MoveToNextWaypoint()
        {
            if (waypoints == null || currentWaypointIndex >= waypoints.Length) return;
            continueWithoutPausing = false;
            isMoving = true;
        }

        /// <summary>Continues through all remaining waypoints without pausing between them.</summary>
        public void ContinueRoute()
        {
            if (waypoints == null || currentWaypointIndex >= waypoints.Length) return;
            continueWithoutPausing = true;
            isMoving = true;
        }

        /// <summary>Stops at the current position without discarding the remaining route.</summary>
        public void StopMovement()
        {
            if (delayedStartRoutine != null)
            {
                StopCoroutine(delayedStartRoutine);
                delayedStartRoutine = null;
            }
            isMoving = false;
        }

        public void FinishRouteUnderCover()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            for (int i = waypoints.Length - 1; i >= 0; i--) if (waypoints[i])
            { var position = waypoints[i].position; position.y = transform.position.y; transform.position = position; break; }
            currentWaypointIndex = waypoints.Length; isMoving = false;
        }

        bool ShouldPauseAt(int waypointIndex)
        {
            if (pauseAtWaypointIndices == null) return false;
            foreach (int pauseIndex in pauseAtWaypointIndices)
                if (pauseIndex == waypointIndex) return true;
            return false;
        }
    }
}
