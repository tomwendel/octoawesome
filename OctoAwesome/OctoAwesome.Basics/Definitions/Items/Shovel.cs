using OctoAwesome.Definitions;
using OctoAwesome.Definitions.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.Definitions.Items
{
    class Shovel : Item
    {
        public Shovel(ShovelDefinition definition, IMaterialDefinition materialDefinition)
            : base(definition, materialDefinition)
        {

        }

        public override int Hit(IMaterialDefinition material, BlockInfo blockInfo, decimal volumeRemaining, int volumePerHit)
        {
            if (!Definition.CanMineMaterial(material))
                return 0;

            if (material is ISolidMaterialDefinition solid)
            {
                if (solid.Granularity <= 1)
                    return 0;

                //if (solid * 1.2f < material.Hardness)
                //    return 0;

                return (int)(Math.Sin(solid.Granularity / 40) * 2 * volumePerHit);
            }

            return 0;

        }
    }
}
