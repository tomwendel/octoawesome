using System;

namespace OctoAwesome
{
    /// <summary>
    /// Basisinterface für einen lokalen Chunkcache
    /// </summary>
    public interface ILocalChunkCache
    {
        /// <summary>
        /// Setzt den Zentrums-Chunk für diesen lokalen Cache.
        /// </summary>
        /// <param name="planet">Der Planet, auf dem sich der Chunk befindet</param>
        /// <param name="index">Die Koordinaten an der sich der Chunk befindet</param>
        /// <param name="successCallback">Routine die Aufgerufen werden soll, falls das setzen erfolgreich war oder nicht</param>
        bool SetCenter(IPlanet planet, Index2 index, Action<bool> successCallback = null);

        /// <summary>
        /// Aktueller Planet auf dem sich der Cache bezieht.
        /// </summary>
        IPlanet Planet { get; }

        /// <summary>
        /// Liefert den Chunk an der angegebenen Chunk-Koordinate zurück.
        /// </summary>
        /// <param name="index">Chunk Index</param>
        /// <returns>Instanz des Chunks</returns>
        IChunk GetChunk(Index3 index);

        /// <summary>
        /// Liefert den Chunk an der angegebenen Chunk-Koordinate zurück.
        /// </summary>
        /// <param name="x">X Koordinate</param>
        /// <param name="y">Y Koordinate</param>
        /// <param name="z">Z Koordinate</param>
        /// <returns>Instanz des Chunks</returns>
        IChunk GetChunk(int x, int y, int z);

        IPlanet LoadPlanet(int id);

        /// <summary>
        /// Setzt den Zentrums-Chunk für diesen lokalen Cache.
        /// </summary>
        /// <param name="planetid">ID des Planet, auf dem sich der Chunk befindet</param>
        /// <param name="index">Die Koordinaten an der sich der Chunk befindet</param>
        /// <param name="successCallback">Routine die Aufgerufen werden soll, falls das setzen erfolgreich war oder nicht</param>
        bool SetCenter(int planetid, Index2 index, Action<bool> successCallback = null);

        /// <summary>
        /// Leert den Cache und gibt sie beim GlobalChunkCache wieder frei
        /// </summary>
        void Flush();

        /// <summary>
        /// Liefert den Block an der angegebenen Block-Koodinate zurück.
        /// </summary>
        /// <param name="index">Block Index</param>
        /// <returns>Die Block-ID an der angegebenen Koordinate</returns>
        ushort GetBlock(Index3 index);

        /// <summary>
        /// Liefert den Block an der angegebenen Block-Koodinate zurück.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <returns>Die Block-ID an der angegebenen Koordinate</returns>
        ushort GetBlock(int x, int y, int z);


        /// <summary>
        /// Überschreibt den Block an der angegebenen Koordinate.
        /// </summary>
        /// <param name="index">Block-Koordinate</param>
        /// <param name="block">Die neue Block-ID.</param>
        void SetBlock(Index3 index, ushort block);

        /// <summary>
        /// Überschreibt den Block an der angegebenen Koordinate.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="block">Die neue Block-ID.</param>
        void SetBlock(int x, int y, int z, ushort block);

        /// <summary>
        /// Gibt die Metadaten des Blocks an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <returns>Die Metadaten des angegebenen Blocks</returns>
        int GetBlockMeta(int x, int y, int z);

        /// <summary>
        /// Gibt die Metadaten des Blocks an der angegebenen Koordinate zurück.
        /// </summary>
        /// <param name="index">Block-Koordinate</param>
        /// <returns>Die Metadaten des angegebenen Blocks</returns>
        int GetBlockMeta(Index3 index);

        /// <summary>
        /// Ändert die Metadaten des Blockes an der angegebenen Koordinate.
        /// </summary>
        /// <param name="x">X-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="y">Y-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="z">Z-Anteil der Koordinate des Blocks innerhalb des Chunks</param>
        /// <param name="meta">Die neuen Metadaten</param>
        void SetBlockMeta(int x, int y, int z, int meta);

        /// <summary>
        /// Ändert die Metadaten des Blockes an der angegebenen Koordinate.
        /// </summary>
        /// <param name="index">Block-Koordinate</param>
        /// <param name="meta">Die neuen Metadaten</param>
        void SetBlockMeta(Index3 index, int meta);

        /// <summary>
        /// Gibt das Höhenniveua der angegebenen Position zurück.
        /// </summary>
        /// <param name="x">Globale X-Koordinate in Blöcken.</param>
        /// <param name="y">Globale Y-Koordinate in Blöcken.</param>
        /// <returns>Git die Höhe in Blöcken zurück, oder -1 falls der zugehörige Chunk nicht geladen ist.</returns>
        int GroundLevel(int x, int y);
    }
}