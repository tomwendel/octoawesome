using engenious;
using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Basics.EntityComponents
{
    public abstract class ForceComponent : Component, IEntityComponent
    {
        public Vector3 Force { get; set; }


    }
}
