namespace SilverScreen.Domain.Movie
{
    public enum ProductionPhase
    {
        Inactive,
        EnvironmentUnresolved,
        MovingToStations,
        AtStations,
        Blocking,
        ReadyForTake,
        Slating,
        Filming,
        AwaitingTakeDecision,
        AwaitingNextScene,
        Completed,
        Failed
    }
}
