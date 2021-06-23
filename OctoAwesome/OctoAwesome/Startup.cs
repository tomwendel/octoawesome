using NLog;
using NLog.Config;
using NLog.Targets;
using OctoAwesome.Information;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;
using OctoAwesome.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome
{
    public static class Startup
    {
        public static void Register(ITypeContainer typeContainer)
        {
            typeContainer.Register<GlobalChunkCache, GlobalChunkCache>(InstanceBehaviour.Instance);
            typeContainer.Register<IGlobalChunkCache, GlobalChunkCache>(InstanceBehaviour.Instance);

            typeContainer.Register<Logging.NullLogger, Logging.NullLogger>();
            typeContainer.Register<Logging.Logger, Logging.Logger>();
            typeContainer.Register<Logging.ILogger, Logging.Logger>();

            typeContainer.Register<IPool<Awaiter>, Pool<Awaiter>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<Awaiter>, Pool<Awaiter>>(InstanceBehaviour.Singleton);

            typeContainer.Register<IPool<BlockChangedNotification>, Pool<BlockChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<BlockChangedNotification>, Pool<BlockChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<BlocksChangedNotification>, Pool<BlocksChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<BlocksChangedNotification>, Pool<BlocksChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<EntityNotification>, Pool<EntityNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<EntityNotification>, Pool<EntityNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<FunctionalBlockNotification>, Pool<FunctionalBlockNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<FunctionalBlockNotification>, Pool<FunctionalBlockNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<PropertyChangedNotification>, Pool<PropertyChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<Pool<PropertyChangedNotification>, Pool<PropertyChangedNotification>>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<Chunk>, ChunkPool>(InstanceBehaviour.Singleton);
            typeContainer.Register<ChunkPool, ChunkPool>(InstanceBehaviour.Singleton);
            typeContainer.Register<IPool<BlockVolumeState>, Pool<BlockVolumeState>>(InstanceBehaviour.Singleton);
            typeContainer.Register<BlockCollectionService>(InstanceBehaviour.Singleton);
        }

        public static void ConfigureLogger(ClientType clientType)
        {
            var config = new LoggingConfiguration();

            switch (clientType)
            {
                case ClientType.DesktopClient:
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, new FileTarget("octoawesome.logfile")
                    {
                        FileName = $"./logs/octoClient-{DateTime.Now:ddMMyy_hhmmss}.log"
                    });
                    break;
                case ClientType.GameServer:
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, new ColoredConsoleTarget("octoawesome.logconsole"));
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, new FileTarget("octoawesome.logfile")
                    {
                        FileName = $"./logs/server-{DateTime.Now:ddMMyy_hhmmss}.log"
                    });
                    break;
                default:
                    config.AddRule(LogLevel.Trace, LogLevel.Fatal, new FileTarget("octoawesome.logfile")
                    {
                        FileName = $"./logs/generic-{DateTime.Now:ddMMyy_hhmmss}.log"
                    });
                    break;
            }            

            LogManager.Configuration = config;
        }
    }
}
