namespace ProjectWait.M.Elevator.DialoguePreview
{
    public static class ElevatorTravelTiming
    {
        public static bool IsWalking(ElevatorStep step) => step==ElevatorStep.GymDeparture||step==ElevatorStep.Entering||step==ElevatorStep.Exiting||step==ElevatorStep.GoingHome;
        public static bool IsRelocation(ElevatorStep step)=>IsWalking(step)||step==ElevatorStep.Returning;
        public static bool IsTimedDialogue(ElevatorStep step) => step==ElevatorStep.Connecting||step==ElevatorStep.CallQuestion||step==ElevatorStep.CallResponse||step==ElevatorStep.CallLiftQuestion||step==ElevatorStep.CallLocationResponse||step==ElevatorStep.CallSafetyAdvice||step==ElevatorStep.Stopped;
    }
}
