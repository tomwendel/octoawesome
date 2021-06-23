using OctoAwesome.Database;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Serialization
{
    public struct ChunkDiffTag : ITag, IEquatable<ChunkDiffTag>
    {
        public int Length => sizeof(int) * 4;

        public Index3 ChunkPositon { get; set; }
        public int FlatIndex { get; set; }

        public ChunkDiffTag(Index3 chunkPosition, int flatIndex)
        {
            ChunkPositon = chunkPosition;
            FlatIndex = flatIndex;
        }

        public void FromBytes(byte[] array, int startIndex)
        {
            var x = BitConverter.ToInt32(array, startIndex);
            var y = BitConverter.ToInt32(array, startIndex + sizeof(int));
            var z = BitConverter.ToInt32(array, startIndex + sizeof(int) * 2);
            FlatIndex = BitConverter.ToInt32(array, startIndex + sizeof(int) * 3);
            ChunkPositon = new Index3(x, y, z);
        }

        public byte[] GetBytes()
        {
            var array = new byte[Length];

            const int intSize = sizeof(int);
            BitConverter.TryWriteBytes(array[0..(intSize * 1)], ChunkPositon.X);
            BitConverter.TryWriteBytes(array[(intSize * 1)..(intSize * 2)], ChunkPositon.Y);
            BitConverter.TryWriteBytes(array[(intSize * 2)..(intSize * 3)], ChunkPositon.Z);
            BitConverter.TryWriteBytes(array[(intSize * 3)..(intSize * 4)], FlatIndex);

            return array;
        }
        public void WriteBytes(Span<byte> span)
        {
            const int intSize = sizeof(int);
            BitConverter.TryWriteBytes(span[0..(intSize * 1)], ChunkPositon.X);
            BitConverter.TryWriteBytes(span[(intSize * 1)..(intSize * 2)], ChunkPositon.Y);
            BitConverter.TryWriteBytes(span[(intSize * 2)..(intSize * 3)], ChunkPositon.Z);
            BitConverter.TryWriteBytes(span[(intSize * 3)..(intSize * 4)], FlatIndex);
        }

        public override bool Equals(object obj)
            => obj is ChunkDiffTag tag && Equals(tag);

        public bool Equals(ChunkDiffTag other)
            => Length == other.Length &&
                FlatIndex == other.FlatIndex && 
                EqualityComparer<Index3>.Default.Equals(ChunkPositon, other.ChunkPositon);


        public override int GetHashCode()
        {
            int hashCode = 1893591923;
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + ChunkPositon.GetHashCode();
            hashCode = hashCode * -1521134295 + FlatIndex.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ChunkDiffTag left, ChunkDiffTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkDiffTag left, ChunkDiffTag right)
        {
            return !(left == right);
        }
    }
}
