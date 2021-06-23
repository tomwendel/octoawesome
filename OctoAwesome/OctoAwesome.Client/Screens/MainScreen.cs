using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.Client.Components;
using OctoAwesome.Runtime;
using OctoAwesome.UI.Components;
using System.Diagnostics;

namespace OctoAwesome.Client.Screens
{
    internal sealed class MainScreen : BaseScreen
    {
        private AssetComponent assets;

        public MainScreen(ScreenComponent manager) : base(manager)
        {
            assets = manager.Game.Assets;

            Padding = new Border(0,0,0,0);

            Background = new TextureBrush(assets.LoadTexture( "background"), TextureBrushMode.Stretch);

            StackPanel stack = new StackPanel(manager);
            Controls.Add(stack);

            Button startButton = new TextButton(manager, UI.Languages.OctoClient.Start);
            startButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            startButton.Margin = new Border(0, 0, 0, 10);
            startButton.LeftMouseClick += (s, e) =>
            {
                ((ContainerResourceManager)manager.Game.ResourceManager).CreateManager(false);
                manager.NavigateToScreen(new LoadScreen(manager));
            };
            stack.Controls.Add(startButton);

            Button multiplayerButton = new TextButton(manager, UI.Languages.OctoClient.Multiplayer);
            multiplayerButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            multiplayerButton.Margin = new Border(0, 0, 0, 10);
            multiplayerButton.LeftMouseClick += (s, e) =>
            {
                manager.NavigateToScreen(new ConnectionScreen(manager));
            };
            stack.Controls.Add(multiplayerButton);

            Button optionButton = new TextButton(manager, UI.Languages.OctoClient.Options);
            optionButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            optionButton.Margin = new Border(0, 0, 0, 10);
            optionButton.MinWidth = 300;
            optionButton.LeftMouseClick += (s, e) =>
            {
                manager.NavigateToScreen(new OptionsScreen(manager));
            };
            stack.Controls.Add(optionButton);

            Button creditsButton = new TextButton(manager, UI.Languages.OctoClient.CreditsCrew);
            creditsButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            creditsButton.Margin = new Border(0, 0, 0, 10);
            creditsButton.LeftMouseClick += (s, e) =>
            {
                manager.NavigateToScreen(new CreditsScreen(manager));
            };
            stack.Controls.Add(creditsButton);

            Button webButton = new TextButton(manager, "Octoawesome.net");
            webButton.VerticalAlignment = VerticalAlignment.Bottom;
            webButton.HorizontalAlignment = HorizontalAlignment.Right;
            webButton.Margin = new Border(10, 10, 10, 10);
            webButton.LeftMouseClick += (s, e) =>
            {
                Process.Start("http://octoawesome.net/");
            };
            Controls.Add(webButton);

            Button exitButton = new TextButton(manager, UI.Languages.OctoClient.Exit);
            exitButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            exitButton.Margin = new Border(0, 0, 0, 10);
            exitButton.LeftMouseClick += (s, e) => { manager.Exit(); };
            stack.Controls.Add(exitButton);
        }
    }
}
