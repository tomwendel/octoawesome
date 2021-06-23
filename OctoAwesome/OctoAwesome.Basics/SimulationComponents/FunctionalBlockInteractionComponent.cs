using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using engenious;
using OctoAwesome.Services;
using OctoAwesome.Definitions.Items;
using OctoAwesome.Definitions;
using OctoAwesome.Components;

namespace OctoAwesome.Basics.SimulationComponents
{
    public class FunctionalBlockInteractionComponent : SimulationComponent<
        Entity,
        SimulationComponentRecord<Entity, ControllableComponent, InventoryComponent>,
        ControllableComponent,
        InventoryComponent>
    {
        private readonly Simulation simulation;
        private readonly BlockCollectionService service;


        public FunctionalBlockInteractionComponent(Simulation simulation, BlockCollectionService interactionService)
        {
            this.simulation = simulation;
            service = interactionService;
        }

        protected override void UpdateValue(GameTime gameTime, SimulationComponentRecord<Entity, ControllableComponent, InventoryComponent> value)
        {
            var entity = value.Value;
            var controller = value.Component1;

            controller
                .Selection?
                .Map(
                blockInfo => { },
                functionalBlock => InternalUpdate(controller, entity, functionalBlock),
                entity => { }
                );
        }

        private void InternalUpdate(ControllableComponent controller, Entity entity, FunctionalBlock functionalBlock)
        {
        }
    }
}
