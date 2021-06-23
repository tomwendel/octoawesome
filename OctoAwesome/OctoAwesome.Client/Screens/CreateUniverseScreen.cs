using engenious;
using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.Client.Components;
using System;

namespace OctoAwesome.Client.Screens
{
    class CreateUniverseScreen : BaseScreen
    {
        new readonly ScreenComponent Manager;
        private readonly Textbox nameInput;
        private readonly Textbox seedInput;
        readonly Button createButton;

        private readonly ISettings settings;

        public CreateUniverseScreen(ScreenComponent manager) : base(manager)
        {
            Manager = manager;
            settings = manager.Game.Settings;

            Padding = new Border(0, 0, 0, 0);

            Title = UI.Languages.OctoClient.CreateUniverse;

            SetDefaultBackground();

            var panel = new Panel(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = Border.All(50),
                Background = new BorderBrush(Color.White * 0.5f),
                Padding = Border.All(10)
            };
            Controls.Add(panel);

            var grid = new Grid(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            panel.Controls.Add(grid);

            grid.Columns.Add(new ColumnDefinition() { ResizeMode = ResizeMode.Auto });
            grid.Columns.Add(new ColumnDefinition() { Width = 1, ResizeMode = ResizeMode.Parts });

            nameInput = GetTextbox();
            nameInput.TextChanged += (s, e) =>
            {
                createButton.Visible = !string.IsNullOrEmpty(e.NewValue);
            };
            AddLabeledControl(grid, string.Format("{0}: ", UI.Languages.OctoClient.Name), nameInput);

            seedInput = GetTextbox();
            AddLabeledControl(grid, string.Format("{0}: ", UI.Languages.OctoClient.Seed), seedInput);

            createButton = new TextButton(manager, UI.Languages.OctoClient.Create);
            createButton.HorizontalAlignment = HorizontalAlignment.Right;
            createButton.VerticalAlignment = VerticalAlignment.Bottom;
            createButton.Visible = false;
            createButton.LeftMouseClick += (s, e) =>
            {
                if (string.IsNullOrEmpty(nameInput.Text))
                    return;
                
                manager.Player.SetEntity(null);

                Guid guid = Manager.Game.Simulation.NewGame(nameInput.Text, seedInput.Text);
                settings.Set("LastUniverse", guid.ToString());

                Player player = manager.Game.Simulation.LoginPlayer("");
                manager.Game.Player.SetEntity(player);

                manager.NavigateToScreen(new LoadingScreen(manager));
            };
            panel.Controls.Add(createButton);

        }

        private Textbox GetTextbox()
        {
            var t = new Textbox(Manager)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new BorderBrush(Color.LightGray, LineType.Solid, Color.Black)
            };
            return t;
        }
    }
}
