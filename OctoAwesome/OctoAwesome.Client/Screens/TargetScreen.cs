using engenious;
using engenious.Graphics;
using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.Client.Components;
using OctoAwesome.UI.Components;
using System;

namespace OctoAwesome.Client.Screens
{
    internal sealed class TargetScreen : Screen
    {
        private AssetComponent assets;

        public TargetScreen(ScreenComponent manager, Action<int, int> tp, int x, int y) : base(manager)
        {
            assets = manager.Game.Assets;

            IsOverlay = true;
            Background = new BorderBrush(Color.Black * 0.5f);
            Title = UI.Languages.OctoClient.SelectTarget;

            Texture2D panelBackground = assets.LoadTexture( "panel");
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
                Text = x.ToString()
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
                Text = y.ToString()
            };
            yStack.Controls.Add(yText);

            Button closeButton = new TextButton(manager, UI.Languages.OctoClient.Teleport);
            closeButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            closeButton.LeftMouseClick += (s, e) =>
            {
                if (tp != null)
                    tp(int.Parse(xText.Text), int.Parse(yText.Text));
                else
                    manager.NavigateBack();
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
