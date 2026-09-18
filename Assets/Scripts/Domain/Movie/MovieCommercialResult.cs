using System;
using SilverScreen.Domain.Finance;

namespace SilverScreen.Domain.Movie
{
    [Serializable]
    public sealed class MovieCommercialResult
    {
        public MovieCommercialResult(
            int audienceReception,
            Money totalBoxOfficeGross,
            Money totalStudioRevenue,
            Money productionBudget)
        {
            AudienceReception = Math.Clamp(audienceReception, 0, 100);
            TotalBoxOfficeGross = totalBoxOfficeGross;
            TotalStudioRevenue = totalStudioRevenue;
            ProductionBudget = productionBudget;
            CommercialProfitLoss = totalStudioRevenue - productionBudget;
        }

        public int AudienceReception { get; }
        public Money TotalBoxOfficeGross { get; }
        public Money TotalStudioRevenue { get; }
        public Money ProductionBudget { get; }
        public Money CommercialProfitLoss { get; }
    }
}
