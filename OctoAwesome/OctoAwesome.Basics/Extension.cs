using OctoAwesome.Basics.Definitions.Blocks;
using OctoAwesome.Basics.Entities;
using OctoAwesome.Basics.EntityComponents;
using OctoAwesome.Basics.SimulationComponents;
using OctoAwesome.EntityComponents;
using System.Reflection;
using System.Linq;
using System;
using engenious;
using OctoAwesome.Services;
using OctoAwesome.Definitions;

namespace OctoAwesome.Basics
{
    public sealed class Extension : IExtension
    {
        public string Description => Languages.OctoBasics.ExtensionDescription;

        public string Name => Languages.OctoBasics.ExtensionName;


        public void Register(ITypeContainer typeContainer)
        {
            
        }

        public void Register(IExtensionLoader extensionLoader, ITypeContainer typeContainer)
        {
            typeContainer.Register<IPlanet, ComplexPlanet>();

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {                
                if (!t.IsAbstract && typeof(IDefinition).IsAssignableFrom(t))
                    extensionLoader.RegisterDefinition(t);
            }

            extensionLoader.RegisterMapGenerator(new ComplexPlanetGenerator());

            extensionLoader.RegisterMapPopulator(new TreePopulator());
            extensionLoader.RegisterMapPopulator(new WauziPopulator());

            extensionLoader.RegisterEntity<WauziEntity>();
            extensionLoader.RegisterDefaultEntityExtender<WauziEntity>();

            extensionLoader.RegisterEntityExtender<Player>((p) =>
            {
                var posComponent = new PositionComponent { Position = new Coordinate(0, new Index3(0, 0, 200), new Vector3(0, 0, 0)) };

                p.Components.AddComponent(posComponent);
                p.Components.AddComponent(new BodyComponent() { Mass = 50f, Height = 3.5f, Radius = 0.75f });
                p.Components.AddComponent(new BodyPowerComponent() { Power = 600f, JumpTime = 120 });
                p.Components.AddComponent(new GravityComponent());
                p.Components.AddComponent(new MoveableComponent());
                p.Components.AddComponent(new BoxCollisionComponent(Array.Empty<BoundingBox>()));
                p.Components.AddComponent(new EntityCollisionComponent());
                p.Components.AddComponent(new LocalChunkCacheComponent(posComponent.Planet.GlobalChunkCache, 4, 2));

            });

            
            extensionLoader.RegisterSimulationExtender((s) =>
            {
                s.Components.AddComponent(new WattMoverComponent());
                s.Components.AddComponent(new NewtonGravitatorComponent());
                s.Components.AddComponent(new ForceAggregatorComponent());
                s.Components.AddComponent(new PowerAggregatorComponent());
                s.Components.AddComponent(new AccelerationComponent());
                s.Components.AddComponent(new MoveComponent());
                s.Components.AddComponent(new BlockInteractionComponent(s, typeContainer.Get<BlockCollectionService>()));

                //TODO: unschön
                //TODO: TypeContainer?
            });
        }
    }
}
