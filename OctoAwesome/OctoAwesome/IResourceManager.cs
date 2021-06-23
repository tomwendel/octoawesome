using OctoAwesome.Components;
using OctoAwesome.Definitions;
using OctoAwesome.EntityComponents;
using OctoAwesome.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OctoAwesome
{
    /// <summary>
    /// Interface, un die Ressourcen in OctoAwesome zu verfalten
    /// </summary>
    public interface IResourceManager
    {
        IDefinitionManager DefinitionManager { get; }

        /// <summary>
        /// Erzuegt ein neues Universum.
        /// </summary>
        /// <param name="name">Name des neuen Universums.</param>
        /// <param name="seed">Weltgenerator-Seed für das neue Universum.</param>
        /// <returns>Die Guid des neuen Universums.</returns>
        Guid NewUniverse(string name, int seed);

        /// <summary>
        /// Lädt das Universum für die angegebene GUID.
        /// </summary>
        /// <param name="universeId">Die Guid des Universums.</param>
        bool TryLoadUniverse(Guid universeId);

        /// <summary>
        /// Das aktuell geladene Universum.
        /// </summary>
        IUniverse CurrentUniverse { get; }

        /// <summary>
        /// Entlädt das aktuelle Universum.
        /// </summary>
        void UnloadUniverse();

        /// <summary>
        /// Gibt alle Universen zurück, die geladen werden können.
        /// </summary>
        /// <returns>Die Liste der Universen.</returns>
        IUniverse[] ListUniverses();

        /// <summary>
        /// Löscht ein Universum.
        /// </summary>
        /// <param name="id">Die Guid des Universums.</param>
        void DeleteUniverse(Guid id);

        /// <summary>
        /// Lädt einen Player.
        /// </summary>
        /// <param name="playername">Der Name des Players.</param>
        /// <returns></returns>
        Player LoadPlayer(string playername);

        /// <summary>
        /// Speichert einen Player.
        /// </summary>
        /// <param name="player">Der Player.</param>
        void SavePlayer(Player player);

        /// <summary>
        /// Entlädt das aktuelle Universum
        /// </summary>
        /// <returns>Das gewünschte Universum, falls es existiert</returns>
        IUniverse GetUniverse();

        /// <summary>
        /// Gibt den Planeten mit der angegebenen ID zurück
        /// </summary>
        /// <param name="planetId">Die Planteten-ID des gewünschten Planeten</param>
        /// <returns>Der gewünschte Planet, falls er existiert</returns>
        IPlanet GetPlanet(int planetId);
        ConcurrentDictionary<int, IPlanet> Planets { get; }

        IUpdateHub UpdateHub { get; }

        Player CurrentPlayer { get; }

        void SaveEntity(Entity entity);
        void SaveChunkColumn(IChunkColumn value);
        IChunkColumn LoadChunkColumn(IPlanet planet, Index2 index);
        Entity LoadEntity(Guid entityId);
        IEnumerable<Entity> LoadEntitiesWithComponent<T>() where T : IEntityComponent;
        IEnumerable<Guid> GetEntityIdsFromComponent<T>() where T : IEntityComponent;
        IEnumerable<Guid> GetEntityIds();
        (Guid Id, T Component)[] GetEntityComponents<T>(Guid[] entityIds) where T : IEntityComponent, new();
    }
}