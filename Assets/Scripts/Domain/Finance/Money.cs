using System;

namespace SilverScreen.Domain.Finance
{
    [Serializable]
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public static readonly Money Zero = new Money(0);

        public long Cents { get; }
        public long WholeDollars => Cents / 100;

        private Money(long cents)
        {
            Cents = cents;
        }

        public static Money FromCents(long cents) => new Money(cents);
        public static Money FromDollars(long dollars) => new Money(checked(dollars * 100));

        public int CompareTo(Money other) => Cents.CompareTo(other.Cents);
        public bool Equals(Money other) => Cents == other.Cents;
        public override bool Equals(object obj) => obj is Money other && Equals(other);
        public override int GetHashCode() => Cents.GetHashCode();
        public override string ToString() => $"${WholeDollars:N0}";

        public static Money operator +(Money left, Money right) => FromCents(checked(left.Cents + right.Cents));
        public static Money operator -(Money left, Money right) => FromCents(checked(left.Cents - right.Cents));
        public static Money operator -(Money value) => FromCents(checked(-value.Cents));
        public static bool operator >(Money left, Money right) => left.Cents > right.Cents;
        public static bool operator <(Money left, Money right) => left.Cents < right.Cents;
        public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;
        public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;
        public static bool operator ==(Money left, Money right) => left.Equals(right);
        public static bool operator !=(Money left, Money right) => !left.Equals(right);
    }
}
