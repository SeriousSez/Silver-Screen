using System;

namespace SilverScreen.Domain.Movie
{
    [Serializable]
    public sealed class MovieProductionResult
    {
        public MovieProductionResult(
            int overallQuality,
            int castPerformance,
            int direction,
            int productionValue)
        {
            OverallQuality = ClampScore(overallQuality);
            CastPerformance = ClampScore(castPerformance);
            Direction = ClampScore(direction);
            ProductionValue = ClampScore(productionValue);
        }

        public int OverallQuality { get; }
        public int CastPerformance { get; }
        public int Direction { get; }
        public int ProductionValue { get; }

        private static int ClampScore(int score)
        {
            return Math.Clamp(score, 0, 100);
        }
    }
}
