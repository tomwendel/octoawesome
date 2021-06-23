using OctoAwesome.Definitions;
using OctoAwesome.Definitions.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.Definitions.Items
{
    public class ChestItemDefinition : IItemDefinition
    {
        public string Name { get; }
        public string Icon { get; }

        public ChestItemDefinition()
        {
            Name = "Chest";
            Icon = "chest";
        }

        public bool CanMineMaterial(IMaterialDefinition material) 
            => false;

        public Item Create(IMaterialDefinition material) 
            => new ChestItem(this, material);
    }
}
