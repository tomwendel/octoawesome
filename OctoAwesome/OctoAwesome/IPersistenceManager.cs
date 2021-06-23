using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using OctoAwesome.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OctoAwesome
{
    /// <summary>
    /// Interface für das Persistieren der Welt
    /// </summary>
    public interface IPersistenceManager
    {
        /// <summary>
        /// Gibt alle Universen zurück, die geladen werden können.
        /// </summary>
        /// <returns>Die Liste der Universen.</returns>
        Awaiter Load(out SerializableCollection<IUniverse> universes);

        /// <summary>
        /// Lädt das Universum mit der angegebenen Guid.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <returns>Das geladene Universum.</returns>
        Awaiter Load(out IUniverse universe, Guid universeGuid);

        /// <summary>
        /// Speichert das Universum.
        /// </summary>
        /// <param name="universe">Das zu speichernde Universum</param>
        void SaveUniverse(IUniverse universe);

        /// <summary>
        /// Löscht ein Universum.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        void DeleteUniverse(Guid universeGuid);

        /// <summary>
        /// Lädt einen Planeten.
        /// </summary>
        /// <param name="universeGuid">Guid des Universums</param>
        /// <param name="planetId">Index des Planeten</param>
        /// <returns></returns>
        Awaiter Load(out IPlanet planet, Guid universeGuid, int planetId);

        /// <summary>
        /// Speichert einen Planeten.
        /// </summary>
        /// <param name="universeGuid">Guid des Universums</param>
        /// <param name="planet">Zu speichernder Planet</param>
        void SavePlanet(Guid universeGuid, IPlanet planet);

        /// <summary>
        /// Lädt eine <see cref="IChunkColumn"/>.
        /// </summary>
        /// <param name="universeGuid">GUID des Universums.</param>
        /// <param name="planet">Index des Planeten.</param>
        /// <param name="columnIndex">Zu serialisierende ChunkColumn.</param>
        /// <returns>Die neu geladene ChunkColumn.</returns>
        Awaiter Load(out IChunkColumn column, Guid universeGuid, IPlanet planet, Index2 columnIndex);

        /// <summary>
        /// Speichert eine <see cref="IChunkColumn"/>.
        /// </summary>
        /// <param name="universeGuid">GUID des Universums.</param>
        /// <param name="planet">Planet der Column.</param>
        /// <param name="column">Zu serialisierende ChunkColumn.</param>
        void SaveColumn(Guid universeGuid, IPlanet planet, IChunkColumn column);

        /// <summary>
        /// Lädt einen Player.
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <param name="playername">Der Name des Spielers.</param>
        /// <returns></returns>
        Awaiter Load(out Player player, Guid universeGuid, string playername);

        /// <summary>
        /// Speichert einen Player
        /// </summary>
        /// <param name="universeGuid">Die Guid des Universums.</param>
        /// <param name="player">Der Player.</param>
        void SavePlayer(Guid universeGuid, Player player);
        IEnumerable<Entity> LoadEntitiesWithComponent<T>(Guid universeGuid) where T : IEntityComponent;
        void SaveEntity(Entity entity, Guid universe);
        Awaiter Load(out Entity entity, Guid universeGuid, Guid entityId);
        IEnumerable<Guid> GetEntityIdsFromComponent<T>(Guid universeGuid) where T : IEntityComponent;
        IEnumerable<(Guid Id, T Component)> GetEntityComponents<T>(Guid universeGuid, Guid[] entityIds) where T : IEntityComponent, new();
        IEnumerable<Guid> GetEntityIds(Guid universeGuid);
    }
}
