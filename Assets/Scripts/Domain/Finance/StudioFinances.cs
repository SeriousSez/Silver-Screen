using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Finance
{
    public sealed class StudioFinances : IStudioFinanceService
    {
        public static readonly Money DefaultStartingCash = Money.FromDollars(250000);

        private readonly List<FinancialTransaction> _transactions = new List<FinancialTransaction>();

        public StudioFinances(SimulationDateTime startingDate, Money? startingCash = null)
        {
            Money capital = startingCash ?? DefaultStartingCash;
            CurrentCash = Money.Zero;
            TotalIncome = Money.Zero;
            TotalExpenses = Money.Zero;

            if (capital > Money.Zero)
            {
                RecordIncome(
                    capital,
                    FinancialTransactionCategory.StartingCapital,
                    startingDate,
                    "Starting Capital");
            }
        }

        public Money CurrentCash { get; private set; }
        public Money TotalIncome { get; private set; }
        public Money TotalExpenses { get; private set; }
        public IReadOnlyList<FinancialTransaction> Transactions => _transactions;

        public event Action OnFinancesChanged;

        public bool CanAfford(Money amount)
        {
            return amount >= Money.Zero && CurrentCash >= amount;
        }

        public bool TryRecordExpense(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null)
        {
            ValidatePositiveAmount(amount);
            if (!CanAfford(amount)) return false;

            RecordSignedTransaction(-amount, category, date, description, referenceId);
            return true;
        }

        public void RecordObligation(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null)
        {
            ValidatePositiveAmount(amount);
            RecordSignedTransaction(-amount, category, date, description, referenceId);
        }

        public void RecordIncome(
            Money amount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId = null)
        {
            ValidatePositiveAmount(amount);
            RecordSignedTransaction(amount, category, date, description, referenceId);
        }

        private void RecordSignedTransaction(
            Money signedAmount,
            FinancialTransactionCategory category,
            SimulationDateTime date,
            string description,
            string referenceId)
        {
            var transaction = new FinancialTransaction(
                Guid.NewGuid().ToString(),
                date,
                signedAmount,
                category,
                description,
                referenceId);

            _transactions.Add(transaction);
            CurrentCash += signedAmount;

            if (signedAmount.Cents > 0)
            {
                TotalIncome += signedAmount;
            }
            else
            {
                TotalExpenses += -signedAmount;
            }

            OnFinancesChanged?.Invoke();
        }

        private static void ValidatePositiveAmount(Money amount)
        {
            if (amount <= Money.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Financial amounts must be positive; transaction direction is chosen by the API.");
            }
        }
    }
}
