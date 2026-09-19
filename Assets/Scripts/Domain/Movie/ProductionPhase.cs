namespace SilverScreen.Domain.Movie
{
    public enum ProductionPhase
    {
        Inactive,
        MovingToStations,
        AtStations,
        Blocking,
        ReadyForTake,
        Slating,
        Filming,
        AwaitingNextScene,
        Completed,
        Failed
    }
}
