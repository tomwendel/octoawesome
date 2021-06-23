using engenious;
using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using OctoAwesome.Notifications;
using OctoAwesome.Serialization;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome
{
    public abstract class FunctionalBlock : ISerializable, IIdentification, IContainsComponents, INotificationSubject<SerializableNotification>
    {
        /// <summary>
        /// Contains all Components.
        /// </summary>
        public ComponentList<IFunctionalBlockComponent> Components { get; private set; }

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
        public FunctionalBlock()
        {
            Components = new(ValidateAddComponent, ValidateRemoveComponent, OnAddComponent, OnRemoveComponent);
            notificationComponents = new();
            Id = Guid.Empty;
        }

        private void OnRemoveComponent(IFunctionalBlockComponent component)
        {

        }

        private void OnAddComponent(IFunctionalBlockComponent component)
        {
            if (component is InstanceComponent<INotificationSubject<SerializableNotification>> instanceComponent)
                instanceComponent.SetInstance(this);

            if (component is INotificationSubject<SerializableNotification> nofiticationComponent)
                notificationComponents.Add(nofiticationComponent);
        }

        private void ValidateAddComponent(IFunctionalBlockComponent component)
        {
            if (Simulation != null)
                throw new NotSupportedException("Can't add components during simulation");
        }

        private void ValidateRemoveComponent(IFunctionalBlockComponent component)
        {
            if (Simulation != null)
                throw new NotSupportedException("Can't remove components during simulation");
        }

        public void Initialize(IResourceManager mananger)
        {
            OnInitialize(mananger);
        }

        public void Interact(GameTime gameTime, Entity entity)
        {
            OnInteract(gameTime, entity);
        }

        protected abstract void OnInteract(GameTime gameTime, Entity entity);

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
            if (obj is FunctionalBlock entity)
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
