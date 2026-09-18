using System;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Finance
{
    [Serializable]
    public sealed class FinancialTransaction
    {
        public FinancialTransaction(
            string id,
            SimulationDateTime date,
            Money amount,
            FinancialTransactionCategory category,
            string description,
            string referenceId = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
            Date = date;
            Amount = amount;
            Category = category;
            Description = description ?? string.Empty;
            ReferenceId = referenceId ?? string.Empty;
        }

        public string Id { get; }
        public SimulationDateTime Date { get; }
        public Money Amount { get; }
        public FinancialTransactionCategory Category { get; }
        public string Description { get; }
        public string ReferenceId { get; }
        public bool IsIncome => Amount.Cents > 0;
        public bool IsExpense => Amount.Cents < 0;
    }
}
