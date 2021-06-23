using System;

namespace OctoAwesome.Components
{
    public struct Comparer<TOriginal, TSelected> : IEquatable<Comparer<TOriginal, TSelected>>, IEquatable<TOriginal>
    {
        public TOriginal Value { get; set; }

        private readonly Func<TOriginal, TSelected, bool> compareFunc;

        public Comparer(TOriginal value, Func<TOriginal, TSelected, bool> compareFunc)
        {
            Value = value;
            this.compareFunc = compareFunc;
        }

        public override bool Equals(object obj)
            => (obj is Comparer<TOriginal, TSelected> comparer && Equals(comparer))
            || (obj is TSelected rigth && Equals(rigth))
            || obj is TOriginal left && Equals(left);

        public bool Equals(Comparer<TOriginal, TSelected> other)
            => Equals(other.Value);

        public bool Equals(TOriginal other)
            => Equals(Value, other);

        public bool Equals(TSelected other)
            => compareFunc(Value, other);

        public override int GetHashCode()
            => Value.GetHashCode();

        public static bool operator ==(Comparer<TOriginal, TSelected> left, Comparer<TOriginal, TSelected> right)
            => left.Equals(right);
        public static bool operator !=(Comparer<TOriginal, TSelected> left, Comparer<TOriginal, TSelected> right)
            => !(left == right);

        public static bool operator ==(TOriginal left, Comparer<TOriginal, TSelected> right)
            => right.Equals(left);
        public static bool operator !=(TOriginal left, Comparer<TOriginal, TSelected> right)
            => !(left == right);

        public static bool operator ==(Comparer<TOriginal, TSelected> left, TOriginal right)
            => left.Equals(right);
        public static bool operator !=(Comparer<TOriginal, TSelected> left, TOriginal right)
            => !(left == right);

        public static bool operator ==(TSelected left, Comparer<TOriginal, TSelected> right)
            => right.Equals(left);
        public static bool operator !=(TSelected left, Comparer<TOriginal, TSelected> right)
            => !(left == right);

        public static bool operator ==(Comparer<TOriginal, TSelected> left, TSelected right)
            => left.Equals(right);
        public static bool operator !=(Comparer<TOriginal, TSelected> left, TSelected right)
            => !(left == right);

        public static implicit operator TOriginal(Comparer<TOriginal, TSelected> comparer)
            => comparer.Value;

        public static implicit operator Comparer<TOriginal, TSelected>((TOriginal value, Func<TOriginal, TSelected, bool> comparer) tuple)
            => new Comparer<TOriginal, TSelected>(tuple.value, tuple.comparer);

        public static implicit operator (TOriginal value, Func<TOriginal, TSelected, bool> comparer)(Comparer<TOriginal, TSelected> comparer)
            => (comparer.Value, comparer.compareFunc);
    }
}

