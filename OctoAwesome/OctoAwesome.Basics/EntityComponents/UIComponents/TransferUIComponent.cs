using engenious.UI;

using OctoAwesome.Basics.UI.Screens;
using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.EntityComponents.UIComponents
{
    public class TransferUIComponent : UIComponent
    {
        public event EventHandler<NavigationEventArgs> Closed;

        private readonly TransferScreen transferScreen;
        private readonly InventoryComponent chestInventory;

        public TransferUIComponent(InventoryComponent chestInventory)
        {
            this.chestInventory = chestInventory;
            transferScreen = new TransferScreen(ScreenComponent, AssetComponent, chestInventory, new InventoryComponent());
            transferScreen.Closed += TransferScreen_Closed;
            Serializeable = false;
        }

        private void TransferScreen_Closed(object sender, engenious.UI.NavigationEventArgs e)
        {
            Closed?.Invoke(sender, e);
        }

        public void Show(Player p)
        {
            var playerInventory = p.Components.GetComponent<InventoryComponent>();
            if (playerInventory is null) 
                return;
            transferScreen.Rebuild(chestInventory, playerInventory);

            if (ScreenComponent.ActiveScreen != transferScreen)
                ScreenComponent.NavigateToScreen(transferScreen);
        }
    }
}
