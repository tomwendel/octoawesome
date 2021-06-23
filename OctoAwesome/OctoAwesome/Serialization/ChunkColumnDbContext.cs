using OctoAwesome.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Serialization
{
    public sealed class ChunkColumnDbContext : DatabaseContext<Index2Tag, IChunkColumn>
    {
        private readonly IPlanet currentPlanet;

        public ChunkColumnDbContext(Database<Index2Tag> database, IPlanet planet) : base(database) => currentPlanet = planet;

        public override void AddOrUpdate(IChunkColumn value)
        {
            using (Database.Lock(Operation.Write))
                Database.AddOrUpdate(new Index2Tag(value.Index), new Value(Serializer.SerializeCompressed(value, 2048)));
        }

        public IChunkColumn Get(Index2 key)
            => Get(new Index2Tag(key));
        public override IChunkColumn Get(Index2Tag key)
        {
            if (!Database.ContainsKey(key))
                return null;

            var chunkColumn = new ChunkColumn(currentPlanet);
            using (var stream = new MemoryStream(Database.GetValue(key).Content))
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            using(var buffered = new BufferedStream(zip))
            using (var reader = new BinaryReader(buffered))
            {
                chunkColumn.Deserialize(reader);
                return chunkColumn;
            }
        }

        public override void Remove(IChunkColumn value)
        {
            using (Database.Lock(Operation.Write))
                Database.Remove(new Index2Tag(value.Index));
        }
    }
}
