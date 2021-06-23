using OctoAwesome.Network.Pooling;
using OctoAwesome.Network.ServerNotifications;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using OctoAwesome.Serialization;
using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OctoAwesome.Network
{
    public sealed class ConnectedClient : BaseClient, INotificationObserver
    {
        public IDisposable NetworkChannelSubscription { get; set; }
        public IDisposable ServerSubscription { get; set; }

        private readonly PackagePool packagePool;

        public ConnectedClient(Socket socket) : base(socket)
        {
            packagePool = TypeContainer.Get<PackagePool>();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            Socket.Close();
            throw error;
        }

        public void OnNext(Notification value)
        {
            if (value.SenderId == Id)
                return;

            OfficialCommand command;
            byte[] payload;
            switch (value)
            {
                case EntityNotification entityNotification:
                    command = OfficialCommand.EntityNotification;
                    payload = Serializer.Serialize(entityNotification);
                    break;
                case FunctionalBlockNotification functionalBlockNotification:
                    command = OfficialCommand.FunctionalBlockNotification;
                    payload = Serializer.Serialize(functionalBlockNotification);
                    break;

                case BlocksChangedNotification _:
                case BlockChangedNotification _:
                    command = OfficialCommand.ChunkNotification;
                    payload = Serializer.Serialize(value as SerializableNotification);
                    break;
                default:
                    return;
            }

            BuildAndSendPackage(payload, command);
        }

        private void BuildAndSendPackage(byte[] data, OfficialCommand officialCommand)
        {
            var package = packagePool.Get();
            package.Payload = data;
            package.Command = (ushort)officialCommand;
            SendPackageAndRelase(package);
        }
    }
}