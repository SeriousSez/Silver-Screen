using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    [Serializable]
    public struct BudgetTier : IEquatable<BudgetTier>
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Amount { get; }

        public BudgetTier(string id, string displayName, int amount)
        {
            Id = id;
            DisplayName = displayName;
            Amount = amount;
        }

        public static readonly BudgetTier Low = new BudgetTier("low", "Low", 25000);
        public static readonly BudgetTier Standard = new BudgetTier("standard", "Standard", 50000);
        public static readonly BudgetTier High = new BudgetTier("high", "High", 100000);

        public static IReadOnlyList<BudgetTier> DefaultTiers => new[] { Low, Standard, High };

        public static string GetIdForAmount(int amount)
        {
            foreach (var tier in DefaultTiers)
            {
                if (tier.Amount == amount) return tier.Id;
            }

            return Standard.Id;
        }

        public bool Equals(BudgetTier other) => Id == other.Id && Amount == other.Amount;
        public override bool Equals(object obj) => obj is BudgetTier other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Amount);
        public override string ToString() => $"{DisplayName} (${Amount:N0})";
    }
}
