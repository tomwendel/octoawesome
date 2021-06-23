using OctoAwesome.Components;
using OctoAwesome.Database;
using OctoAwesome.EntityComponents;
using OctoAwesome.Logging;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using OctoAwesome.Serialization;
using OctoAwesome.Serialization.Entities;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OctoAwesome.Runtime
{
    /// <summary>
    /// Persistiert Chunks auf die Festplatte.
    /// </summary>
    public class DiskPersistenceManager : IPersistenceManager, IDisposable, INotificationObserver
    {
        private const string UniverseFilename = "universe.info";

        private const string PlanetGeneratorInfo = "generator.info";

        private const string PlanetFilename = "planet.info";

        private DirectoryInfo root;
        private IUniverse currentUniverse;
        private readonly ISettings settings;
        private readonly IPool<Awaiter> awaiterPool;
        private readonly IPool<BlockChangedNotification> blockChangedNotificationPool;
        private readonly IDisposable chunkSubscription;
        private readonly IExtensionResolver extensionResolver;
        private readonly DatabaseProvider databaseProvider;

        public DiskPersistenceManager(IExtensionResolver extensionResolver, ISettings Settings, IUpdateHub updateHub)
        {
            this.extensionResolver = extensionResolver;
            settings = Settings;
            databaseProvider = new DatabaseProvider(GetRoot(), TypeContainer.Get<ILogger>());
            awaiterPool = TypeContainer.Get<IPool<Awaiter>>();
            blockChangedNotificationPool = TypeContainer.Get<IPool<BlockChangedNotification>>();
            chunkSubscription = updateHub.Subscribe(this, DefaultChannels.Chunk);
        }

        private string GetRoot()
        {
            if (root != null)
                return root.FullName;

            string appconfig = settings.Get<string>("ChunkRoot");
            if (!string.IsNullOrEmpty(appconfig))
            {
                root = new DirectoryInfo(appconfig);
                if (!root.Exists) root.Create();
                return root.FullName;
            }
            else
            {
                var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                root = new DirectoryInfo(exePath + Path.DirectorySeparatorChar + "OctoMap");
                if (!root.Exists) root.Create();
                return root.FullName;
            }
        }

        /// <summary>
        /// Speichert das Universum.
        /// </summary>
        /// <param name="universe">Das zu speichernde Universum</param>
        public void SaveUniverse(IUniverse universe)
        {
            string path = Path.Combine(GetRoot(), universe.Id.ToString());
            Directory.CreateDirectory(path);
            currentUniverse = universe;
            string file = Path.Combine(path, UniverseFilename);
            using (Stream stream = File.Open(file, FileMode.Create, FileAccess.Write))
            using (GZipStream zip = new GZipStream(stream, CompressionMode.Compress))
            using (var writer = new BinaryWriter(zip))
            {
                universe.Serialize(writer);
            }
        }

        /// <summary>
        /// Löscht ein Universum.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        public void DeleteUniverse(Guid universeGuid)
        {
            string path = Path.Combine(GetRoot(), universeGuid.ToString());
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Speichert einen Planeten.
        /// </summary>
        /// <param name="universeGuid">Guid des Universums</param>
        /// <param name="planet">Zu speichernder Planet</param>
        public void SavePlanet(Guid universeGuid, IPlanet planet)
        {
            string path = Path.Combine(GetRoot(), universeGuid.ToString(), planet.Id.ToString());
            Directory.CreateDirectory(path);

            string generatorInfo = Path.Combine(path, PlanetGeneratorInfo);
            using (Stream stream = File.Open(generatorInfo, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    bw.Write(planet.Generator.GetType().FullName!);
                }
            }

            string file = Path.Combine(path, PlanetFilename);
            using (Stream stream = File.Open(file, FileMode.Create, FileAccess.Write))
            using (GZipStream zip = new GZipStream(stream, CompressionMode.Compress))
            using (BinaryWriter writer = new BinaryWriter(zip))
                planet.Serialize(writer);
        }

        /// <summary>
        /// Speichert eine <see cref="IChunkColumn"/>.
        /// </summary>
        /// <param name="universeGuid">GUID des Universums.</param>
        /// <param name="planet">Index des Planeten.</param>
        /// <param name="column">Zu serialisierende ChunkColumn.</param>
        public void SaveColumn(Guid universeGuid, IPlanet planet, IChunkColumn column)
        {
            var chunkColumContext = new ChunkColumnDbContext(databaseProvider.GetDatabase<Index2Tag>(universeGuid, planet.Id, false), planet);
            chunkColumContext.AddOrUpdate(column);
        }

        /// <summary>
        /// Speichert einen Player
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <param name="player">Der Player.</param>
        public void SavePlayer(Guid universeGuid, Player player)
        {
            string path = Path.Combine(GetRoot(), universeGuid.ToString());
            Directory.CreateDirectory(path);

            // TODO: Player Name berücksichtigen
            string file = Path.Combine(path, "player.info");
            using (Stream stream = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    player.Serialize(writer);
                }
            }
        }

        public void SaveEntity(Entity entity, Guid universe)
        {
            var context = new EntityDbContext(databaseProvider, universe);
            context.AddOrUpdate(entity);
        }

        /// <summary>
        /// Gibt alle Universen zurück, die geladen werden können.
        /// </summary>
        /// <returns>Die Liste der Universen.</returns>
        public Awaiter Load(out SerializableCollection<IUniverse> universes)
        {
            string root = GetRoot();
            var awaiter = awaiterPool.Get();
            universes = new SerializableCollection<IUniverse>();
            awaiter.Serializable = universes;
            foreach (var folder in Directory.GetDirectories(root))
            {
                string id = Path.GetFileNameWithoutExtension(folder);//folder.Replace(root + "\\", "");
                if (Guid.TryParse(id, out Guid guid))
                {
                    Load(out var universe, guid).WaitOnAndRelease();
                    universes.Add(universe);
                }
            }
            awaiter.SetResult(universes);

            return awaiter;
        }

        /// <summary>
        /// Lädt das Universum mit der angegebenen Guid.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <returns>Das geladene Universum.</returns>
        public Awaiter Load(out IUniverse universe, Guid universeGuid)
        {
            string file = Path.Combine(GetRoot(), universeGuid.ToString(), UniverseFilename);
            universe = new Universe();
            currentUniverse = universe;
            if (!File.Exists(file))
                return null;

            using (Stream stream = File.Open(file, FileMode.Open, FileAccess.Read))
            using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new BinaryReader(zip))
            {
                var awaiter = awaiterPool.Get();
                universe.Deserialize(reader);
                awaiter.SetResult(universe);
                return awaiter;
            }

        }

        /// <summary>
        /// Lädt einen Planeten.
        /// </summary>
        /// <param name="universeGuid">Guid des Universums</param>
        /// <param name="planetId">Index des Planeten</param>
        /// <returns></returns>
        public Awaiter Load(out IPlanet planet, Guid universeGuid, int planetId)
        {
            string file = Path.Combine(GetRoot(), universeGuid.ToString(), planetId.ToString(), PlanetFilename);
            string generatorInfo = Path.Combine(GetRoot(), universeGuid.ToString(), planetId.ToString(), PlanetGeneratorInfo);
            planet = new Planet();
            if (!File.Exists(generatorInfo) || !File.Exists(file))
                return null;

            IMapGenerator generator = null;
            using (Stream stream = File.Open(generatorInfo, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader bw = new BinaryReader(stream))
                {
                    string generatorName = bw.ReadString();
                    generator = extensionResolver.GetMapGenerator().FirstOrDefault(g => g.GetType().FullName!.Equals(generatorName));
                }
            }

            if (generator == null)
                throw new Exception("Unknown Generator");


            using (Stream stream = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
                {
                    var awaiter = awaiterPool.Get();
                    planet = generator.GeneratePlanet(zip);
                    awaiter.SetResult(planet);
                    return awaiter;
                }
            }
        }

        /// <summary>
        /// Lädt eine <see cref="IChunkColumn"/>.
        /// </summary>
        /// <param name="universeGuid">GUID des Universums.</param>
        /// <param name="planet">Index des Planeten.</param>
        /// <param name="columnIndex">Zu serialisierende ChunkColumn.</param>
        /// <returns>Die neu geladene ChunkColumn.</returns>
        public Awaiter Load(out IChunkColumn column, Guid universeGuid, IPlanet planet, Index2 columnIndex)
        {
            var chunkColumContext = new ChunkColumnDbContext(databaseProvider.GetDatabase<Index2Tag>(universeGuid, planet.Id, false), planet);

            column = chunkColumContext.Get(columnIndex);

            if (column == null)
                return null;
            //var localColumn = column;

            ApplyChunkDiff(column, universeGuid, planet);

            var awaiter = awaiterPool.Get();
            awaiter.SetResult(column);
            return awaiter;
        }

        public Awaiter Load(out Entity entity, Guid universeGuid, Guid entityId)
        {
            var entityContext = new EntityDbContext(databaseProvider, universeGuid);
            entity = entityContext.Get(new GuidTag<Entity>(entityId));

            var awaiter = awaiterPool.Get();
            awaiter.SetResult(entity);
            return awaiter;
        }


        /// <summary>
        /// Lädt einen Player.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <param name="playername">Der Name des Spielers.</param>
        /// <returns></returns>
        public Awaiter Load(out Player player, Guid universeGuid, string playername)
        {
            //TODO: Später durch Playername ersetzen
            string file = Path.Combine(GetRoot(), universeGuid.ToString(), "player.info");
            player = new Player();
            if (!File.Exists(file))
                return null;

            using (Stream stream = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    try
                    {
                        var awaiter = awaiterPool.Get();
                        awaiter.Serializable = player;
                        player.Deserialize(reader);
                        awaiter.SetResult(player);
                        return awaiter;
                    }
                    catch (Exception ex)
                    {
                        // File.Delete(file);
                    }
                }
            }

            return null;
        }

        public IEnumerable<Entity> LoadEntitiesWithComponent<T>(Guid universeGuid) where T : IEntityComponent
            => new EntityDbContext(databaseProvider, universeGuid).GetEntitiesWithComponent<T>();

        public IEnumerable<Guid> GetEntityIdsFromComponent<T>(Guid universeGuid) where T : IEntityComponent
            => new EntityDbContext(databaseProvider, universeGuid).GetEntityIdsFromComponent<T>().Select(i => i.Tag);
        public IEnumerable<Guid> GetEntityIds(Guid universeGuid)
            => new EntityDbContext(databaseProvider, universeGuid).GetAllKeys().Select(i => i.Tag);

        public IEnumerable<(Guid Id, T Component)> GetEntityComponents<T>(Guid universeGuid, Guid[] entityIds) where T : IEntityComponent, new()
        {
            foreach (var entityId in entityIds)
                yield return (entityId, new EntityComponentsDbContext(databaseProvider, universeGuid).Get<T>(entityId));
        }

        public void Dispose()
        {
            databaseProvider.Dispose();
            chunkSubscription.Dispose();
        }

        public void OnCompleted() { }

        public void OnError(Exception error)
            => throw error;

        public void OnNext(Notification notification)
        {
            if (notification is BlockChangedNotification blockChanged)
                SaveChunk(blockChanged);
            else if (notification is BlocksChangedNotification blocksChanged)
                SaveChunk(blocksChanged);
        }

        private void SaveChunk(BlockChangedNotification chunkNotification)
        {
            var database = databaseProvider.GetDatabase<ChunkDiffTag>(currentUniverse.Id, chunkNotification.Planet, true);
            var databaseContext = new ChunkDiffDbContext(database, blockChangedNotificationPool);
            databaseContext.AddOrUpdate(chunkNotification);
        }

        private void SaveChunk(BlocksChangedNotification chunkNotification)
        {
            var database = databaseProvider.GetDatabase<ChunkDiffTag>(currentUniverse.Id, chunkNotification.Planet, true);
            var databaseContext = new ChunkDiffDbContext(database, blockChangedNotificationPool);
            databaseContext.AddOrUpdate(chunkNotification);
        }

        private void ApplyChunkDiff(IChunkColumn column, Guid universeGuid, IPlanet planet)
        {
            var database = databaseProvider.GetDatabase<ChunkDiffTag>(universeGuid, planet.Id, true);
            var databaseContext = new ChunkDiffDbContext(database, blockChangedNotificationPool);
            var keys = databaseContext.GetAllKeys();

            for (int i = 0; i < keys.Count; i++)
            {
                ChunkDiffTag key = keys[i];
                if (key.ChunkPositon.X == column.Index.X && key.ChunkPositon.Y == column.Index.Y)
                {
                    var block = databaseContext.Get(key);
                    column.Chunks[key.ChunkPositon.Z].Blocks[key.FlatIndex] = block.BlockInfo.Block;
                    column.Chunks[key.ChunkPositon.Z].MetaData[key.FlatIndex] = block.BlockInfo.Meta;
                }
            }

            if (keys.Count > 1000)
            {
                SaveColumn(universeGuid, planet, column);
                databaseContext.Remove(keys);
            }
        }
    }
}
