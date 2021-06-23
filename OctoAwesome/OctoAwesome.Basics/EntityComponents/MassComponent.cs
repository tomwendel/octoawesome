using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.EntityComponents
{
    public sealed class MassComponent : Component, IEntityComponent
    {
        public float Mass { get; set; }
    }
}
