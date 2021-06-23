using OctoAwesome.Database.Expressions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OctoAwesome.Database
{
    public readonly struct Key<TTag> : IEquatable<Key<TTag>> where TTag : ITag, new()
    {
        public const int BASE_KEY_SIZE = sizeof(long) + sizeof(int);
        public static int KEY_SIZE { get; }
        public static Key<TTag> Empty { get; }

        static Key()
        {
            var emptyTag = new TTag();
            KEY_SIZE = emptyTag.Length + BASE_KEY_SIZE;
            Empty = new Key<TTag>();
        }

        /// <summary>
        /// The uniqe identification object for this key
        /// </summary>
        public TTag Tag { get; }
        /// <summary>
        /// The current position of this Key and the referenced <see cref="Value"/> in the value file
        /// </summary>
        public long Index { get; }
        /// <summary>
        /// The length of the referenced <see cref="Value"/> in the value file
        /// </summary>
        public int ValueLength { get; }
        /// <summary>
        /// The current position of the key in the <see cref="KeyStore{TTag}"/> file
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// Returns true if the Key is not valid. Comparing with default should have the same result
        /// </summary>
        public bool IsEmpty => ValueLength == 0 && Tag == null;

        public Key(TTag tag, long index, int length, long position)
        {
            Tag = tag;
            Index = index;
            ValueLength = length;
            Position = position;
        }

        public Key(TTag tag, long index, int length) : this(tag, index, length, -1)
        {
        }

        public byte[] GetBytes()
        {
            var byteArray = new byte[KEY_SIZE];
            Buffer.BlockCopy(BitConverter.GetBytes(Index), 0, byteArray, 0, sizeof(long));
            Buffer.BlockCopy(BitConverter.GetBytes(ValueLength), 0, byteArray, sizeof(long), sizeof(int));

            if (Tag != null)
                Buffer.BlockCopy(Tag.GetBytes(), 0, byteArray, BASE_KEY_SIZE, Tag.Length);

            return byteArray;
        }

        public void WriteBytes(Writer writer, long position, bool flush = false)
        {
            Span<byte> byteArray = stackalloc byte[KEY_SIZE];

            BitConverter.TryWriteBytes(byteArray, Index);
            BitConverter.TryWriteBytes(byteArray[sizeof(long)..], ValueLength);

            if (Tag is not null)
                Tag.WriteBytes(byteArray[BASE_KEY_SIZE..(BASE_KEY_SIZE + Tag.Length)]);

            if (flush)
                    writer.WriteAndFlush(byteArray, position);
            else
                    writer.Write(byteArray, position);
        }
        public void WriteBytes(Writer writer, bool flush = false)
        {
            Span<byte> byteArray = stackalloc byte[KEY_SIZE];

            BitConverter.TryWriteBytes(byteArray, Index);
            BitConverter.TryWriteBytes(byteArray[sizeof(long)..], ValueLength);

            if (Tag != null)
                Tag.WriteBytes(byteArray[BASE_KEY_SIZE..(BASE_KEY_SIZE + Tag.Length)]);

            if (flush)
                    writer.WriteAndFlush(byteArray);
            else
                    writer.Write(byteArray);
        }

        public static Key<TTag> FromBytes(byte[] array, int index)
        {
            var localIndex = BitConverter.ToInt64(array, index);
            var length = BitConverter.ToInt32(array, index + sizeof(long));
            var tag = InstanceCreator<TTag>.CreateInstance();
            tag.FromBytes(array, index + BASE_KEY_SIZE);

            return new Key<TTag>(tag, localIndex, length, index);
        }

        public override bool Equals(object obj)
            => obj is Key<TTag> key
            && Equals(key);
        public bool Equals(Key<TTag> other)
            => EqualityComparer<TTag>.Default.Equals(Tag, other.Tag)
               && ValueLength == other.ValueLength;

        public override int GetHashCode()
        {
            var hashCode = 139101280;
            hashCode = hashCode * -1521134295 + EqualityComparer<TTag>.Default.GetHashCode(Tag);
            hashCode = hashCode * -1521134295 + ValueLength.GetHashCode();
            return hashCode;
        }

        public bool Validate()
            => ValueLength >= 0
               && Position >= 0
               && Index >= 0
               && KEY_SIZE > BASE_KEY_SIZE;

        public static bool operator ==(Key<TTag> left, Key<TTag> right)
            => left.Equals(right);
        public static bool operator !=(Key<TTag> left, Key<TTag> right)
            => !(left == right);
    }
}
