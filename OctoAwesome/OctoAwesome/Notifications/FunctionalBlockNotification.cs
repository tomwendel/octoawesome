using OctoAwesome.Pooling;
using OctoAwesome.Serialization;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Notifications
{
    public sealed class FunctionalBlockNotification : SerializableNotification
    {
        public ActionType Type { get; set; }
        public Guid BlockId { get; set; }
        public FunctionalBlock Block
        {
            get => block; set
            {
                block = value;
                BlockId = value?.Id ?? default;
            }
        }

        public PropertyChangedNotification Notification { get; set; }

        private FunctionalBlock block;

        private readonly IPool<PropertyChangedNotification> propertyChangedNotificationPool;

        public FunctionalBlockNotification()
        {
            propertyChangedNotificationPool = TypeContainer.Get<IPool<PropertyChangedNotification>>();
        }

        public FunctionalBlockNotification(Guid id) : this()
        {
            BlockId = id;
        }

        public override void Deserialize(BinaryReader reader)
        {
            Type = (ActionType)reader.ReadInt32();


            if (Type == ActionType.Add)
            {
                var typeName = reader.ReadString();
                var t = System.Type.GetType(typeName);
                var deMethod = typeof(Serializer).GetMethod(nameof(Serializer.Deserialize), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                deMethod = deMethod.MakeGenericMethod(t);
                var blockBytes = reader.ReadBytes(reader.ReadInt32());

                var parameterArr = new object[1];
                parameterArr[0] = blockBytes;
                Block = (FunctionalBlock)deMethod.Invoke(null, parameterArr);
                BlockId = Block.Id;
            }
            else
                BlockId = new Guid(reader.ReadBytes(16));

            var isNotification = reader.ReadBoolean();
            if (isNotification)
                Notification = Serializer.DeserializePoolElement(
                    propertyChangedNotificationPool, reader.ReadBytes(reader.ReadInt32()));

        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((int)Type);

            if (Type == ActionType.Add)
            {
                writer.Write(Block.GetType().AssemblyQualifiedName);
                var bytes = Serializer.Serialize(Block);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
                writer.Write(BlockId.ToByteArray());

            var subNotification = Notification != null;
            writer.Write(subNotification);
            if (subNotification)
            {
                var bytes = Serializer.Serialize(Notification);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        protected override void OnRelease()
        {
            Type = default;
            Block = default;

            base.OnRelease();
        }

        public enum ActionType
        {
            None,
            Add,
            Remove,
            Update,
            Request
        }
    }
}
