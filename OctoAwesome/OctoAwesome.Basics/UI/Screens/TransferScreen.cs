using engenious;
using engenious.Graphics;
using engenious.Input;
using engenious.UI;
using engenious.UI.Controls;
using OctoAwesome.EntityComponents;
using OctoAwesome.UI.Components;
using OctoAwesome.UI.Controls;
using System;
using System.Collections.Generic;

namespace OctoAwesome.Basics.UI.Screens
{
    public class TransferScreen : Screen
    {
        public event EventHandler<NavigationEventArgs> Closed;

        private readonly AssetComponent assetComponent;
        private readonly Texture2D panelBackground;
        private readonly InventoryControl inventoryA;
        private readonly InventoryControl inventoryB;
        private readonly Label nameLabel;
        private readonly Label massLabel;
        private readonly Label volumeLabel;

        private InventoryComponent componentA;
        private InventoryComponent componentB;

        public TransferScreen(BaseScreenComponent manager, AssetComponent assetComponent, InventoryComponent inventoryComponentA, InventoryComponent inventoryComponentB) : base(manager)
        {
            Background = new BorderBrush(Color.Black * 0.3f);
            IsOverlay = true;
            this.assetComponent = assetComponent;
            componentA = inventoryComponentA;
            componentB = inventoryComponentB;

            panelBackground = this.assetComponent.LoadTexture("panel");
            var grid = new Grid(manager)
            {
                Width = 800,
                Height = 500,
            };

            grid.Columns.Add(new ColumnDefinition() { ResizeMode = ResizeMode.Fixed, Width = 600 });
            grid.Columns.Add(new ColumnDefinition() { ResizeMode = ResizeMode.Fixed, Width = 200 });
            grid.Rows.Add(new RowDefinition() { ResizeMode = ResizeMode.Parts, Height = 100 });
            grid.Rows.Add(new RowDefinition() { ResizeMode = ResizeMode.Parts, Height = 5, });
            grid.Rows.Add(new RowDefinition() { ResizeMode = ResizeMode.Parts, Height = 100 });

            Controls.Add(grid);

            inventoryA = new InventoryControl(manager, assetComponent, componentA.Inventory)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                Padding = Border.All(20),
            };

            inventoryA.EndDrop += (s, e) => OnInventoryDrop(inventoryB, componentB, inventoryA, componentA, e);

            inventoryB = new InventoryControl(manager, assetComponent, componentB.Inventory)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                Padding = Border.All(20),
            };

            inventoryB.EndDrop += (s, e) => OnInventoryDrop(inventoryA, componentA, inventoryB, componentB, e);

            grid.AddControl(inventoryA, 0, 0);
            grid.AddControl(inventoryB, 0, 2);

            var infoPanel = new StackPanel(manager)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = NineTileBrush.FromSingleTexture(panelBackground, 30, 30),
                Padding = Border.All(20),
                Margin = Border.All(10, 0, 0, 0),

            };

            nameLabel = new Label(manager);
            infoPanel.Controls.Add(nameLabel);
            massLabel = new Label(manager);
            infoPanel.Controls.Add(massLabel);
            volumeLabel = new Label(manager);
            infoPanel.Controls.Add(volumeLabel);
            grid.AddControl(infoPanel, 1, 0, 1, 3);
        }

        private void OnInventoryDrop(InventoryControl sourceControl, InventoryComponent source, InventoryControl targetControl, InventoryComponent target, DragEventArgs e)
        {
            if (e.Content is InventorySlot slot)
            {
                e.Handled = true;
                if (source.RemoveSlot(slot))
                    target.AddSlot(slot);

                sourceControl.Rebuild(source.Inventory);
                targetControl.Rebuild(target.Inventory);
            }

        }

        internal void Rebuild(InventoryComponent inventoryComponentA, InventoryComponent inventoryComponentB)
        {
            componentA = inventoryComponentA;
            componentB = inventoryComponentB;

            inventoryA.Rebuild(componentA.Inventory);
            inventoryB.Rebuild(componentB.Inventory);
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {

            if (Manager.CanGoBack && (args.Key == Keys.Escape || args.Key == Keys.I))
            {
                args.Handled = true;
                Manager.NavigateBack();
            }

            base.OnKeyDown(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            base.OnNavigatedFrom(args);
            Closed?.Invoke(this, args);
        }
    }
}
