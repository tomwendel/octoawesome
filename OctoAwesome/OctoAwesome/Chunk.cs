using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using System;

namespace OctoAwesome
{
    /// <summary>
    /// Repräsentiert einen Karten-Abschnitt innerhalb des Planeten.
    /// </summary>
    public sealed class Chunk : IChunk
    {
        /// <summary>
        /// Zweierpotenz der Chunkgrösse. Ausserdem gibt es die Anzahl Bits an,
        /// die die X-Koordinate im Array <see cref="Blocks"/> verwendet.
        /// </summary>
        public const int LimitX = 4;
        /// <summary>
        /// Zweierpotenz der Chunkgrösse. Ausserdem gibt es die Anzahl Bits an,
        /// die die Y-Koordinate im Array <see cref="Blocks"/> verwendet.
        /// </summary>
        public const int LimitY = 4;
        /// <summary>
        /// Zweierpotenz der Chunkgrösse. Ausserdem gibt es die Anzahl Bits an,
        /// die die Z-Koordinate im Array <see cref="Blocks"/> verwendet.
        /// </summary>
        public const int LimitZ = 4;

        /// <summary>
        /// Größe eines Chunks in Blocks in X-Richtung.
        /// </summary>
        public const int CHUNKSIZE_X = 1 << LimitX;

        /// <summary>
        /// Größe eines Chunks in Blocks in Y-Richtung.
        /// </summary>
        public const int CHUNKSIZE_Y = 1 << LimitY;

        /// <summary>
        /// Größe eines Chunks in Blocks in Z-Richtung.
        /// </summary>
        public const int CHUNKSIZE_Z = 1 << LimitZ;

        /// <summary>
        /// Grösse eines Chunk als <see cref="Index3"/>
        /// </summary>
        public static readonly Index3 CHUNKSIZE = new Index3(CHUNKSIZE_X, CHUNKSIZE_Y, CHUNKSIZE_Z);
        private IChunkColumn chunkColumn;

        /// <summary>
        /// Array, das alle Blöcke eines Chunks enthält. Jeder eintrag entspricht einer Block-ID.
        /// Der Index ist derselbe wie bei <see cref="MetaData"/> und <see cref="Resources"/>.
        /// </summary>
        public ushort[] Blocks { get; private set; }

        /// <summary>
        /// Array, das die Metadaten zu den Blöcken eines Chunks enthält.
        /// Der Index ist derselbe wie bei <see cref="Blocks"/> und <see cref="Resources"/>.
        /// </summary>
        public int[] MetaData { get; private set; }

        /// <summary>
        /// Chunk Index innerhalb des Planeten.
        /// </summary>
        public Index3 Index { get; private set; }

        /// <summary>
        /// Referenz auf den Planeten.
        /// </summary>
        public IPlanet Planet { get; private set; }

        /// <summary>
        /// Erzeugt eine neue Instanz der Klasse Chunk
        /// </summary>
        /// <param name="pos">Position des Chunks</param>
        /// <param name="planet">Index des Planeten</param>
        public Chunk(Index3 pos, IPlanet planet)
        {
            Blocks = new ushort[CHUNKSIZE_X * CHUNKSIZE_Y * CHUNKSIZE_Z];
            MetaData = new int[CHUNKSIZE_X * CHUNKSIZE_Y * CHUNKSIZE_Z];

            Index = pos;
            Planet = planet;
        }

        /// <summary>
        /// Liefet den Block an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="index">Koordinate des Blocks innerhalb des Chunkgs</param>
        /// <returns>Die Block-ID an der angegebenen Koordinate</returns>
        public ushort GetBlock(Index3 index)
        {
            return GetBlock(index.X, index.Y, index.Z);
        }

        /// <summary>
        /// Liefet den Block an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks</param>
        /// <returns>Block-ID der angegebenen Koordinate</returns>
        public ushort GetBlock(int x, int y, int z)
        {
            return Blocks[GetFlatIndex(x, y, z)];
        }

        /// <summary>
        /// Überschreibt den Block an der angegebenen Koordinate.
        /// </summary>
        /// <param name="index">Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="block">Die neue Block-ID.</param>
        /// <param name="meta">(Optional) Metainformationen für den Block</param>
        public void SetBlock(Index3 index, ushort block, int meta = 0)
        {
            SetBlock(index.X, index.Y, index.Z, block);
        }
        /// <summary>
        /// Überschreibt den Block an der angegebenen Koordinate.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="block">Die neue Block-ID</param>
        /// <param name="meta">(Optional) Die Metadaten des Blocks</param>
        public void SetBlock(int x, int y, int z, ushort block, int meta = 0)
            => SetBlock(GetFlatIndex(x, y, z), new BlockInfo(x, y, z, block, meta));
        public void SetBlock(int flatIndex, BlockInfo blockInfo)
        {
            Blocks[flatIndex] = blockInfo.Block;
            MetaData[flatIndex] = blockInfo.Meta;
            Changed?.Invoke(this);

            BlockChanged(blockInfo);
        }

        public void SetBlocks(bool issueNotification, params BlockInfo[] blockInfos)
        {
            for (int i = 0; i < blockInfos.Length; i++)
            {
                var flatIndex = GetFlatIndex(blockInfos[i].Position);
                Blocks[flatIndex] = blockInfos[i].Block;
                MetaData[flatIndex] = blockInfos[i].Meta;
            }
            if (issueNotification)
            {
                Changed?.Invoke(this);

                BlocksChanged(blockInfos);
            }
        }

        public void FlagDirty()
        {
            Changed?.Invoke(this);
        }


        /// <summary>
        /// Gibt die Metadaten des Blocks an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <returns>Die Metadaten des angegebenen Blocks</returns>
        public int GetBlockMeta(int x, int y, int z)
        {
            return MetaData[GetFlatIndex(x, y, z)];
        }

        /// <summary>
        /// Ändert die Metadaten des Blockes an der angegebenen Koordinate. 
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="meta">Die neuen Metadaten</param>
        public void SetBlockMeta(int x, int y, int z, int meta)
        {
            MetaData[GetFlatIndex(x, y, z)] = meta;
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Liefert alle Ressourcen im Block an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <returns>Ein Array aller Ressourcen des Blocks</returns>
        public ushort[] GetBlockResources(int x, int y, int z)
        {
            return Array.Empty<ushort>();
        }

        /// <summary>
        /// Ändert die Ressourcen des Blocks an der angegebenen Koordinate
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="resources">Ein <see cref="ushort"/>-Array, das alle Ressourcen enthält</param>
        public void SetBlockResources(int x, int y, int z, ushort[] resources)
        {
            Changed?.Invoke(this);
        }

        public void SetColumn(IChunkColumn chunkColumn)
            => this.chunkColumn = chunkColumn;

        public void OnUpdate(SerializableNotification notification)
            => chunkColumn?.OnUpdate(notification);

        public void Update(SerializableNotification notification)
        {
            if (notification is BlockChangedNotification blockChanged)
            {
                var flatIndex = GetFlatIndex(blockChanged.BlockInfo.Position);
                Blocks[flatIndex] = blockChanged.BlockInfo.Block;
                MetaData[flatIndex] = blockChanged.BlockInfo.Meta;
                Changed?.Invoke(this);
            }
            else if (notification is BlocksChangedNotification blocksChanged)
            {
                foreach (var block in blocksChanged.BlockInfos)
                {
                    var flatIndex = GetFlatIndex(block.Position);
                    Blocks[flatIndex] = block.Block;
                    MetaData[flatIndex] = block.Meta;
                }

                Changed?.Invoke(this);
            }
        }

        private void BlockChanged(BlockInfo blockInfo)
        {
            var notification = TypeContainer.Get<IPool<BlockChangedNotification>>().Get();
            notification.BlockInfo = blockInfo;
            notification.ChunkPos = Index;
            notification.Planet = Planet.Id;

            OnUpdate(notification);

            notification.Release();
        }
        private void BlocksChanged(params BlockInfo[] blockInfos)
        {
            var notification = TypeContainer.Get<IPool<BlocksChangedNotification>>().Get();
            notification.BlockInfos = blockInfos;
            notification.ChunkPos = Index;
            notification.Planet = Planet.Id;

            OnUpdate(notification);

            notification.Release();
        }

        public event Action<IChunk> Changed;

        /// <summary>
        /// Liefert den Index des Blocks im abgeflachten Block-Array der angegebenen 3D-Koordinate zurück. Sollte die Koordinate ausserhalb
        /// der Chunkgrösse liegen, wird dies gewrapt.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate</param>
        /// <param name="y">Y-Anteil der Koordinate</param>
        /// <param name="z">Z-Anteil der Koordinate</param>
        /// <returns>Index innerhalb des flachen Arrays</returns>
        public static int GetFlatIndex(int x, int y, int z)
        {
            return ((z & (CHUNKSIZE_Z - 1)) << (LimitX + LimitY))
                   | ((y & (CHUNKSIZE_Y - 1)) << LimitX)
                   | (x & (CHUNKSIZE_X - 1));
        }
        /// <summary>
        /// Liefert den Index des Blocks im abgeflachten Block-Array der angegebenen 3D-Koordinate zurück. Sollte die Koordinate ausserhalb
        /// der Chunkgrösse liegen, wird dies gewrapt.
        /// </summary>
        /// <param name="position">Die aktuelle Blockposition</param>
        /// <returns>Index innerhalb des flachen Arrays</returns>
        public static int GetFlatIndex(Index3 position)
        {
            return ((position.Z & (CHUNKSIZE_Z - 1)) << (LimitX + LimitY))
                   | ((position.Y & (CHUNKSIZE_Y - 1)) << LimitX)
                   | (position.X & (CHUNKSIZE_X - 1));
        }
    }
}
