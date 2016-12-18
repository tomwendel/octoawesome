﻿using OctoAwesome.Basics.Definitions.Blocks;
using OctoAwesome.Basics.Entities;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Basics.SimulationComponents;
using OctoAwesome.EntityComponents;
using System.Reflection;
using System.Linq;
using System;
using engenious;

namespace OctoAwesome.Basics
{
    public sealed class Extension : IExtension
    {
        public string Description
        {
            get
            {
                return Languages.OctoBasics.ExtensionDescription;
            }
        }

        public string Name
        {
            get
            {
                return Languages.OctoBasics.ExtensionName;
            }
        }

        public void Register(IExtensionLoader extensionLoader)
        {

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(
                t => t.Namespace == "OctoAwesome.Basics.Definitions.Blocks" &&
                typeof(IDefinition).IsAssignableFrom(t)))
            {
                extensionLoader.RegisterDefinition((IDefinition)Activator.CreateInstance(t));
            }



            extensionLoader.RegisterMapGenerator(new ComplexPlanetGenerator());

            //extensionLoader.RegisterDefinition(new GroundBlockDefinition());
            //extensionLoader.RegisterDefinition(new StoneBlockDefinition());
            //extensionLoader.RegisterDefinition(new GrassBlockDefinition());
            //extensionLoader.RegisterDefinition(new SandBlockDefinition());
            //extensionLoader.RegisterDefinition(new SnowBlockDefinition());
            //extensionLoader.RegisterDefinition(new WaterBlockDefinition());


            extensionLoader.RegisterEntity<AppleEntity>();

            extensionLoader.RegisterEntityExtender<Player>((p) =>
            {

                p.Components.AddComponent(new GravityComponent());
                p.Components.AddComponent(new PositionComponent() { Position = new Coordinate(0,new Index3(0,0,200),new Vector3(0,0,0)) } );
                p.Components.AddComponent(new BodyComponent() { Mass = 50f, Height = 3.5f, Radius = 0.75f });
                p.Components.AddComponent(new BodyPowerComponent() { Power = 600f, JumpTime = 120 });
                p.Components.AddComponent(new MoveableComponent());
                p.Components.AddComponent(new BoxCollisionComponent());
                p.Components.AddComponent(new EntityCollisionComponent());
            });

            extensionLoader.RegisterSimulationExtender((s) =>
            {
                s.Components.AddComponent(new WattMoverComponent());
                s.Components.AddComponent(new NewtonGravitatorComponent());
                s.Components.AddComponent(new ForceAggregatorComponent());
                s.Components.AddComponent(new PowerAggregatorComponent());
                s.Components.AddComponent(new AccelerationComponent());
                s.Components.AddComponent(new MoveComponent());
                s.Components.AddComponent(new BlockInteractionComponent(s));
            });
        }
    }
}
