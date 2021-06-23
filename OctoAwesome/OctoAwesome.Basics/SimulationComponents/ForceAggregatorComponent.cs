using System.Linq;
using engenious;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Components;
using SimulationComponentRecord = OctoAwesome.Components.SimulationComponentRecord<
                                    OctoAwesome.Entity,
                                    OctoAwesome.Basics.EntityComponents.ForceComponent,
                                    OctoAwesome.Basics.EntityComponents.MoveableComponent>;

namespace OctoAwesome.Basics.SimulationComponents
{
    public sealed class ForceAggregatorComponent : SimulationComponent<
            Entity,
            ForceAggregatorComponent.ForcedEntity,
            ForceComponent,
            MoveableComponent>
    {

        protected override ForcedEntity OnAdd(Entity entity)
        {
            return new ForcedEntity(entity,
                null,
                entity.Components.GetComponent<MoveableComponent>(),
                entity.Components.OfType<ForceComponent>().ToArray());
        }

        protected override void UpdateValue(GameTime gameTime, ForcedEntity forcedEntity)
        {
            forcedEntity.MoveableComponent.ExternalForces =
                forcedEntity.Forces.Aggregate(Vector3.Zero, (s, f) => s + f.Force);
        }

        public record ForcedEntity(Entity Entity, ForceComponent ForceComponent, MoveableComponent MoveableComponent, ForceComponent[] Forces)
            : SimulationComponentRecord(Entity, ForceComponent, MoveableComponent);

    }
}
