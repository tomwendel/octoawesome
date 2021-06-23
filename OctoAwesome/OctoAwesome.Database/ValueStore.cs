using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OctoAwesome.Database
{
    internal class ValueStore : IDisposable
    {
        public bool FixedValueLength { get;  }

        private readonly Writer writer;
        private readonly Reader reader;

        public ValueStore(Writer writer, Reader reader, bool fixedValueLength)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader)); 
            FixedValueLength = fixedValueLength;
        }
        public ValueStore(Writer writer, Reader reader) : this(writer, reader, false)
        {

        }
        
        public Value GetValue<TTag>(Key<TTag> key) where TTag : ITag, new()
        {
            var byteArray = reader.Read(key.Index + Key<TTag>.KEY_SIZE, key.ValueLength);
            return new Value(byteArray);
        }

        internal Key<TTag> AddValue<TTag>(TTag tag, Value value) where TTag : ITag, new()
        {
            
            var key = new Key<TTag>(tag, writer.ToEnd(), value.Content.Length);
            //TODO: Hash, Sync
            key.WriteBytes(writer);
            writer.WriteAndFlush(value.Content, 0, value.Content.Length);
            return key;
        }

        /// <summary>
        /// Update a value on the exact <paramref name="key"/> index <see cref="Key{TTag}.Index"/> 
        /// </summary>
        /// <typeparam name="TTag"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException">If <see cref="FixedValueLength"/> is false</exception>
        internal void Update<TTag>(Key<TTag> key, Value value) where TTag : ITag, new()
        {
            if (!FixedValueLength)
                throw new NotSupportedException("Update is not allowed when value have no fixed size");

            writer.WriteAndFlush(value.Content, 0, key.ValueLength, key.Index + Key<TTag>.KEY_SIZE);
        }

        internal void Remove<TTag>(Key<TTag> key) where TTag : ITag, new()
        {
            Key<TTag>.Empty.WriteBytes(writer, key.Index);
            writer.WriteAndFlush(BitConverter.GetBytes(key.ValueLength), 0, sizeof(int), key.Index + Key<TTag>.KEY_SIZE);
        }

        internal void Open()
        {
            writer.Open();
        }

        internal void Close()
        {
            writer.Close();
        }

        public void Dispose()
        {
            writer.Dispose(); //TODO: Move to owner
        }
        
    }
}
