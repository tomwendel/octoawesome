using OctoAwesome.Definitions;
using OctoAwesome.Definitions.Items;
using OctoAwesome.OctoMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.Definitions.Items
{
    public class Axe : Item
    {

        private static readonly Polynomial polynomial;

        static Axe()
        {
            polynomial = new Polynomial(0, 3f / 8f, 1f / 800f, -1f / 320000f);
        }

        public Axe(AxeDefinition definition, IMaterialDefinition materialDefinition)
            : base(definition, materialDefinition)
        {

        }

        public override int Hit(IMaterialDefinition material, BlockInfo blockInfo, decimal volumeRemaining, int volumePerHit)
        {
            //⁅𝑥^2/800+3𝑥/8+(−𝑥^3)/320000⁆
            var baseEfficiency = base.Hit(material, blockInfo, volumeRemaining, volumePerHit);

            if (material is ISolidMaterialDefinition solid && baseEfficiency > 0)
            {
                var fractureEfficiency = polynomial.Evaluate(solid.FractureToughness); 
                return (int)(baseEfficiency * (fractureEfficiency) / 100);
            }

            return baseEfficiency;
        }
    }
}
