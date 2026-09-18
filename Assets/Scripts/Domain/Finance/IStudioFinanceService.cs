using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Finance
{
    public interface IStudioFinanceService
    {
        Money CurrentCash { get; }
        Money TotalIncome { get; }
        Money TotalExpenses { get; }
        IReadOnlyList<FinancialTransaction> Transactions { get; }

        event Action OnFinancesChanged;

        bool CanAfford(Money amount);
        bool TryRecordExpense(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null);
        void RecordObligation(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null);
        void RecordIncome(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null);
    }
}
