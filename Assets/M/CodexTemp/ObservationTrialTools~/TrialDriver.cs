public static class TrialDriver
{
    public static object Build() => ProjectWait.M.Editor.ElevatorObservationTrialSetup.PrepareAndSave();
    public static object Flow() => ProjectWait.M.Editor.ElevatorObservationTrialSetup.ValidateFlow();
    public static object Scene() => ProjectWait.M.Editor.ElevatorObservationTrialSetup.ValidateScene();
}
