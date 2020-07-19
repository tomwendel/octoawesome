using engenious;
using engenious.UI;
using engenious.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Client.Controls
{
    public class ChatControl : ContainerControl
    {
        private ScrollContainer textScroll;
        private StackPanel textStack;
        private Textbox textBox;
        private readonly BaseScreenComponent manager;

        public ChatControl(BaseScreenComponent manager, string style = "") : base(manager, style)
        {
            textBox = new Textbox(manager)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new BorderBrush(Color.Black * 0.7f),
                TextColor = Color.White
            };

            textBox.KeyDown += TextBox_KeyDown;

            textStack = new StackPanel(manager)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            textScroll = new ScrollContainer(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            textScroll.Content = textStack;

            var mainGrid = new Grid(manager)
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new BorderBrush(Color.Gray * 0.7f)
            };

            mainGrid.Columns.Add(new ColumnDefinition() { ResizeMode = ResizeMode.Parts, Width = 1 });
            mainGrid.Rows.Add(new RowDefinition() { ResizeMode = ResizeMode.FitParts, Height = 1 });
            mainGrid.Rows.Add(new RowDefinition() { ResizeMode = ResizeMode.Auto });

            mainGrid.AddControl(textScroll, 0, 0);
            mainGrid.AddControl(textBox, 0, 1);
            Controls.Add(mainGrid);
            this.manager = manager;
        }

        bool shouldScrollDown = false;
        public void AddTextMessage(string message)
        {
            var label = new Label(manager) { 
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextColor = Color.White
            };
            textStack.Controls.Add(label);
            shouldScrollDown = true;
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            base.OnUpdate(gameTime);
            if(shouldScrollDown)
            {
                shouldScrollDown = false;
                textScroll.VerticalScrollPosition = int.MaxValue;
            }
        }

        private void TextBox_KeyDown(Control sender, KeyEventArgs args)
        {
            if(args.Key == engenious.Input.Keys.Enter && Visible && textBox.Text != "")
            {
                OnMessage?.Invoke(textBox.Text);
                textBox.Text = "";
            }
        }

        protected override void OnVisibleChanged(PropertyEventArgs<bool> args)
        {
            if (args.NewValue)
            {
                manager.FreeMouse();
                textBox.Focus();
            }
            else
            {
                manager.CaptureMouse();
                textBox.Unfocus();
            }
            base.OnVisibleChanged(args);
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            if (Visible)
            {
                args.Handled = true;
                if (args.Key == engenious.Input.Keys.Escape)
                {
                    Visible = false;
                }
            }
            base.OnKeyDown(args);
        }

        public event MessageEventArgs OnMessage;

        public delegate void MessageEventArgs(string message);

    }

}
