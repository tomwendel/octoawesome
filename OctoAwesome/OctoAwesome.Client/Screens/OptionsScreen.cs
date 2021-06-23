using engenious.UI;
using OctoAwesome.Client.Components;
using OctoAwesome.Client.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using engenious.Graphics;
using engenious;
using engenious.Input;
using engenious.UI.Controls;
using OctoAwesome.UI.Components;

namespace OctoAwesome.Client.Screens
{
    internal sealed class OptionsScreen : BaseScreen
    {
        private readonly AssetComponent assets;
        private readonly ISettings settings;
        private readonly Button exitButton;

        public OptionsScreen(ScreenComponent manager) : base(manager)
        {
            assets = manager.Game.Assets;
            settings = manager.Game.Settings;
            Padding = new Border(0, 0, 0, 0);

            Title = UI.Languages.OctoClient.Options;

            Texture2D panelBackground = assets.LoadTexture("panel");

            SetDefaultBackground();

            TabControl tabs = new TabControl(manager)
            {
                Padding = new Border(20, 20, 20, 20),
                Width = 700,
                TabPageBackground = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                TabBrush = NineTileBrush.FromSingleTexture(assets.LoadTexture( "buttonLong_brown"), 15, 15),
                TabActiveBrush = NineTileBrush.FromSingleTexture(assets.LoadTexture( "buttonLong_beige"), 15, 15),
            };
            Controls.Add(tabs);

            #region OptionsPage

            TabPage optionsPage = new TabPage(manager, UI.Languages.OctoClient.Options);
            tabs.Pages.Add(optionsPage);

            OptionsOptionControl optionsOptions = new OptionsOptionControl(manager, this, settings, assets)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            optionsPage.Controls.Add(optionsOptions);

            #endregion

            #region BindingsPage

            TabPage bindingsPage = new TabPage(manager, UI.Languages.OctoClient.KeyBindings);
            bindingsPage.Padding = Border.All(10);
            tabs.Pages.Add(bindingsPage);

            BindingsOptionControl bindingsOptions = new BindingsOptionControl(manager, assets, manager.Game.KeyMapper, settings)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            bindingsPage.Controls.Add(bindingsOptions);

            #endregion

            #region TexturePackPage

            TabPage resourcePackPage = new TabPage(manager, "Resource Packs");
            tabs.Pages.Add(resourcePackPage);

            ResourcePacksOptionControl resourcePacksOptions = new ResourcePacksOptionControl(manager, assets)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            resourcePackPage.Controls.Add(resourcePacksOptions);

            #endregion

            #region ExtensionPage

            TabPage extensionPage = new TabPage(manager, UI.Languages.OctoClient.Extensions);
            tabs.Pages.Add(extensionPage);

            ExtensionsOptionControl extensionOptions = new ExtensionsOptionControl(manager, manager.Game.ExtensionLoader)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            extensionPage.Controls.Add(extensionOptions);

            #endregion

            ////////////////////////////////////////////Restart Button////////////////////////////////////////////
            exitButton = new TextButton(manager, UI.Languages.OctoClient.RestartGameToApplyChanges);
            exitButton.VerticalAlignment = VerticalAlignment.Top;
            exitButton.HorizontalAlignment = HorizontalAlignment.Right;
            exitButton.Enabled = false;
            exitButton.Visible = false;
            exitButton.LeftMouseClick += (s, e) => Program.Restart();
            exitButton.Margin = new Border(10, 10, 10, 10);
            Controls.Add(exitButton);
        }

        public void NeedRestart()
        {
            exitButton.Visible = true;
            exitButton.Enabled = true;
        }        
    }
}
