using engenious;
using OctoAwesome.Components;

namespace OctoAwesome.Basics.SimulationComponents
{
    public sealed class CollisionComponent : SimulationComponent<Entity>
    {
        public override void Update(GameTime gameTime) { }
        protected override void OnAdd(Entity value) => throw new System.NotImplementedException();
        protected override void OnRemove(Entity value) => throw new System.NotImplementedException();
    }
}
