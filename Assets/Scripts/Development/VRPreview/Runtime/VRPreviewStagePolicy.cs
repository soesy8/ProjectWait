#if UNITY_EDITOR
using ProjectWait.M.Elevator.DialoguePreview;

namespace ProjectWait.M.VRPreview
{
    // Presentation follows story ownership, never inferred gaze/locomotion intent.
    public static class VRPreviewStagePolicy
    {
        public enum Mode { Cinematic, Observe, Description, RequiredReading, Complete }

        public static Mode Evaluate(ElevatorLesson lesson)
        {
            var flow = lesson.Flow;
            if (flow == null || flow.IsCinematic) return Mode.Cinematic;
            if (flow.Step == ElevatorStep.Complete) return Mode.Complete;
            // These cards stay readable while another world object replaces them.
            // Choice buttons are still gated until rendered by VRPreviewReading.
            if (flow.HasObjectPreview || flow.SearchingIdentity || flow.ReviewingIdentity)
                return Mode.Description;
            if (flow.AwaitingCallContinue || flow.Step == ElevatorStep.Retry)
                return Mode.RequiredReading;
            if (lesson.scenario && lesson.scenario.objectGuidedScene)
            {
                switch (flow.Step)
                {
                    case ElevatorStep.Stopped:
                    case ElevatorStep.Unanswered:
                    case ElevatorStep.DoorPosterPointing:
                    case ElevatorStep.Rescue:
                        return Mode.Observe;
                }
                if (flow.SearchingIdentity) return Mode.Observe;
            }
            if (NeedsAcknowledgment(lesson)) return Mode.RequiredReading;
            if (flow.CanObserve && lesson.view.ChoiceButtons.Count == 0) return Mode.Observe;
            return Mode.RequiredReading;
        }

        public static bool NeedsAcknowledgment(ElevatorLesson lesson)
        {
            if (!lesson.scenario || !lesson.scenario.objectGuidedScene) return false;
            switch (lesson.Flow.Step)
            {
                case ElevatorStep.Lobby: case ElevatorStep.Boarding:
                case ElevatorStep.ReturnLobby: case ElevatorStep.GroundFloor:
                    return true;
                default: return false;
            }
        }

        public static string Cue(ElevatorLesson lesson)
        {
            var flow = lesson.Flow;
            if (flow.SearchingIdentity) return "건물 이름과 승강기 번호를 찾아보세요.";
            switch (flow.Step)
            {
                case ElevatorStep.Lobby: return "엘리베이터 호출 버튼을 눌러 주세요.";
                case ElevatorStep.Boarding: return flow.DestinationSelected ? "친구가 건네는 공을 받아 주세요." : "1층 버튼을 눌러 주세요.";
                case ElevatorStep.Stopped: return "비상통화 버튼으로 도움을 요청하세요.";
                case ElevatorStep.Unanswered: return "휴대폰으로 119에 연락해 주세요.";
                case ElevatorStep.DoorPosterPointing: return "친구와 이야기해 보세요.";
                case ElevatorStep.Waiting: return "친구의 이야기를 들어 주세요.";
                case ElevatorStep.Rescue:
                case ElevatorStep.ReturnLobby: return "구조요원을 보고 안내를 따라 주세요.";
                case ElevatorStep.GroundFloor: return "친구와 함께 출입구로 이동하세요.";
                default: return "이야기를 읽고 다음 행동을 골라 주세요.";
            }
        }
    }
}
#endif
