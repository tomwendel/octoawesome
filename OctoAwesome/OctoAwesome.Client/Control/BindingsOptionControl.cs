using engenious;
using engenious.Input;
using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.Client.Components;
using OctoAwesome.Client.Screens;
using OctoAwesome.UI.Components;
using System.Linq;

namespace OctoAwesome.Client.Controls
{
    internal sealed class BindingsOptionControl : Panel
    {
        private readonly AssetComponent assets;
        private readonly ISettings settings;
        private readonly KeyMapper keyMapper;

        public BindingsOptionControl(BaseScreenComponent manager, AssetComponent assets, KeyMapper keyMapper, ISettings settings) : base(manager)
        {
            this.assets = assets;
            this.settings = settings;
            this.keyMapper = keyMapper;
            ScrollContainer bindingsScroll = new ScrollContainer(manager);
            Controls.Add(bindingsScroll);

            StackPanel bindingsStack = new StackPanel(manager)
            {
                Orientation = Orientation.Vertical,
                Padding = new Border(20, 20, 20, 20),
                Width = 650
            };
            bindingsScroll.Content = bindingsStack;

            //////////////////////////////KeyBindings////////////////////////////////////////////
            var bindings = keyMapper.GetBindings();
            foreach (var binding in bindings)
            {
                StackPanel bindingStack = new StackPanel(manager)
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 35
                };

                Label lbl = new Label(manager)
                {
                    Text = binding.DisplayName,
                    Width = 480
                };

                Label bindingKeyLabel = new Label(manager)
                {
                    Text = binding.Keys.First().ToString(),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 90,
                    Background = new BorderBrush(Color.LightGray, LineType.Solid, Color.Gray),
                    Tag = new object[] { binding.Id, binding.Keys.First() }
                };
                bindingKeyLabel.LeftMouseClick += BindingKeyLabel_LeftMouseClick;

                bindingStack.Controls.Add(lbl);
                bindingStack.Controls.Add(bindingKeyLabel);
                bindingsStack.Controls.Add(bindingStack);
            }
        }

        private void BindingKeyLabel_LeftMouseClick(Control sender, MouseEventArgs args)
        {
            object[] data = (object[])sender.Tag;

            if (data is null)
                return;

            string id = (string)data[0];
            Keys oldKey = (Keys)data[1];

            Label lbl = (Label)sender;

            MessageScreen screen = new MessageScreen(ScreenManager, assets, UI.Languages.OctoClient.PressKey, "", UI.Languages.OctoClient.Cancel);
            screen.KeyDown += (s, a) =>
            {
                keyMapper.RemoveKey(id, oldKey);
                keyMapper.AddKey(id, a.Key);
                data[1] = a.Key;
                settings.Set("KeyMapper-" + id, a.Key.ToString());
                lbl.Text = a.Key.ToString();
                ScreenManager.NavigateBack();
            };
            ScreenManager.NavigateToScreen(screen);
        }
    }
}
