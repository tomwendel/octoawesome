using engenious;
using OctoAwesome.Basics.FunctionBlocks;
using OctoAwesome.Definitions;
using OctoAwesome.Definitions.Items;
using OctoAwesome.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.Definitions.Items
{
    public class ChestItem : Item
    {
        public override int VolumePerUnit => base.VolumePerUnit;

        public override int StackLimit => base.StackLimit;


        private readonly IUpdateHub updateHub;

        public ChestItem(ChestItemDefinition definition, IMaterialDefinition materialDefinition)
            : base(definition, materialDefinition)
        {
            updateHub = TypeContainer.Get<IUpdateHub>();
        }

        public override int Hit(IMaterialDefinition material, BlockInfo blockInfo, decimal volumeRemaining, int volumePerHit)
        {
            //TODO: Implement Place Chest and remove this item
            var position = blockInfo.Position;
            Chest chest = new(new Coordinate(0, new (position.X, position.Y, position.Z + 1), new Vector3(0.5f, 0.5f, 0.5f)));
            var notification = new FunctionalBlockNotification
            {
                Block = chest,
                Type = FunctionalBlockNotification.ActionType.Add
            };

            updateHub.Push(notification, DefaultChannels.Simulation);

            //updateHub.Push(notification, DefaultChannels.Network);
            return 0;
        }
    }
}
