using System;
using System.Collections.Generic;
using System.Linq;
using engenious;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Components;
using OctoAwesome.EntityComponents;
using SimulationComponentRecord = OctoAwesome.Components.SimulationComponentRecord<
                                    OctoAwesome.Entity,
                                    OctoAwesome.Basics.EntityComponents.MoveableComponent,
                                    OctoAwesome.EntityComponents.BodyComponent>;

namespace OctoAwesome.Basics.SimulationComponents
{
    public sealed class AccelerationComponent : SimulationComponent<
        Entity,
        AccelerationComponent.AcceleratedEntity,
        MoveableComponent, 
        BodyComponent>
    {

        protected override void UpdateValue(GameTime gameTime, AcceleratedEntity entity)
        {
                // Convert external Forces to Powers
                Vector3 power = ((entity.Move.ExternalForces * entity.Move.ExternalForces) / (2 * entity.Body.Mass)) * 
                    (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Take care of direction
                power *= new Vector3(
                    Math.Sign(entity.Move.ExternalForces.X), 
                    Math.Sign(entity.Move.ExternalForces.Y), 
                    Math.Sign(entity.Move.ExternalForces.Z));

                // Add external Power
                power += entity.Move.ExternalPowers;

                // Friction Power
                power -= new Vector3(60F,60f,0.1f) * entity.Move.Velocity;

                //DEBUGGING
                var a1 = 2.0f / entity.Body.Mass;
                var b = 2 / entity.Body.Mass;
                var a2 = a1 * power;
                var c = new Vector3(a1 * power.X, a1 * power.Y, a1 * power.Z);

                var a3 = a2 * (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                //DEBUGGING


                // Calculate Velocity change
                Vector3 velocityChange = ((2.0f / entity.Body.Mass) * power) *
                    (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Take care of direction
                entity.Move.Velocity += new Vector3(
                    (float)(velocityChange.X < 0 ? -Math.Sqrt(-velocityChange.X) : Math.Sqrt(velocityChange.X)),
                    (float)(velocityChange.Y < 0 ? -Math.Sqrt(-velocityChange.Y) : Math.Sqrt(velocityChange.Y)),
                    (float)(velocityChange.Z < 0 ? -Math.Sqrt(-velocityChange.Z) : Math.Sqrt(velocityChange.Z)));

                // Calculate Move Vector for the upcoming frame
                entity.Move.PositionMove = entity.Move.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        protected override AcceleratedEntity OnAdd(Entity entity) 
            => new AcceleratedEntity(
                entity,
                entity.Components.GetComponent<MoveableComponent>(),
                entity.Components.GetComponent<BodyComponent>());


        public record AcceleratedEntity(Entity Entity, MoveableComponent Move, BodyComponent Body)
            : SimulationComponentRecord(Entity, Move, Body);
    }
}
