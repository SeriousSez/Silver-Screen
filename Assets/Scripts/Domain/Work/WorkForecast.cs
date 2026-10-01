using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Work
{
    public sealed class WorkForecast
    {
        public SimulationInstant? EstimatedCompletion { get; }
        public string Reason { get; }
        public long BasedOnRevision { get; }
        internal WorkForecast(SimulationInstant? completion, string reason, long revision)
        {
            EstimatedCompletion = completion; Reason = reason; BasedOnRevision = revision;
        }
    }
}
