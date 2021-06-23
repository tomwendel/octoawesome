using OctoAwesome.Client.Components;
using OctoAwesome.Serialization;

using System;
using System.Buffers;
using System.IO;

namespace OctoAwesome.Client.Cache
{
    internal partial class VerticesForChunk : IDisposable, ISerializable
    {
        public int Version { get; set; }
        public Index3 ChunkPosition { get; set; }
        public VertexPositionNormalTextureLight[] Vertices { get; set; }

        public VerticesForChunk()
        {

        }
        public VerticesForChunk(int version, Index3 chunkPosition, VertexPositionNormalTextureLight[] vertices)
        {
            Version = version;
            ChunkPosition = chunkPosition;
            Vertices = vertices;
        }


        public void Deserialize(BinaryReader reader)
        {
            Version = reader.ReadInt32();
            ChunkPosition = new Index3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

            var length = reader.ReadInt32();
            Vertices = ArrayPool<VertexPositionNormalTextureLight>.Shared.Rent(length);
            for (int i = 0; i < length; i++)
                Vertices[i] = new VertexPositionNormalTextureLight(reader.ReadUInt32(), reader.ReadUInt32());
        }

        public void Dispose()
        {
            if (Vertices != null)
                ArrayPool<VertexPositionNormalTextureLight>.Shared.Return(Vertices);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(ChunkPosition.X);
            writer.Write(ChunkPosition.Y);
            writer.Write(ChunkPosition.Z);
            writer.Write(Vertices.Length);
            foreach (var vert in Vertices)
            {
                writer.Write(vert.PackedValue);
                writer.Write(vert.PackedValue2);

            }
        }
    }
}
