using engenious;
using engenious.Graphics;

using OctoAwesome.Components;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.EntityComponents
{
    public class AnimationComponent : InstanceComponent<INotificationSubject<SerializableNotification>>, IEntityComponent, IFunctionalBlockComponent
    {
        public new const int Size = Component.Size + sizeof(float) * 3;

        public float CurrentTime { get => currentTime; set => SetValue(ref currentTime, value); }
        public float MaxTime { get => maxTime; set => SetValue(ref maxTime, value); }
        public float AnimationSpeed { get => animationSpeed; set => SetValue(ref animationSpeed, value); }

        private readonly IPool<PropertyChangedNotification> propertyChangedNotificationPool;
        private float currentTime;
        private float maxTime;
        private float animationSpeed;

        public AnimationComponent()
        {
            Sendable = true;
            propertyChangedNotificationPool = TypeContainer.Get<IPool<PropertyChangedNotification>>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(currentTime);
            writer.Write(maxTime);
            writer.Write(animationSpeed);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            currentTime = reader.ReadSingle();
            maxTime = reader.ReadSingle();
            animationSpeed = reader.ReadSingle();
        }

        protected override void Serialize(Span<byte> writer)
        {
            // Position
            base.Serialize(writer);
            var offset = Component.Size;
            BitConverter.TryWriteBytes(writer[offset..], currentTime);
            offset += sizeof(float);
            BitConverter.TryWriteBytes(writer[offset..], maxTime);
            offset += sizeof(float);
            BitConverter.TryWriteBytes(writer[offset..], animationSpeed);
        }

        protected override void OnPropertyChanged<T>(T value, string callerName)
        {
            base.OnPropertyChanged(value, callerName);

            var updateNotification = propertyChangedNotificationPool.Get();
            var arr = ArrayPool<byte>.Shared.Rent(Size);

            updateNotification.Issuer = nameof(AnimationComponent);
            updateNotification.Property = callerName;

            Serialize(arr.AsSpan());
            updateNotification.Value = arr;

            Push(updateNotification);
            ArrayPool<byte>.Shared.Return(arr);
            updateNotification.Release();
        }

       
        public override void OnNotification(SerializableNotification notification)
        {
            base.OnNotification(notification);

            if (notification is PropertyChangedNotification changedNotification)
            {
                if (changedNotification.Issuer == nameof(AnimationComponent))
                {

                    using (var stream = new MemoryStream(changedNotification.Value))
                    using (var reader = new BinaryReader(stream))
                    {
                        Deserialize(reader);
                    }
                }
            }
        }

        private float NextSmallerValue(float value)
        {
            if (value < 0)
                return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value) + 1);
            else if (value > 0)
                return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value) - 1);
            return -float.Epsilon;
        }

        public void Update(GameTime gameTime, Model model)
        {
            if (model.CurrentAnimation is null)
                return;

            currentTime = Math.Clamp(currentTime + AnimationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds, 0, NextSmallerValue(MaxTime));
            model.UpdateAnimation(currentTime);
        }
    }
}
