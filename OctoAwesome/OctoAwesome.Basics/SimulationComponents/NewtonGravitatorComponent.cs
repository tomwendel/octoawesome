using OctoAwesome.Basics.EntityComponents;
using engenious;
using OctoAwesome.EntityComponents;
using OctoAwesome.Components;
using SimulationComponentRecord = OctoAwesome.Components.SimulationComponentRecord<
                                    OctoAwesome.Entity,
                                    OctoAwesome.Basics.EntityComponents.GravityComponent,
                                    OctoAwesome.EntityComponents.BodyComponent>;

namespace OctoAwesome.Basics.SimulationComponents
{
    public class NewtonGravitatorComponent : SimulationComponent<
        Entity,
        NewtonGravitatorComponent.GravityEntity,
        GravityComponent,
        BodyComponent>
    {
        protected override void UpdateValue(GameTime gameTime, GravityEntity gravityEntity)
        {
            var gravity = 10f;

            var positionComponent = gravityEntity.Entity.Components.GetComponent<PositionComponent>();
            if (positionComponent != null)
            {
                var id = positionComponent.Position.Planet;
                var planet = gravityEntity.Entity.Simulation.ResourceManager.GetPlanet(id);
                gravity = planet.Gravity;
            }

            gravityEntity.GravityComponent.Force = new Vector3(0, 0, -gravityEntity.BodyComponent.Mass * gravity);
        }

        protected override GravityEntity OnAdd(Entity entity)
            => new GravityEntity(
                entity,
                entity.Components.GetComponent<GravityComponent>(),
                entity.Components.GetComponent<BodyComponent>());

        public record GravityEntity(Entity Entity, GravityComponent GravityComponent, BodyComponent BodyComponent)
            : SimulationComponentRecord(Entity, GravityComponent, BodyComponent);
    }
}
