using System;
using System.Collections.Generic;
using SilverScreen.Domain.Finance;

namespace SilverScreen.Domain.Movie
{
    public static class TheatricalMarketConfiguration
    {
        public const int TheatricalRunDays = 28;
        public const int DaysPerWeek = 7;
        public static readonly IReadOnlyList<int> WeeklyPercentages = new[] { 45, 30, 17, 8 };

        public static Money GetMarketBase(string budgetTierId)
        {
            if (string.Equals(budgetTierId, BudgetTier.Low.Id, StringComparison.OrdinalIgnoreCase))
            {
                return Money.FromDollars(75000);
            }

            if (string.Equals(budgetTierId, BudgetTier.High.Id, StringComparison.OrdinalIgnoreCase))
            {
                return Money.FromDollars(350000);
            }

            return Money.FromDollars(175000);
        }

        public static Money CalculateTargetGross(string budgetTierId, int audienceReception)
        {
            Money marketBase = GetMarketBase(budgetTierId);
            int reception = Math.Clamp(audienceReception, 0, 100);
            long multiplierBasisPoints = 2500L + 125L * reception;
            long targetCents = checked((marketBase.Cents * multiplierBasisPoints + 5000L) / 10000L);
            return Money.FromCents(targetCents);
        }

        public static IReadOnlyList<Money> CalculateWeeklyGrosses(Money targetGross)
        {
            var grosses = new Money[4];
            long allocatedCents = 0;
            for (int i = 0; i < grosses.Length - 1; i++)
            {
                long weekCents = targetGross.Cents * WeeklyPercentages[i] / 100L;
                grosses[i] = Money.FromCents(weekCents);
                allocatedCents += weekCents;
            }

            grosses[3] = Money.FromCents(targetGross.Cents - allocatedCents);
            return grosses;
        }
    }
}
