using System;

namespace SilverScreen.Domain.Movie
{
    public sealed class MovieQualityCalculator : IMovieQualityCalculator
    {
        public const int ProtagonistWeight = 2;
        public const int SupportingWeight = 1;
        public const int CastPercentage = 40;
        public const int DirectionPercentage = 35;
        public const int ProductionValuePercentage = 25;
        public const int LowBudgetProductionValue = 40;
        public const int StandardBudgetProductionValue = 65;
        public const int HighBudgetProductionValue = 90;

        public MovieProductionResult Calculate(MovieProject movie)
        {
            if (movie == null) throw new ArgumentNullException(nameof(movie));

            int castPerformance = CalculateCastPerformance(movie);
            int direction = ClampScore(movie.AssignedDirector?.Skill ?? 0);
            int productionValue = GetProductionValue(movie.BudgetTierId);
            int weightedTotal =
                castPerformance * CastPercentage +
                direction * DirectionPercentage +
                productionValue * ProductionValuePercentage;
            int overallQuality = ClampScore(RoundDivided(weightedTotal, 100));

            return new MovieProductionResult(
                overallQuality,
                castPerformance,
                direction,
                productionValue);
        }

        public static int GetProductionValue(string budgetTierId)
        {
            if (string.Equals(budgetTierId, BudgetTier.Low.Id, StringComparison.OrdinalIgnoreCase))
            {
                return LowBudgetProductionValue;
            }

            if (string.Equals(budgetTierId, BudgetTier.High.Id, StringComparison.OrdinalIgnoreCase))
            {
                return HighBudgetProductionValue;
            }

            return StandardBudgetProductionValue;
        }

        private static int CalculateCastPerformance(MovieProject movie)
        {
            int weightedSkillTotal = 0;
            int totalWeight = 0;

            foreach (var role in movie.Roles)
            {
                if (role.AssignedActor == null) continue;

                int roleWeight = role.RoleType == MovieRoleType.Protagonist
                    ? ProtagonistWeight
                    : SupportingWeight;
                weightedSkillTotal += ClampScore(role.AssignedActor.Skill) * roleWeight;
                totalWeight += roleWeight;
            }

            return totalWeight == 0
                ? 0
                : ClampScore(RoundDivided(weightedSkillTotal, totalWeight));
        }

        private static int RoundDivided(int numerator, int denominator)
        {
            return (numerator + denominator / 2) / denominator;
        }

        private static int ClampScore(int score)
        {
            return Math.Clamp(score, 0, 100);
        }
    }
}
