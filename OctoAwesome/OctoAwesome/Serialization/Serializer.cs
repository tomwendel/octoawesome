using NLog.Internal;

using OctoAwesome.Pooling;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Serialization
{
    public static class Serializer
    {
        public static byte[] Serialize<T>(T obj) where T : ISerializable
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                obj.Serialize(writer);
                return stream.ToArray();
            }
        }

        public static byte[] SerializeCompressed<T>(T obj) where T : ISerializable
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.Default, true))
                    obj.Serialize(writer);

                using (var ms = new MemoryStream())
                {
                    stream.Position = 0;
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        stream.CopyTo(zip);

                    return ms.ToArray();
                }
            }
        }

        public static byte[] SerializeCompressed<T>(T obj, int capacity) where T : ISerializable
        {
            using (var stream = new MemoryStream(capacity))
            using (var zip = new GZipStream(stream, CompressionMode.Compress))
            using (var buff = new BufferedStream(zip))
            {
                using (var writer = new BinaryWriter(buff, Encoding.Default, true))
                    obj.Serialize(writer);

                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data) where T : ISerializable, new()
        {
            var obj = new T();
            InternalDeserialize(ref obj, data);
            return obj;
        }

        public static T DeserializeCompressed<T>(byte[] data) where T : ISerializable, new()
        {
            var obj = new T();
            InternalDeserializeCompressed(ref obj, data);
            return obj;
        }

        public static T DeserializePoolElement<T>(byte[] data) where T : ISerializable, IPoolElement, new()
        {
            var obj = TypeContainer.Get<IPool<T>>().Get();
            InternalDeserialize(ref obj, data);
            return obj;
        }

        public static T DeserializePoolElement<T>(IPool<T> pool, byte[] data) where T : ISerializable, IPoolElement, new()
        {
            var obj = pool.Get();
            InternalDeserialize(ref obj, data);
            return obj;
        }

        private static void InternalDeserialize<T>(ref T instance, byte[] data) where T : ISerializable
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                instance.Deserialize(reader);
            }
        }

        private static void InternalDeserializeCompressed<T>(ref T instance, byte[] data) where T : ISerializable
        {
            using (var stream = new MemoryStream(data))
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new BinaryReader(zip))
            {
                instance.Deserialize(reader);
            }
        }
    }
}
