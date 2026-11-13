using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Authored scene locations, not a head-relative search. Each region's pose is latched
    // for the run, so text/choices, gaze, movement, subtitles and projection changes cannot move it.
    public sealed class VRDialogueWorldAnchors
    {
        public enum Region { Friend, CabinDoor, Rescue, Landing, Ground, Complete }
        readonly Dictionary<Region, Pose> poses = new Dictionary<Region, Pose>();
        int run = -1;
        public void Reset() { poses.Clear(); run = -1; }
        public static Region ForStep(ElevatorStep step)
        {
            switch (step)
            {
                case ElevatorStep.StartupFade: case ElevatorStep.GymDeparture: case ElevatorStep.Lobby: case ElevatorStep.Entering: return Region.Friend;
                case ElevatorStep.Rescue: case ElevatorStep.Exiting: return Region.Rescue;
                case ElevatorStep.ReturnLobby: case ElevatorStep.Returning: return Region.Landing;
                case ElevatorStep.GroundFloor: case ElevatorStep.GoingHome: return Region.Ground;
                case ElevatorStep.Complete: return Region.Complete;
                default: return Region.CabinDoor;
            }
        }
        public Pose Get(ElevatorLesson lesson, ElevatorStep step)
        {
            if (run != lesson.RunVersion) { poses.Clear(); run = lesson.RunVersion; }
            var region = ForStep(step);
            if (poses.TryGetValue(region, out var pose)) return pose;
            var space = lesson.presentation ? lesson.presentation.space : null;
            var map = space ? space.sportsCentre : null;
            Vector3 inside = map ? map.inside : space ? space.insidePosition : Vector3.zero;
            Vector3 lobby = map ? map.lobby : space ? space.lobbyPosition : Vector3.back * 2;
            Vector3 friend = map ? map.friendLobby : space ? space.friendOutside : lesson.friend.position;
            Vector3 forward = map ? Vector3.back : Vector3.forward;
            var presentation = lesson.presentation;
            if (presentation && presentation.leftDoor && presentation.rightDoor)
            {
                var direction = Vector3.ProjectOnPlane((presentation.leftDoor.position + presentation.rightDoor.position) * .5f - inside, Vector3.up);
                if (direction.sqrMagnitude > .01f) forward = direction.normalized;
            }
            Vector3 right = Vector3.Cross(Vector3.up, forward), reference = inside, centre;
            if (region == Region.Friend)
            {
                reference = lobby;
                var lobbyForward = Vector3.ProjectOnPlane(friend - lobby, Vector3.up).normalized;
                var lobbyRight = Vector3.Cross(Vector3.up, lobbyForward);
                centre = Vector3.Lerp(lobby, friend, .45f) + lobbyRight * .45f;
            }
            else if (region == Region.Landing)
            {
                reference = lobby; centre = lobby - forward * VRUIReadingLayout.Distance;
            }
            else if (region == Region.Ground || region == Region.Complete)
            {
                reference = map ? (region == Region.Complete ? map.groundExit : map.groundLobby) : lobby;
                centre = reference + forward * VRUIReadingLayout.Distance;
            }
            else centre = inside + forward * (region == Region.Rescue ? 1.65f : VRUIReadingLayout.Distance)
                - (region == Region.Rescue ? right * .35f : Vector3.zero);

            float eyeHeight = Mathf.Clamp(lesson.playerCamera.transform.position.y - reference.y, 1.0f, 1.85f);
            centre.y = reference.y + eyeHeight + (region == Region.Rescue ? .28f : 0);
            // Normal cards use the authored viewing footprint. Rescue faces the actual
            // player once when it appears, and does not billboard on subsequent frames.
            var viewer = region == Region.Rescue ? lesson.playerCamera.transform.position : reference + Vector3.up * eyeHeight;
            var facing = centre - viewer;
            pose = new Pose(centre, Quaternion.LookRotation(facing.sqrMagnitude > .001f ? facing : forward, Vector3.up));
            poses.Add(region, pose);
            return pose;
        }
    }
}
