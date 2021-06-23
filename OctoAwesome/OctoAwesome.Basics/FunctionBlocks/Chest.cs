using engenious;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Basics.EntityComponents.UIComponents;
using OctoAwesome.EntityComponents;
using OctoAwesome.Notifications;
using OctoAwesome.Pooling;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.FunctionBlocks
{
    public class Chest : FunctionalBlock
    {
        private readonly InventoryComponent inventoryComponent;
        private readonly AnimationComponent animationComponent;
        private readonly TransferUIComponent transferUiComponent;
        private readonly IPool<FunctionalBlockNotification> functionalBlockNotificationPool;

        public Chest()
        {
            functionalBlockNotificationPool = TypeContainer.Get<IPool<FunctionalBlockNotification>>();

        }

        public Chest(Coordinate position) :this()
        {
            inventoryComponent = new InventoryComponent();
            animationComponent = new AnimationComponent();
            transferUiComponent = new TransferUIComponent(inventoryComponent);
            transferUiComponent.Closed += TransferUiComponentClosed;
            Components.AddComponent(inventoryComponent);
            Components.AddComponent(new PositionComponent()
            {
                Position = position
            });

            Components.AddComponent(new BodyComponent() { Height = 0.4f, Radius = 0.2f });
            Components.AddComponent(new BoxCollisionComponent(new[] { new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1)) }));
            Components.AddComponent(new RenderComponent() { Name = "Chest", ModelName = "chest", TextureName = "texchestmodel", BaseZRotation = -90 }, true);
            Components.AddComponent(transferUiComponent, true);
            Components.AddComponent(animationComponent);

            //Simulation.Entities.FirstOrDefault(x=>x.)
        }

        private void TransferUiComponentClosed(object sender, engenious.UI.NavigationEventArgs e)
        {
            animationComponent.AnimationSpeed = -60f;
        }

        public override void OnNotification(SerializableNotification notification)
        {
            base.OnNotification(notification);

            var functionalBlockNotification = functionalBlockNotificationPool.Get();
            functionalBlockNotification.Block = this;
            functionalBlockNotification.Type = FunctionalBlockNotification.ActionType.Update;
            functionalBlockNotification.Notification = notification as PropertyChangedNotification;

            Simulation?.OnUpdate(functionalBlockNotification);
            functionalBlockNotification.Release();
        }



        protected override void OnInteract(GameTime gameTime, Entity entity)
        {
            if (entity is Player p)
            {
                transferUiComponent.Show(p);
                animationComponent.CurrentTime = 0f;
                animationComponent.AnimationSpeed = 60f;
            }
            else
            {
            }
        }
    }
}
