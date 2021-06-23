using System.Linq;
using engenious;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Components;
using SimulationComponentRecord = OctoAwesome.Components.SimulationComponentRecord<
                                    OctoAwesome.Entity,
                                    OctoAwesome.Basics.EntityComponents.PowerComponent,
                                    OctoAwesome.Basics.EntityComponents.MoveableComponent>;

namespace OctoAwesome.Basics.SimulationComponents
{
    public sealed class PowerAggregatorComponent : SimulationComponent<
        Entity,
        PowerAggregatorComponent.PoweredEntity,
        PowerComponent,
        MoveableComponent>
    {

        protected override void UpdateValue(GameTime gameTime, PoweredEntity poweredEntity)
        {
            poweredEntity.Moveable.ExternalPowers =
                    poweredEntity.Powers.Aggregate(Vector3.Zero, (s, f) => s + f.Power * f.Direction);
        }

        protected override PoweredEntity OnAdd(Entity entity)
            => new PoweredEntity(entity, entity.Components.GetComponent<MoveableComponent>(), entity.Components.OfType<PowerComponent>().ToArray());

        public record PoweredEntity(Entity Entity, MoveableComponent Moveable, PowerComponent[] Powers)
            : SimulationComponentRecord(Entity, default, Moveable);
    }
}
