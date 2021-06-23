using OctoAwesome.Logging;
using OctoAwesome.Network;
using OctoAwesome.Network.Pooling;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using OctoAwesome.Serialization;
using OctoAwesome.Threading;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OctoAwesome.Network
{
    public class NetworkUpdateManager : IAsyncObserver<Package>, INotificationObserver
    {
        private readonly Client client;
        private readonly IUpdateHub updateHub;
        private readonly ILogger logger;
        private readonly IDisposable hubSubscription;
        private readonly IDisposable clientSubscription;
        private readonly IPool<EntityNotification> entityNotificationPool;
        private readonly IPool<FunctionalBlockNotification> functionalBlockNotificationPool;
        private readonly IPool<BlockChangedNotification> blockChangedNotificationPool;
        private readonly IPool<BlocksChangedNotification> blocksChangedNotificationPool;
        private readonly PackagePool packagePool;

        public NetworkUpdateManager(Client client, IUpdateHub updateHub)
        {
            this.client = client;
            this.updateHub = updateHub;

            logger = (TypeContainer.GetOrNull<ILogger>() ?? NullLogger.Default).As(typeof(NetworkUpdateManager));
            entityNotificationPool = TypeContainer.Get<IPool<EntityNotification>>();
            functionalBlockNotificationPool = TypeContainer.Get<IPool<FunctionalBlockNotification>>();
            blockChangedNotificationPool = TypeContainer.Get<IPool<BlockChangedNotification>>();
            blocksChangedNotificationPool = TypeContainer.Get<IPool<BlocksChangedNotification>>();
            packagePool = TypeContainer.Get<PackagePool>();

            hubSubscription = updateHub.Subscribe(this, DefaultChannels.Network);
            clientSubscription = client.Subscribe(this);
            
        }

        public Task OnNext(Package package)
        {
            switch (package.OfficialCommand)
            {
                case OfficialCommand.EntityNotification:
                    var entityNotification = Serializer.DeserializePoolElement(entityNotificationPool, package.Payload);
                    updateHub.Push(entityNotification, DefaultChannels.Simulation);
                    entityNotification.Release();
                    break;
                case OfficialCommand.FunctionalBlockNotification:
                    var functionalBlockNotification = Serializer.DeserializePoolElement(functionalBlockNotificationPool, package.Payload);
                    updateHub.Push(functionalBlockNotification, DefaultChannels.Simulation);
                    functionalBlockNotification.Release();
                    break;
                case OfficialCommand.ChunkNotification:
                    var notificationType = (BlockNotificationType)package.Payload[0];
                    Notification chunkNotification;
                    switch (notificationType)
                    {
                        case BlockNotificationType.BlockChanged:
                            chunkNotification = Serializer.DeserializePoolElement(blockChangedNotificationPool, package.Payload);
                            break;
                        case BlockNotificationType.BlocksChanged:
                            chunkNotification = Serializer.DeserializePoolElement(blocksChangedNotificationPool, package.Payload);
                            break;
                        default:
                            throw new NotSupportedException($"This Type is not supported: {notificationType}");
                    }
                    updateHub.Push(chunkNotification, DefaultChannels.Chunk);
                    chunkNotification.Release();
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        public void OnNext(Notification value)
        {
            ushort command;
            byte[] payload;
            switch (value)
            {
                case EntityNotification entityNotification:
                    command = (ushort)OfficialCommand.EntityNotification;
                    payload = Serializer.Serialize(entityNotification);
                    break;
                case FunctionalBlockNotification functionalBlockNotification:
                    command = (ushort)OfficialCommand.FunctionalBlockNotification;
                    payload = Serializer.Serialize(functionalBlockNotification);
                    break;
                case BlockChangedNotification chunkNotification:
                    command = (ushort)OfficialCommand.ChunkNotification;
                    payload = Serializer.Serialize(chunkNotification);
                    break;
                default:
                    return;
            }
            var package = packagePool.Get();
            package.Command = command;
            package.Payload = payload;
            client.SendPackageAndRelase(package);
        }

        public Task OnError(Exception error)
        {
            logger.Error(error.Message, error);
            return Task.CompletedTask;
        }

        public Task OnCompleted()
        {
            clientSubscription.Dispose();
            return Task.CompletedTask;
        }

        void INotificationObserver.OnCompleted()
        {
            //hubSubscription.Dispose();
        }

        void INotificationObserver.OnError(Exception error)
        {
            logger.Error(error.Message, error);
        }
    }
}