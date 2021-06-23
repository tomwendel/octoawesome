using OctoAwesome.Definitions;
using OctoAwesome.Definitions.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.Definitions.Items
{
    class Bucket : Item, IFluidInventory
    {
        public int Quantity { get; private set; }
        public IBlockDefinition FluidBlock { get; private set; }
        public int MaxQuantity { get; }

        public Bucket(BucketDefinition definition, IMaterialDefinition materialDefinition)
            : base(definition, materialDefinition)
        {
            MaxQuantity = 125;
        }

        public void AddFluid(int quantity, IBlockDefinition fluidBlock)
        {
            if (!Definition.CanMineMaterial(fluidBlock.Material))
                return;

            Quantity += quantity;
            FluidBlock = fluidBlock;
        }

        public override int Hit(IMaterialDefinition material, BlockInfo blockInfo, decimal volumeRemaining, int volumePerHit)
        {
            if (!Definition.CanMineMaterial(material))
                return 0;

            if (material is IFluidMaterialDefinition fluid)
            {
                if (!(FluidBlock is null) && fluid != FluidBlock.Material)
                    return 0;

                if (Quantity + volumePerHit >= MaxQuantity)
                    return MaxQuantity - Quantity;

                return volumePerHit;
            }


            return base.Hit(material, blockInfo, volumeRemaining, volumePerHit);
        }
    }
}
