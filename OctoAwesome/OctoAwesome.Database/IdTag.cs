using System;
using System.Collections.Generic;
using System.Text;

namespace OctoAwesome.Database
{
    public struct IdTag<T> : ITag, IEquatable<IdTag<T>>
    {
        public int Tag { get; private set; }

        public int Length => sizeof(int);

        public IdTag(int id)
        {
            Tag = id;
        }

        public byte[] GetBytes()
            => BitConverter.GetBytes(Tag);

        public void FromBytes(byte[] array, int startIndex)
            => Tag = BitConverter.ToInt32(array, startIndex);

        public override bool Equals(object obj)
            => obj is IdTag<T> tag && Equals(tag);

        public bool Equals(IdTag<T> other)
            => Length == other.Length && Tag == other.Tag;

        public override int GetHashCode()
        {
            int hashCode = 139101280;
            hashCode = hashCode * -1521134295 + Tag.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }

        public void WriteBytes(Span<byte> span)
        {
            BitConverter.TryWriteBytes(span, Tag);
        }

        public static bool operator ==(IdTag<T> left, IdTag<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IdTag<T> left, IdTag<T> right)
        {
            return !(left == right);
        }
    }
}
