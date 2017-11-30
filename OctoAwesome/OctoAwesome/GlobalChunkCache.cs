using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OctoAwesome
{
    /// <summary>
    /// Globaler Cache für Chunks
    /// </summary>
    public sealed class GlobalChunkCache : IGlobalChunkCache
    {
        /// <summary>
        /// Dictionary, das alle <see cref="CacheItem"/>s hält.
        /// </summary>
        private readonly Dictionary<Index3, CacheItem> cache;

        private Queue<CacheItem> newchunks;

        private Queue<CacheItem> oldchunks;

        private object updatelockobject = new object();

        /// <summary>
        /// Funktion, die für das Laden der Chunks verwendet wird
        /// </summary>
        private readonly Func<int, Index2, IChunkColumn> loadDelegate;

        /// <summary>
        /// Routine, die für das Speichern der Chunks verwendet wird.
        /// </summary>
        private readonly Action<int, Index2, IChunkColumn> saveDelegate;

        private readonly Func<int, IPlanet> loadPlanetDelagte;

        /// <summary>
        /// Objekt, das für die Locks benutzt wird
        /// </summary>
        private readonly object lockObject = new object();

        // TODO: Früher oder später nach draußen auslagern
        private Thread cleanupThread;


        /// <summary>
        /// Gibt die Anzahl der aktuell geladenen Chunks zurück.
        /// </summary>
        public int LoadedChunkColumns
        {
            get
            {
                lock (lockObject)
                {
                    return cache.Count;
                }
            }
        }

        /// <summary>
        /// Anzahl der noch nicht gespeicherten ChunkColumns.
        /// </summary>
        public int DirtyChunkColumn => _dirtyItems.Count;

        /// <summary>
        /// Erzeugt eine neue Instaz der Klasse GlobalChunkCache
        /// </summary>
        /// <param name="loadDelegate">Delegat, der nicht geladene ChunkColumns nachläd.</param>
        /// <param name="saveDelegate">Delegat, der nicht mehr benötigte ChunkColumns abspeichert.</param>
        public GlobalChunkCache(Func<int, Index2, IChunkColumn> loadDelegate, Func<int, IPlanet> loadPlanetDelegate,
            Action<int, Index2, IChunkColumn> saveDelegate)
        {
            if (loadDelegate == null) throw new ArgumentNullException("loadDelegate");
            if (saveDelegate == null) throw new ArgumentNullException("saveDelegate");
            if (loadPlanetDelegate == null) throw new ArgumentNullException(nameof(loadPlanetDelegate));

            this.loadDelegate = loadDelegate;
            this.saveDelegate = saveDelegate;
            this.loadPlanetDelagte = loadPlanetDelegate;

            cache = new Dictionary<Index3, CacheItem>();
            newchunks = new Queue<CacheItem>();
            oldchunks = new Queue<CacheItem>();

            cleanupThread = new Thread(BackgroundCleanup)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            cleanupThread.Start();
        }

        /// <summary>
        /// Abonniert einen Chunk.
        /// </summary>
        /// <param name="planet">Die Id des Planeten</param>
        /// <param name="position">Position des Chunks</param>
        /// <returns></returns>
        public IChunkColumn Subscribe(int planet, Index2 position, bool passive)
        {
            CacheItem cacheItem = null;

            lock (lockObject)
            {
                if (!cache.TryGetValue(new Index3(position, planet), out cacheItem))
                {
                    //TODO: Überdenken
                    if (passive)
                    {
                        return null;
                    }

                    cacheItem = new CacheItem()
                    {
                        Planet = planet,
                        Index = position,
                        References = 0,
                        PassiveReference = 0,
                        ChunkColumn = null,
                    };
                    cacheItem.Changed += ItemChanged;
                    //_dirtyItems.Enqueue(cacheItem);
                    cache.Add(new Index3(position, planet), cacheItem);
                    //_autoResetEvent.Set();
                }

                if (passive)
                    cacheItem.PassiveReference++;
                else
                    cacheItem.References++;

            }

            lock (cacheItem)
            {
                if (cacheItem.ChunkColumn == null)
                {
                    cacheItem.ChunkColumn = loadDelegate(planet, position);

                    lock (updatelockobject)
                    {
                        newchunks.Enqueue(cacheItem);
                    }
                }
            }

            return cacheItem.ChunkColumn;
        }

        public bool IsChunkLoaded(int planet, Index2 position)
        {
            return cache.ContainsKey(new Index3(position, planet));
        }

        private void ItemChanged(CacheItem obj)
        {
            _dirtyItems.Enqueue(obj);
            _autoResetEvent.Set();
        }

        /// <summary>
        /// Liefert den Chunk, sofern geladen.
        /// </summary>
        /// <param name="planet">Die Id des Planeten</param>
        /// <param name="position">Die Position des zurückzugebenden Chunks</param>
        /// <returns>Chunk Instanz oder null, falls nicht geladen</returns>
        public IChunkColumn Peek(int planet, Index2 position)
        {
            CacheItem cacheItem = null;
            if (cache.TryGetValue(new Index3(position, planet), out cacheItem))
            {
                return cacheItem.ChunkColumn;
            }
            return null;
        }


        /// <summary>
        /// Löscht den gesamten Inhalt des Caches.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                foreach (var value in cache.Values)
                {
                    value.References = 0;
                    value.PassiveReference = 0;
                    _unreferencedItems.Enqueue(value);
                }

            }
            _autoResetEvent.Set();
        }

        /// <summary>
        /// Gibt einen abonnierten Chunk wieder frei.
        /// </summary>
        /// <param name="planet">Die Id des Planeten</param>
        /// <param name="position">Die Position des freizugebenden Chunks</param>
        public void Release(int planet, Index2 position, bool passive)
        {
            CacheItem cacheItem = null;
            lock (lockObject)
            {
                if (!cache.TryGetValue(new Index3(position, planet), out cacheItem))
                {
                    if (!passive)
                    {
                        throw new NotSupportedException(string.Format("Kein Chunk für die Position ({0}) im Cache", position));
                    }

                    return;
                }

                if (passive)
                {
                    if (cacheItem != null)
                        cacheItem.PassiveReference--;
                }
                else
                {
                    if (--cacheItem.References <= 0)
                    {
                        _unreferencedItems.Enqueue(cacheItem);
                        _autoResetEvent.Set();
                    }
                }
            }
        }

        private readonly ConcurrentQueue<CacheItem> _dirtyItems = new ConcurrentQueue<CacheItem>();
        private readonly ConcurrentQueue<CacheItem> _unreferencedItems = new ConcurrentQueue<CacheItem>();
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private void BackgroundCleanup()
        {
            while (true)
            {
                _autoResetEvent.WaitOne();
                var itemsToSave = new List<CacheItem>();
                CacheItem ci;
                while (_dirtyItems.TryDequeue(out ci))
                {
                    itemsToSave.Add(ci);
                }

                foreach (var item in itemsToSave.Distinct())
                {
                    lock (item)
                    {
                        saveDelegate(item.Planet, item.Index, item.ChunkColumn);
                    }

                }

                while (_unreferencedItems.TryDequeue(out ci))
                {
                    lock (ci)
                    {
                        if (ci.References <= 0)
                        {
                            var key = new Index3(ci.Index, ci.Planet);
                            lock (lockObject)
                            {
                                ci.Changed -= ItemChanged;
                                cache.Remove(key);
                            }
                            lock (updatelockobject)
                            {
                                oldchunks.Enqueue(ci);
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Gibt einen Planenten anhand seiner ID zurück
        /// </summary>
        /// <param name="id">ID des Planeten</param>
        /// <returns>Planet</returns>
        public IPlanet GetPlanet(int id)
        {
            return loadPlanetDelagte(id);
        }

        public void BeforSimulationUpdate(Simulation simulation)
        {
            lock (updatelockobject)
            {
                //Neue Chunks in die Simulation einpflegen
                while (newchunks.Count > 0)
                {
                    var chunk = newchunks.Dequeue();
                    foreach (var entity in chunk.ChunkColumn.Entities)
                    {
                        simulation.AddEntity(entity);
                    }
                }

                //Alte Chunks aus der Siumaltion entfernen
                while (oldchunks.Count > 0)
                {
                    var chunk = oldchunks.Dequeue();
                    foreach (var entity in chunk.ChunkColumn.Entities)
                    {
                        simulation.RemoveEntity(entity);
                    }
                }
            }
        }

        public void AfterSimulationUpdate(Simulation simulation)
        {
            //TODO: Überarbeiten

            lock (lockObject)
            {
                var failchunkentities = (from chunk in cache
                                         where chunk.Value.ChunkColumn != null
                                         from entity in chunk.Value.ChunkColumn.Entities.FailChunkEntity()
                                         select entity).ToArray();
                foreach (var entity in failchunkentities)
                {
                    var currentchunk = Peek(entity.CurrentPlanet, entity.CurrentChunk);
                    var targetchunk = Peek(entity.TargetPlanet, entity.TargetChunk);

                    currentchunk.Entities.Remove(entity.Entity);

                    if (targetchunk != null)
                    {
                        targetchunk.Entities.Add(entity.Entity);
                    }
                    else
                    {
                        targetchunk = loadDelegate(entity.CurrentPlanet, entity.TargetChunk);
                        targetchunk.Entities.Add(entity.Entity);
                        saveDelegate(entity.CurrentPlanet, entity.TargetChunk, targetchunk);
                        simulation.RemoveEntity(entity.Entity);
                    }
                }

            }
        }

        /// <summary>
        /// Element für den Cache
        /// </summary>
        private class CacheItem
        {
            private IChunkColumn _chunkColumn;
            public int Planet { get; set; }

            public Index2 Index { get; set; }

            /// <summary>
            /// Die Zahl der Subscriber, die das Item Abboniert hat.
            /// </summary>
            public int References { get; set; }

            public int PassiveReference { get; set; }

            /// <summary>
            /// Der Chunk, auf den das <see cref="CacheItem"/> referenziert
            /// </summary>
            public IChunkColumn ChunkColumn
            {
                get { return _chunkColumn; }
                set
                {
                    if (_chunkColumn != null)
                        _chunkColumn.Changed -= OnChanged;

                    _chunkColumn = value;

                    if (value != null)
                        value.Changed += OnChanged;
                }
            }

            public event Action<CacheItem> Changed;
            private void OnChanged(IChunkColumn arg1, IChunk arg2, int arg3)
            {
                Changed?.Invoke(this);
            }

        }
    }
}
