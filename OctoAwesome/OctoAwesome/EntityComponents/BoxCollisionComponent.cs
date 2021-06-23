using engenious;
using OctoAwesome.Components;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.EntityComponents
{
    public sealed class BoxCollisionComponent : CollisionComponent, IFunctionalBlockComponent
    {
        public ReadOnlySpan<BoundingBox> BoundingBoxes => new(boundingBoxes);

        private readonly BoundingBox[] boundingBoxes;

        public BoxCollisionComponent()
        {

        }

        public BoxCollisionComponent(BoundingBox[] boundingBoxes)
        {
            this.boundingBoxes = boundingBoxes;
        }
    }
}
