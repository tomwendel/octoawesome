using engenious;

using OctoAwesome.Components;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.EntityComponents
{
    public sealed class PositionComponent : InstanceComponent<INotificationSubject<SerializableNotification>>, IEntityComponent, IFunctionalBlockComponent
    {
        public new const int Size = Component.Size + sizeof(int) * 4 + sizeof(float) * 4;

        public Coordinate Position
        {
            get => position; set
            {
                var valueBlockX = ((int)(value.BlockPosition.X * 100)) / 100f;
                var valueBlockY = ((int)(value.BlockPosition.Y * 100)) / 100f;
                var positionBlockX = ((int)(position.BlockPosition.X * 100)) / 100f;
                var positionBlockY = ((int)(position.BlockPosition.Y * 100)) / 100f;

                posUpdate = valueBlockX != positionBlockX || valueBlockY != positionBlockY
                    || position.BlockPosition.Z != value.BlockPosition.Z;

                SetValue(ref position, value);
                TryUpdatePlanet(value.Planet);
            }
        }

        public float Direction { get; set; }
        public IPlanet Planet { get; private set; }

        private Coordinate position;
        private bool posUpdate;
        private readonly IResourceManager resourceManager;
        private readonly IPool<PropertyChangedNotification> propertyChangedNotificationPool;

        public PositionComponent()
        {
            Sendable = true;
            resourceManager = TypeContainer.Get<IResourceManager>();
            propertyChangedNotificationPool = TypeContainer.Get<IPool<PropertyChangedNotification>>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            // Position
            writer.Write(Position.Planet);
            writer.Write(Position.GlobalBlockIndex.X);
            writer.Write(Position.GlobalBlockIndex.Y);
            writer.Write(Position.GlobalBlockIndex.Z);
            writer.Write(Position.BlockPosition.X);
            writer.Write(Position.BlockPosition.Y);
            writer.Write(Position.BlockPosition.Z);
            writer.Write(Direction);
        }
        protected override void Serialize(Span<byte> writer)
        {
            base.Serialize(writer);
            // Position
            var offset = Component.Size;
            BitConverter.TryWriteBytes(writer[offset..], Position.Planet);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(writer[offset..], Position.GlobalBlockIndex.X);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(writer[offset..], Position.GlobalBlockIndex.Y);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(writer[offset..], Position.GlobalBlockIndex.Z);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(writer[offset..], Position.BlockPosition.X);
            offset += sizeof(float);
            BitConverter.TryWriteBytes(writer[offset..], Position.BlockPosition.Y);
            offset += sizeof(float);
            BitConverter.TryWriteBytes(writer[offset..], Position.BlockPosition.Z);
            offset += sizeof(float);
            BitConverter.TryWriteBytes(writer[offset..], Direction);
            offset += sizeof(float);


        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            // Position
            int planet = reader.ReadInt32();
            int blockX = reader.ReadInt32();
            int blockY = reader.ReadInt32();
            int blockZ = reader.ReadInt32();
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            float posZ = reader.ReadSingle();
            position = new Coordinate(planet, new Index3(blockX, blockY, blockZ), new Vector3(posX, posY, posZ));
            Direction = reader.ReadSingle();

            TryUpdatePlanet(planet);
        }

        private bool TryUpdatePlanet(int planetId)
        {
            if (Planet != null && Planet.Id == planetId)
                return false;

            Planet = resourceManager.GetPlanet(planetId);
            return true;
        }

        protected override void OnPropertyChanged<T>(T value, string callerName)
        {
            base.OnPropertyChanged(value, callerName);

            if (callerName == nameof(Position) && posUpdate)
            {
                var updateNotification = propertyChangedNotificationPool.Get();
                var arr = ArrayPool<byte>.Shared.Rent(Size);

                updateNotification.Issuer = nameof(PositionComponent);
                updateNotification.Property = callerName;

                Serialize(arr.AsSpan());
                updateNotification.Value = arr;

                Push(updateNotification);
                ArrayPool<byte>.Shared.Return(arr);
                updateNotification.Release();
            }
        }

        public override void OnNotification(SerializableNotification notification)
        {
            base.OnNotification(notification);

            if (notification is PropertyChangedNotification changedNotification)
            {
                if (changedNotification.Issuer == nameof(PositionComponent))
                {
                    if (changedNotification.Property == nameof(Position))
                    {
                        using (var stream = new MemoryStream(changedNotification.Value))
                        using (var reader = new BinaryReader(stream))
                        {
                            Deserialize(reader);
                        }
                    }
                }
            }
        }
    }
}
