using OctoAwesome.Components;
using OctoAwesome.Database;
using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Serialization.Entities
{
    public sealed class EntityComponentsDbContext
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly Guid universeGuid;

        public EntityComponentsDbContext(IDatabaseProvider databaseProvider, Guid universe)
        {
            this.databaseProvider = databaseProvider;
            universeGuid = universe;
        }

        public void AddOrUpdate<T>(T value, Entity entity) where T : IEntityComponent
        {
            Database<GuidTag<T>> database = databaseProvider.GetDatabase<GuidTag<T>>(universeGuid, false);
            var tag = new GuidTag<T>(entity.Id);
            using (database.Lock(Operation.Write))
                database.AddOrUpdate(tag, new Value(Serializer.Serialize(value)));
        }

        public T Get<T>(Guid id) where T : IEntityComponent, new()
        {
            Database<GuidTag<T>> database = databaseProvider.GetDatabase<GuidTag<T>>(universeGuid, false);
            var tag = new GuidTag<T>(id);
            return Serializer.Deserialize<T>(database.GetValue(tag).Content);
        }
        public T Get<T>(Entity entity) where T : IEntityComponent, new()
            => Get<T>(entity.Id);

        public IEnumerable<GuidTag<T>> GetAllKeys<T>() where T : IEntityComponent
            => databaseProvider.GetDatabase<GuidTag<T>>(universeGuid, false).Keys;

        public void Remove<T>(Entity entity) where T : IEntityComponent
        {
            Database<GuidTag<T>> database = databaseProvider.GetDatabase<GuidTag<T>>(universeGuid, false);
            var tag = new GuidTag<T>(entity.Id);
            using (database.Lock(Operation.Write))
                database.Remove(tag);
        }

    }
}
