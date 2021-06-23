using OctoAwesome.Database;

using System;
using System.Security.Cryptography;

namespace OctoAwesome.Serialization
{
    public struct Index3Tag : ITag, IEquatable<Index3Tag>
    {
        public int Length => sizeof(int) * 3;

        public Index3 Index { get; private set; }

        public Index3Tag(Index3 index) => Index = index;

        public void FromBytes(byte[] array, int startIndex)
            => Index = new Index3(BitConverter.ToInt32(array, startIndex),
                                    BitConverter.ToInt32(array, startIndex + sizeof(int)),
                                  BitConverter.ToInt32(array, startIndex + sizeof(int) + sizeof(int)));

        public byte[] GetBytes()
        {
            var byteArray = new byte[Length];
            const int intSize = sizeof(int);

            Buffer.BlockCopy(BitConverter.GetBytes(Index.X), 0, byteArray, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(Index.Y), 0, byteArray, sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(Index.Z), 0, byteArray, sizeof(int) + sizeof(int), sizeof(int));
            return byteArray;
        }

        public override bool Equals(object obj)
            => obj is Index3Tag tag && Equals(tag);

        public bool Equals(Index3Tag other)
            => Length == other.Length && Index.Equals(other.Index);

        public override int GetHashCode()
        {
            int hashCode = 802246856;
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + Index.GetHashCode();
            return hashCode;
        }

        public void WriteBytes(Span<byte> span)
        {
            BitConverter.TryWriteBytes(span, Index.X);
            BitConverter.TryWriteBytes(span[sizeof(int)..], Index.Y);
            BitConverter.TryWriteBytes(span[(sizeof(int) + sizeof(int))..], Index.Z);
        }

        public static bool operator ==(Index3Tag left, Index3Tag right)
            => left.Equals(right);

        public static bool operator !=(Index3Tag left, Index3Tag right)
            => !(left == right);
    }
}
