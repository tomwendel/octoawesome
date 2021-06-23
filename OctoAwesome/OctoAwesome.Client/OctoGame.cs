using OctoAwesome;
using OctoAwesome.Client.Components;
using OctoAwesome.Client.Controls;
using OctoAwesome.Runtime;
using System;
using System.Configuration;
using System.Linq;
using engenious.UI;
using EventArgs = System.EventArgs;
using engenious;
using engenious.Input;
using System.Collections.Generic;
using OctoAwesome.Notifications;
using OctoAwesome.Common;
using OctoAwesome.Definitions;
using OctoAwesome.UI.Components;

namespace OctoAwesome.Client
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    internal class OctoGame : Game
    {
        private readonly ITypeContainer typeContainer;

        //GraphicsDeviceManager graphics;

        public CameraComponent Camera { get; private set; }

        public PlayerComponent Player { get; private set; }

        public SimulationComponent Simulation { get; private set; }

        public GameService Service { get; private set; }

        public ScreenComponent Screen { get; private set; }

        public KeyMapper KeyMapper { get; private set; }

        public AssetComponent Assets { get; private set; }

        public Settings Settings { get; private set; }

        public IDefinitionManager DefinitionManager { get; private set; }

        public IResourceManager ResourceManager { get; private set; }

        public ExtensionLoader ExtensionLoader { get; private set; }

        public EntityGameComponent Entity { get; private set; }

        public OctoGame() : base()
        {
            //graphics = new GraphicsDeviceManager(this);
            //graphics.PreferredBackBufferWidth = 1080;
            //graphics.PreferredBackBufferHeight = 720;

            //Content.RootDirectory = "Content";

            Title = "OctoAwesome";
            IsMouseVisible = true;
            //Icon = Properties.Resources.octoawesome;

            typeContainer = TypeContainer.Get<ITypeContainer>();
            Screen = new ScreenComponent(this);

            typeContainer.Register<BaseScreenComponent>(Screen);
            typeContainer.Register<ScreenComponent>(Screen);

            //Window.AllowUserResizing = true;
            Register(typeContainer);

            Settings = typeContainer.Get<Settings>();

            KeyMapper = new KeyMapper(Screen, Settings);
            Assets = new AssetComponent(this, Settings);

            typeContainer.Register(Assets);

            Screen.UpdateOrder = 1;
            Screen.DrawOrder = 1;

            ExtensionLoader = typeContainer.Get<ExtensionLoader>();
            ExtensionLoader.LoadExtensions();

            Service = typeContainer.Get<GameService>();
            //TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 15);

            int width = Settings.Get("Width", 1680);
            int height = Settings.Get("Height", 1050);
            Window.ClientSize = new Size(width, height);

            Window.Fullscreen = Settings.Get("EnableFullscreen", false);

            if (Settings.KeyExists("Viewrange"))
            {
                var viewrange = Settings.Get<int>("Viewrange");

                if (viewrange < 1)
                    throw new NotSupportedException("Viewrange in app.config darf nicht kleiner 1 sein");

                SceneControl.VIEWRANGE = viewrange;
            }



           
            Components.Add(Assets);
            Components.Add(Screen);


            #region GameComponents
            DefinitionManager = typeContainer.Get<DefinitionManager>();

            //var persistenceManager = new DiskPersistenceManager(ExtensionLoader, DefinitionManager, Settings);
            //ResourceManager = new ResourceManager(ExtensionLoader, DefinitionManager, Settings, persistenceManager);
            ResourceManager = typeContainer.Get<ContainerResourceManager>();


            Player = new PlayerComponent(this, ResourceManager);
            Player.UpdateOrder = 2;
            Components.Add(Player);

            Simulation = new Components.SimulationComponent(this,
              ExtensionLoader, ResourceManager);

            Entity = new Components.EntityGameComponent(this, Simulation);
            Entity.UpdateOrder = 2;
            Components.Add(Entity);

            Simulation.UpdateOrder = 3;
            Components.Add(Simulation);

            Camera = new CameraComponent(this);
            Camera.UpdateOrder = 4;
            Components.Add(Camera);

            #endregion GameComponents

            /*Resize += (s, e) =>
            {
                //if (Window.ClientBounds.Height == graphics.PreferredBackBufferHeight &&
                //   Window.ClientBounds.Width == graphics.PreferredBackBufferWidth)
                //    return;

                //graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                //graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                //graphics.ApplyChanges();
            };*/
            SetKeyBindings();

        }

        private static void Register(ITypeContainer typeContainer)
        {
            typeContainer.Register<Settings>(InstanceBehaviour.Singleton);
            typeContainer.Register<ISettings, Settings>(InstanceBehaviour.Singleton);
            typeContainer.Register<ExtensionLoader>(InstanceBehaviour.Singleton);
            typeContainer.Register<IExtensionLoader, ExtensionLoader>(InstanceBehaviour.Singleton);
            typeContainer.Register<IExtensionResolver, ExtensionLoader>(InstanceBehaviour.Singleton);
            typeContainer.Register<DefinitionManager>(InstanceBehaviour.Singleton);
            typeContainer.Register<IDefinitionManager, DefinitionManager>(InstanceBehaviour.Singleton);
            typeContainer.Register<ContainerResourceManager>(InstanceBehaviour.Singleton);
            typeContainer.Register<IResourceManager, ContainerResourceManager>(InstanceBehaviour.Singleton);
            typeContainer.Register<GameService>(InstanceBehaviour.Singleton);
            typeContainer.Register<IGameService, GameService>(InstanceBehaviour.Singleton);
            typeContainer.Register<UpdateHub>(InstanceBehaviour.Singleton);
            typeContainer.Register<IUpdateHub, UpdateHub>(InstanceBehaviour.Singleton);
        }

        private void SetKeyBindings()
        {
            KeyMapper.RegisterBinding("octoawesome:forward", UI.Languages.OctoKeys.forward);
            KeyMapper.RegisterBinding("octoawesome:left", UI.Languages.OctoKeys.left);
            KeyMapper.RegisterBinding("octoawesome:backward", UI.Languages.OctoKeys.backward);
            KeyMapper.RegisterBinding("octoawesome:right", UI.Languages.OctoKeys.right);
            KeyMapper.RegisterBinding("octoawesome:headup", UI.Languages.OctoKeys.headup);
            KeyMapper.RegisterBinding("octoawesome:headdown", UI.Languages.OctoKeys.headdown);
            KeyMapper.RegisterBinding("octoawesome:headleft", UI.Languages.OctoKeys.headleft);
            KeyMapper.RegisterBinding("octoawesome:headright", UI.Languages.OctoKeys.headright);
            KeyMapper.RegisterBinding("octoawesome:interact", UI.Languages.OctoKeys.interact);
            KeyMapper.RegisterBinding("octoawesome:apply", UI.Languages.OctoKeys.apply);
            KeyMapper.RegisterBinding("octoawesome:flymode", UI.Languages.OctoKeys.flymode);
            KeyMapper.RegisterBinding("octoawesome:jump", UI.Languages.OctoKeys.jump);
            for (int i = 0; i < 10; i++)
                KeyMapper.RegisterBinding("octoawesome:slot" + i, UI.Languages.OctoKeys.ResourceManager.GetString("slot" + i));
            KeyMapper.RegisterBinding("octoawesome:debug.allblocks", UI.Languages.OctoKeys.debug_allblocks);
            KeyMapper.RegisterBinding("octoawesome:debug.control", UI.Languages.OctoKeys.debug_control);
            KeyMapper.RegisterBinding("octoawesome:inventory", UI.Languages.OctoKeys.inventory);
            KeyMapper.RegisterBinding("octoawesome:hidecontrols", UI.Languages.OctoKeys.hidecontrols);
            KeyMapper.RegisterBinding("octoawesome:exit", UI.Languages.OctoKeys.exit);
            KeyMapper.RegisterBinding("octoawesome:freemouse", UI.Languages.OctoKeys.freemouse);
            KeyMapper.RegisterBinding("octoawesome:fullscreen", UI.Languages.OctoKeys.fullscreen);
            KeyMapper.RegisterBinding("octoawesome:teleport", UI.Languages.OctoKeys.teleport);
            KeyMapper.RegisterBinding("octoawesome:toggleAmbientOcclusion", UI.Languages.OctoKeys.toggleAmbientOcclusion);
            KeyMapper.RegisterBinding("octoawesome:toggleWireFrame", UI.Languages.OctoKeys.toggleWireFrame);

            Dictionary<string, Keys> standardKeys = new Dictionary<string, Keys>()
            {
                { "octoawesome:forward", Keys.W },
                { "octoawesome:left", Keys.A },
                { "octoawesome:backward", Keys.S },
                { "octoawesome:right", Keys.D },
                { "octoawesome:headup", Keys.Up },
                { "octoawesome:headdown", Keys.Down },
                { "octoawesome:headleft", Keys.Left },
                { "octoawesome:headright", Keys.Right },
                { "octoawesome:interact", Keys.E },
                { "octoawesome:apply", Keys.Q },
                { "octoawesome:flymode", Keys.ScrollLock },
                { "octoawesome:jump", Keys.Space },
                { "octoawesome:slot0", Keys.D1 },
                { "octoawesome:slot1", Keys.D2 },
                { "octoawesome:slot2", Keys.D3 },
                { "octoawesome:slot3", Keys.D4 },
                { "octoawesome:slot4", Keys.D5 },
                { "octoawesome:slot5", Keys.D6 },
                { "octoawesome:slot6", Keys.D7 },
                { "octoawesome:slot7", Keys.D8 },
                { "octoawesome:slot8", Keys.D9 },
                { "octoawesome:slot9", Keys.D0 },
                { "octoawesome:debug.allblocks", Keys.L },
                { "octoawesome:debug.control", Keys.F10 },
                { "octoawesome:inventory", Keys.I },
                { "octoawesome:hidecontrols", Keys.F9 },
                { "octoawesome:exit", Keys.Escape },
                { "octoawesome:freemouse", Keys.F12 },
                { "octoawesome:fullscreen", Keys.F11 },
                { "octoawesome:teleport", Keys.T },
                { "octoawesome:toggleAmbientOcclusion", Keys.O },
                { "octoawesome:toggleWireFrame", Keys.J },
                { "octoawesome:freezeLight", Keys.L }
            };

            KeyMapper.LoadFromConfig(standardKeys);

            KeyMapper.AddAction("octoawesome:fullscreen", type =>
            {
                if (type == KeyMapper.KeyType.Down)
                {
                    Window.Fullscreen = !Window.Fullscreen;
                }
            });
        }

        protected override void OnExiting(EventArgs args)
        {
            Player.SetEntity(null);
            Simulation.ExitGame();
        }
    }
}
