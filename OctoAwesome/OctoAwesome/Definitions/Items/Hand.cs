using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Definitions.Items
{
    public class Hand : Item
    {
        public Hand(HandDefinition handDefinition) : base(handDefinition, null)
        {

        }

        public override int Hit(IMaterialDefinition material, BlockInfo blockInfo, decimal volumeRemaining, int volumePerHit)
        {
            if(material is ISolidMaterialDefinition solidMaterial)
            {
                if (solidMaterial.Granularity > 1)
                    return volumePerHit / 3;
            }
            if(material is IGasMaterialDefinition || material is IFluidMaterialDefinition)
                return 0;

            return volumePerHit - material.Hardness / 2;
        }
    }
}
