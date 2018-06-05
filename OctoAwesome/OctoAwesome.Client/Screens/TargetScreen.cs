using engenious;
using engenious.Graphics;
using MonoGameUi;
using OctoAwesome.Client.Components;
using System;

namespace OctoAwesome.Client.Screens
{
    internal sealed class TargetScreen : Screen
    {
        private AssetComponent assets;

        public TargetScreen(ScreenComponent manager, Action<Coordinate> tp, Coordinate position) : base(manager)
        {
            assets = manager.Game.Assets;

            IsOverlay = true;
            Background = new BorderBrush(Color.Black * 0.5f);
            Title = Languages.OctoClient.SelectTarget;

            Texture2D panelBackground = assets.LoadTexture(typeof(ScreenComponent), "panel");
            Panel panel = new Panel(manager)
            {
                Background = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                Padding = Border.All(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Controls.Add(panel);

            StackPanel spanel = new StackPanel(manager);
            panel.Controls.Add(spanel);

            Label headLine = new Label(manager)
            {
                Text = Title,
                Font = Skin.Current.HeadlineFont,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            spanel.Controls.Add(headLine);

            StackPanel vstack = new StackPanel(manager);
            vstack.Orientation = Orientation.Vertical;
            spanel.Controls.Add(vstack);

            StackPanel pStack = new StackPanel(manager);
            pStack.Orientation = Orientation.Horizontal;
            vstack.Controls.Add(pStack);

            Label pLabel = new Label(manager);
            pLabel.Text = Languages.OctoClient.Planet + ":";
            pStack.Controls.Add(pLabel);

            Textbox pText = new Textbox(manager)
            {
                Background = new BorderBrush(Color.Gray),
                Width = 150,
                Margin = new Border(2, 10, 2, 10),
                Text = position.Planet.ToString()
            };
            pStack.Controls.Add(pText);

            StackPanel xStack = new StackPanel(manager);
            xStack.Orientation = Orientation.Horizontal;
            vstack.Controls.Add(xStack);

            Label xLabel = new Label(manager);
            xLabel.Text = "X:";
            xStack.Controls.Add(xLabel);

            Textbox xText = new Textbox(manager)
            {
                Background = new BorderBrush(Color.Gray),
                Width = 150,
                Margin = new Border(2, 10, 2, 10),
                Text = position.GlobalBlockIndex.X.ToString()
            };
            xStack.Controls.Add(xText);

            StackPanel yStack = new StackPanel(manager);
            yStack.Orientation = Orientation.Horizontal;
            vstack.Controls.Add(yStack);

            Label yLabel = new Label(manager);
            yLabel.Text = "Y:";
            yStack.Controls.Add(yLabel);

            Textbox yText = new Textbox(manager)
            {
                Background = new BorderBrush(Color.Gray),
                Width = 150,
                Margin = new Border(2, 10, 2, 10),
                Text = position.GlobalBlockIndex.Y.ToString()
            };
            yStack.Controls.Add(yText);

            Button closeButton = Button.TextButton(manager, Languages.OctoClient.Teleport);
            closeButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            closeButton.LeftMouseClick += (s, e) =>
            {
                int planet = int.Parse(pText.Text);
                int x = int.Parse(xText.Text);
                int y = int.Parse(yText.Text);

                var c = new LocalChunkCache(manager.Game.ResourceManager.GlobalChunkCache, false, 2, 1);
                c.SetCenter(planet, new Index2(x, y), (b) =>
                {
                    var gl = c.GroundLevel(x, y);
                    c.Flush();

                    var coordinate = new Coordinate(planet, new Index3(x, y, gl + 2), new Vector3());
                    if (tp != null)
                        tp(coordinate);
                    else
                        manager.NavigateBack();
                });
            };
            spanel.Controls.Add(closeButton);

            KeyDown += (s, e) =>
            {
                if (e.Key == engenious.Input.Keys.Escape)
                    manager.NavigateBack();
                e.Handled = true;
            };
        }
    }
}
