using engenious;
using engenious.Graphics;
using engenious.Helper;
using OctoAwesome.EntityComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using engenious.UserDefined.Shaders;

namespace OctoAwesome.Client.Components
{
    internal sealed class EntityComponent : GameComponent
    {
        private class EntityModelEffect : entityEffect, IModelEffect
        {
            private Matrix projection;
            private Matrix view;
            private Matrix world;
            private Texture texture;

            public EntityModelEffect(GraphicsDevice graphicsDevice)
                : base(graphicsDevice)
            {
            }

            public Matrix Projection
            {
                get => projection;
                set
                {
                    if (projection == value)
                        return;
                    projection = value;
                    UpdateViewProjection();
                }
            }

            public Matrix View
            {
                get => view;
                set
                {
                    if (view == value)
                        return;
                    view = value;
                    UpdateViewProjection();
                }
            }

            private void UpdateViewProjection()
            {
                var viewProjection = Projection * View;
                Ambient.ViewProjection = viewProjection;
                Shadow.ViewProjection = viewProjection;
            }

            public Matrix World
            {
                get => world;
                set
                {
                    if (world == value)
                        return;
                    world = value;
                    Ambient.World = world;
                    Shadow.World = world;
                }
            }

            public Texture Texture
            {
                get => texture;
                set
                {
                    texture = value;
                    Ambient.Texture = (Texture2D)texture;
                }
            }
        }
        private struct ModelInfo
        {
            public bool render;
            public Texture2D texture;
            public Model model;
        }
        private GraphicsDevice graphicsDevice;
        private EntityModelEffect effect;
        public SimulationComponent Simulation { get; private set; }


        private Dictionary<string, ModelInfo> models = new Dictionary<string, ModelInfo>();


        public List<Entity> Entities { get; set; }

        public EntityComponent(OctoGame game, SimulationComponent simulation) : base(game)
        {
            Simulation = simulation;

            Entities = new List<Entity>();
            graphicsDevice = game.GraphicsDevice;

            effect = game.Content.Load<EntityModelEffect>("Shaders/entityEffect");
        }

        public void SetLightEnvironment(Vector3 sunDirection)
        {
            effect.Ambient.AmbientIntensity = 0.4f;
            effect.Ambient.AmbientColor = Color.White;
            effect.Ambient.DiffuseColor = new Color(190, 190, 190);
            effect.Ambient.DiffuseIntensity = 0.6f;
            effect.Ambient.DiffuseDirection = sunDirection;
        }

        private int i = 0;
        public void Draw(Matrix view, Matrix projection, Index3 chunkOffset, Index2 planetSize)
        {
            effect.CurrentTechnique = effect.Ambient;
            effect.Ambient.ViewProjection = projection * view;
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            using (var writer = File.AppendText(Path.Combine(".", "render.log")))
                foreach (var entity in Entities)
                {
                    if (!entity.Components.ContainsComponent<RenderComponent>())
                    {
                        continue;
                    }

                    var rendercomp = entity.Components.GetComponent<RenderComponent>();


                    if (!models.TryGetValue(rendercomp.Name, out ModelInfo modelinfo))
                    {
                        modelinfo = new ModelInfo()
                        {
                            render = true,
                            model = Game.Content.Load<Model>(rendercomp.ModelName),
                            texture = Game.Content.Load<Texture2D>(rendercomp.TextureName),
                        };
                    }

                    if (!modelinfo.render)
                        continue;

                    var positioncomp = entity.Components.GetComponent<PositionComponent>();
                    var position = positioncomp.Position;
                    var body = entity.Components.GetComponent<BodyComponent>();

                    HeadComponent head = new HeadComponent();
                    if (entity.Components.ContainsComponent<HeadComponent>())
                        head = entity.Components.GetComponent<HeadComponent>();

                    Index3 shift = chunkOffset.ShortestDistanceXY(
                   position.ChunkIndex, planetSize);

                    var rotation = MathHelper.WrapAngle(positioncomp.Direction + MathHelper.ToRadians(rendercomp.BaseZRotation));

                    Matrix world = Matrix.CreateTranslation(
                        shift.X * Chunk.CHUNKSIZE_X + position.LocalPosition.X,
                        shift.Y * Chunk.CHUNKSIZE_Y + position.LocalPosition.Y,
                        shift.Z * Chunk.CHUNKSIZE_Z + position.LocalPosition.Z) * Matrix.CreateScaling(body.Radius * 2, body.Radius * 2, body.Height) * Matrix.CreateRotationZ(rotation);
                    effect.Ambient.World = world;
                    modelinfo.model.Transform = world;

                    modelinfo.model.Draw(effect, modelinfo.texture);
                }
        }
        public void DrawShadow(Matrix view, Matrix projection, Index3 chunkOffset, Index2 planetSize)
        {
            effect.CurrentTechnique = effect.Shadow;
            effect.Shadow.ViewProjection = projection * view;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            using (var writer = File.AppendText(Path.Combine(".", "render.log")))
                foreach (var entity in Entities)
                {
                    if (!entity.Components.ContainsComponent<RenderComponent>())
                    {
                        continue;
                    }

                    var rendercomp = entity.Components.GetComponent<RenderComponent>();


                    if (!models.TryGetValue(rendercomp.Name, out ModelInfo modelinfo))
                    {
                        modelinfo = new ModelInfo()
                        {
                            render = true,
                            model = Game.Content.Load<Model>(rendercomp.ModelName),
                            texture = Game.Content.Load<Texture2D>(rendercomp.TextureName),
                        };
                    }

                    if (!modelinfo.render)
                        continue;

                    var positioncomp = entity.Components.GetComponent<PositionComponent>();
                    var position = positioncomp.Position;
                    var body = entity.Components.GetComponent<BodyComponent>();

                    HeadComponent head = new HeadComponent();
                    if (entity.Components.ContainsComponent<HeadComponent>())
                        head = entity.Components.GetComponent<HeadComponent>();

                    Index3 shift = chunkOffset.ShortestDistanceXY(
                   position.ChunkIndex, planetSize);

                    var rotation = MathHelper.WrapAngle(positioncomp.Direction + MathHelper.ToRadians(rendercomp.BaseZRotation));

                    Matrix world = Matrix.CreateTranslation(
                        shift.X * Chunk.CHUNKSIZE_X + position.LocalPosition.X,
                        shift.Y * Chunk.CHUNKSIZE_Y + position.LocalPosition.Y,
                        shift.Z * Chunk.CHUNKSIZE_Z + position.LocalPosition.Z) * Matrix.CreateScaling(body.Radius * 2, body.Radius * 2, body.Height) * Matrix.CreateRotationZ(rotation);
                    effect.Shadow.World = world;
                    modelinfo.model.Transform = world;

                    modelinfo.model.Draw(effect, modelinfo.texture);
                }
        }
        

        public override void Update(GameTime gameTime)
        {
            if (Simulation?.Simulation == null)
                return;

            var simulation = Simulation.Simulation;

            if (!(simulation.State == SimulationState.Running || simulation.State == SimulationState.Paused))
                return;

            Entities.Clear();
            foreach (var item in simulation.Entities)
            {
                if (item.Components.ContainsComponent<PositionComponent>())
                    Entities.Add(item);
            }
            //base.Update(gameTime);
        }
    }
}
