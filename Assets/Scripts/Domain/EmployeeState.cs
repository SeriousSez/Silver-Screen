namespace SilverScreen.Domain
{
    public enum EmployeeState
    {
        Idle,
        Walking,
        Working,
        Casting,
        Rehearsing,
        Filming,
        Eating,
        Socializing,
        Resting
    }

    public enum EmployeeIntentPurpose
    {
        None,
        IdleWander,
        ReportToCasting,
        ReportToStage,
        PerformTask
    }

    public class EmployeeIntent
    {
        public EmployeeIntentPurpose Purpose { get; }
        public string TargetBuildingId { get; }
        public string Description { get; }

        public EmployeeIntent(EmployeeIntentPurpose purpose, string description = "", string targetBuildingId = null)
        {
            Purpose = purpose;
            Description = description;
            TargetBuildingId = targetBuildingId;
        }

        public static EmployeeIntent None => new EmployeeIntent(EmployeeIntentPurpose.None, "No assignment");
        public static EmployeeIntent IdleWander => new EmployeeIntent(EmployeeIntentPurpose.IdleWander, "Strolling the lot");
    }
}
