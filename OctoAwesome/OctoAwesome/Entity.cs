using engenious;
using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using OctoAwesome.Notifications;
using OctoAwesome.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace OctoAwesome
{
    /// <summary>
    /// Basisklasse für alle selbständigen Wesen
    /// </summary>
    public abstract class Entity : ISerializable, IIdentification, IContainsComponents, INotificationSubject<SerializableNotification>
    {
        /// <summary>
        /// Contains all Components.
        /// </summary>
        public ComponentList<IEntityComponent> Components { get; private set; }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Reference to the active Simulation.
        /// </summary>
        public Simulation Simulation { get; internal set; }

        /// <summary>
        /// Contains only Components with notification interface implementation.
        /// </summary>
        private readonly List<INotificationSubject<SerializableNotification>> notificationComponents;

        /// <summary>
        /// Entity die regelmäßig eine Updateevent bekommt
        /// </summary>
        public Entity()
        {
            Components = new(ValidateAddComponent, ValidateRemoveComponent, OnAddComponent, OnRemoveComponent);
            notificationComponents = new();
            Id = Guid.Empty;
        }

        private void OnRemoveComponent(IEntityComponent component)
        {

        }

        private void OnAddComponent(IEntityComponent component)
        {
            if (component is InstanceComponent<INotificationSubject<SerializableNotification>> instanceComponent)
                instanceComponent.SetInstance(this);

            //HACK: Remove PositionComponent Dependency
            if (component is LocalChunkCacheComponent cacheComponent)
            {
                if (cacheComponent.LocalChunkCache != null)
                    return;

                var positionComponent = Components.GetComponent<PositionComponent>();

                if (positionComponent == null)
                    return;

                cacheComponent.LocalChunkCache = new LocalChunkCache(positionComponent.Planet.GlobalChunkCache, 4, 2);
            }

            if (component is INotificationSubject<SerializableNotification> nofiticationComponent)
                notificationComponents.Add(nofiticationComponent);
        }

        private void ValidateAddComponent(IEntityComponent component)
        {
            if (Simulation != null)
                throw new NotSupportedException("Can't add components during simulation");
        }

        private void ValidateRemoveComponent(IEntityComponent component)
        {
            if (Simulation != null)
                throw new NotSupportedException("Can't remove components during simulation");
        }

        public void Initialize(IResourceManager mananger)
        {
            OnInitialize(mananger);
        }

        protected virtual void OnInitialize(IResourceManager manager)
        {
        }

        /// <summary>
        /// Serialisiert die Entität mit dem angegebenen BinaryWriter.
        /// </summary>
        /// <param name="writer">Der BinaryWriter, mit dem geschrieben wird.</param>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(Id.ToByteArray());

            Components.Serialize(writer);
        }

        /// <summary>
        /// Deserialisiert die Entität aus dem angegebenen BinaryReader.
        /// </summary>
        /// <param name="reader">Der BinaryWriter, mit dem gelesen wird.</param>
        public virtual void Deserialize(BinaryReader reader)
        {
            Id = new Guid(reader.ReadBytes(16));
            Components.Deserialize(reader);
        }

        public virtual void RegisterDefault()
        {

        }

        public override int GetHashCode()
            => Id.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Entity entity)
                return entity.Id == Id;

            return base.Equals(obj);
        }

        public virtual void OnNotification(SerializableNotification notification)
        {
        }

        public virtual void Push(SerializableNotification notification)
        {
            foreach (var component in notificationComponents)
                component?.OnNotification(notification);
        }

        public bool ContainsComponent<T>() 
            => Components.ContainsComponent<T>();
        public T GetComponent<T>()
            => Components.GetComponent<T>();
    }
}
