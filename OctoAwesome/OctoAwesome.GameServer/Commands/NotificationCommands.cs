using CommandManagementSystem.Attributes;
using OctoAwesome.Network;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using OctoAwesome.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.GameServer.Commands
{
    public static class NotificationCommands
    {
        private static readonly IUpdateHub updateHub;
        private static readonly IPool<EntityNotification> entityNotificationPool;
        private static readonly IPool<FunctionalBlockNotification> functionalBlockNotificationPool;
        private static readonly IPool<BlockChangedNotification> blockChangedNotificationPool;
        private static readonly IPool<BlocksChangedNotification> blocksChangedNotificationPool;

        static NotificationCommands()
        {
            updateHub = TypeContainer.Get<IUpdateHub>();
            entityNotificationPool = TypeContainer.Get<IPool<EntityNotification>>();
            functionalBlockNotificationPool = TypeContainer.Get<IPool<FunctionalBlockNotification>>();
            blockChangedNotificationPool = TypeContainer.Get<IPool<BlockChangedNotification>>();
            blocksChangedNotificationPool = TypeContainer.Get<IPool<BlocksChangedNotification>>();
        }

        [Command((ushort)OfficialCommand.EntityNotification)]
        public static byte[] EntityNotification(CommandParameter parameter)
        {
            Console.WriteLine("Incomming Entity Notification");

            var entityNotification = Serializer.DeserializePoolElement(entityNotificationPool, parameter.Data);
            entityNotification.SenderId = parameter.ClientId;
            updateHub.Push(entityNotification, DefaultChannels.Simulation);
            updateHub.Push(entityNotification, DefaultChannels.Network);
            entityNotification.Release();
            return null;
        }

        [Command((ushort)OfficialCommand.FunctionalBlockNotification)]
        public static byte[] FunctionalBlockNotification(CommandParameter parameter)
        {
            Console.WriteLine("Incomming Functionblock Notification");

            var entityNotification = Serializer.DeserializePoolElement(functionalBlockNotificationPool, parameter.Data);
            entityNotification.SenderId = parameter.ClientId;
            updateHub.Push(entityNotification, DefaultChannels.Simulation);
            updateHub.Push(entityNotification, DefaultChannels.Network);
            entityNotification.Release();
            return null;
        }

        [Command((ushort)OfficialCommand.ChunkNotification)]
        public static byte[] ChunkNotification(CommandParameter parameter)
        {
            var notificationType = (BlockNotificationType)parameter.Data[0];
            Notification chunkNotification;
            switch (notificationType)
            {
                case BlockNotificationType.BlockChanged:
                    chunkNotification = Serializer.DeserializePoolElement(blockChangedNotificationPool, parameter.Data);
                    break;
                case BlockNotificationType.BlocksChanged:
                    chunkNotification = Serializer.DeserializePoolElement(blocksChangedNotificationPool, parameter.Data);
                    break;
                default:
                    throw new NotSupportedException($"This Type is not supported: {notificationType}");
            }

            chunkNotification.SenderId = parameter.ClientId;
            updateHub.Push(chunkNotification, DefaultChannels.Chunk);
            updateHub.Push(chunkNotification, DefaultChannels.Network);
            chunkNotification.Release();

            return null;
        }
    }
}
