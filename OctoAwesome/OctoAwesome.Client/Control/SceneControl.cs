using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OctoAwesome.Client.Components;
using System.Drawing.Imaging;
using engenious;
using engenious.Graphics;
using engenious.Helper;
using engenious.UI;
using engenious.UserDefined;
using OctoAwesome.Definitions;
using System.Threading.Tasks;
using engenious.UserDefined.Shaders;
using OctoAwesome.EntityComponents;
using System.Diagnostics;
using System.Linq;
using OctoAwesome.EntityComponents;
using OctoAwesome.UI.Components;

namespace OctoAwesome.Client.Controls
{
    internal sealed class SceneControl : Control, IDisposable
    {
        public static int VIEWRANGE = 4; // Anzahl Chunks als Potenz (Volle Sichtweite)
        public const int TEXTURESIZE = 64;
        public static int Mask;
        public static int Span;
        public static int SpanOver2;

        private PlayerComponent player;
        private CameraComponent camera;
        private AssetComponent assets;
        private EntityGameComponent entities;

        private ChunkRenderer[,] chunkRenderer;
        private readonly List<ChunkRenderer> orderedChunkRenderer;

        private IPlanet planet;

        // private List<Index3> distances = new List<Index3>();

        private readonly BasicEffect sunEffect;
        private readonly BasicEffect selectionEffect;
        private Matrix miniMapProjectionMatrix;

        //private Texture2D blockTextures;
        private readonly Texture2DArray blockTextures;
        private readonly Texture2D sunTexture;

        private readonly IndexBuffer selectionIndexBuffer;
        private readonly VertexBuffer selectionLines;
        private readonly VertexBuffer billboardVertexbuffer;
        //private VertexPositionColor[] selectionLines;
        //private VertexPositionTexture[] billboardVertices;

        private Index2 currentChunk = new Index2(-1, -1);

        private readonly Task backgroundTask;
        private readonly Task backgroundThread2;
        private ILocalChunkCache localChunkCache;
        private readonly chunkEffect simpleShader;

        private readonly Task[] _additionalRegenerationThreads;

        public RenderTarget2D MiniMapTexture { get; set; }
        public RenderTarget2D ControlTexture { get; set; }
        public RenderTarget2D ShadowMap { get; set; }

        private float sunPosition = 0f;
        private Matrix sunTransform;

        public event EventHandler OnCenterChanged;

        private readonly VertexPositionColor[] selectionVertices =
        {
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, -0.001f), Color.Black * 0.5f),
        };
        private readonly VertexPositionTexture[] billboardVertices =
        {
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(0.5f, -0.5f, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1)),
        };

        private readonly float sphereRadius;
        private readonly float sphereRadiusSquared;

        private ScreenComponent Manager { get; set; }

        private readonly int _fillIncrement;
        private readonly CancellationTokenSource cancellationTokenSource;

        private bool updateShadowMap;

        public SceneControl(ScreenComponent manager, string style = "") :
            base(manager, style)
        {
            Mask = (int)Math.Pow(2, VIEWRANGE) - 1;
            Span = (int)Math.Pow(2, VIEWRANGE);
            SpanOver2 = Span >> 1;

            player = manager.Player;
            camera = manager.Camera;
            assets = manager.Game.Assets;
            entities = manager.Game.Entity;
            Manager = manager;

            cancellationTokenSource = new CancellationTokenSource();

            var chunkDiag = (float)Math.Sqrt((Chunk.CHUNKSIZE_X * Chunk.CHUNKSIZE_X) + (Chunk.CHUNKSIZE_Y * Chunk.CHUNKSIZE_Y) + (Chunk.CHUNKSIZE_Z * Chunk.CHUNKSIZE_Z));
            var tmpSphereRadius = (float)(((Math.Sqrt((Span * Chunk.CHUNKSIZE_X) * (Span * Chunk.CHUNKSIZE_X) * 3)) / 3) + camera.NearPlaneDistance + (chunkDiag / 2));
            sphereRadius = tmpSphereRadius - (chunkDiag / 2);
            sphereRadiusSquared = tmpSphereRadius * tmpSphereRadius;

            simpleShader = manager.Game.Content.Load<chunkEffect>("Shaders/chunkEffect");
            sunTexture = assets.LoadTexture( "sun");

            //List<Bitmap> bitmaps = new List<Bitmap>();
            var definitions = Manager.Game.DefinitionManager.BlockDefinitions;
            int textureCount = 0;
            foreach (var definition in definitions)
            {
                textureCount += definition.Textures.Length;
            }
            int bitmapSize = 128;
            blockTextures = new Texture2DArray(manager.GraphicsDevice, 8, bitmapSize, bitmapSize, textureCount);
            int layer = 0;
            foreach (var definition in definitions)
            {
                foreach (var bitmap in definition.Textures)
                {
                    System.Drawing.Bitmap texture = manager.Game.Assets.LoadBitmap(definition.GetType(), bitmap);

                    var scaled = texture;//new Bitmap(bitmap, new System.Drawing.Size(bitmapSize, bitmapSize));
                    int[] data = new int[scaled.Width * scaled.Height];
                    var bitmapData = scaled.LockBits(new System.Drawing.Rectangle(0, 0, scaled.Width, scaled.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
                    blockTextures.SetData(data, layer);
                    scaled.UnlockBits(bitmapData);

                    layer++;
                }
            }

            blockTextures.RegenerateMipMaps();
            blockTextures.SamplerState = new SamplerState()
            {
                AddressU = TextureWrapMode.ClampToEdge, AddressV = TextureWrapMode.ClampToEdge, MagFilter = MagFilter.Linear, MinFilter = MinFilter.LinearMipmapLinear,
                MaxAnisotropy = 4,
            };

            planet = Manager.Game.ResourceManager.GetPlanet(player.Position.Position.Planet);

            // TODO: evtl. Cache-Size (Dimensions) VIEWRANGE + 1

            int range = ((int)Math.Pow(2, VIEWRANGE) - 2) / 2;
            localChunkCache = new LocalChunkCache(planet.GlobalChunkCache, VIEWRANGE, range);

            chunkRenderer = new ChunkRenderer[
                (int)Math.Pow(2, VIEWRANGE) * (int)Math.Pow(2, VIEWRANGE),
                planet.Size.Z];
            orderedChunkRenderer = new List<ChunkRenderer>(
                (int)Math.Pow(2, VIEWRANGE) * (int)Math.Pow(2, VIEWRANGE) * planet.Size.Z);

            for (int i = 0; i < chunkRenderer.GetLength(0); i++)
            {
                for (int j = 0; j < chunkRenderer.GetLength(1); j++)
                {
                    ChunkRenderer renderer = new ChunkRenderer(this, Manager.Game.DefinitionManager, simpleShader, manager.GraphicsDevice, camera.Projection, blockTextures);
                    chunkRenderer[i, j] = renderer;
                    orderedChunkRenderer.Add(renderer);
                }
            }

            var token = cancellationTokenSource.Token;

            backgroundTask = new Task(BackgroundLoop, token, token, TaskCreationOptions.LongRunning);
            backgroundTask.Start();

            backgroundThread2 = new Task(ForceUpdateBackgroundLoop, token, token, TaskCreationOptions.LongRunning);
            backgroundThread2.Start();

            int additional;

            if (Environment.ProcessorCount <= 4)
                additional = Environment.ProcessorCount / 3;
            else
                additional = Environment.ProcessorCount - 4;
            additional = additional == 0 ? 1 : additional;
            _fillIncrement = additional + 1;
            additionalFillResetEvents = new AutoResetEvent[additional];
            _additionalRegenerationThreads = new Task[additional];

            for (int i = 0; i < additional; i++)
            {
                var are = new AutoResetEvent(false);
                var t = new Task(AdditionalFillerBackgroundLoop, (are, i, token), token, TaskCreationOptions.LongRunning);
                t.Start();
                additionalFillResetEvents[i] = are;
                _additionalRegenerationThreads[i] = t;

            }



            var selectionVertices = new[]
            {
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, +1.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, +1.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, +1.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(-0.001f, -0.001f, -0.001f), Color.Black * 0.5f),
                new VertexPositionColor(new Vector3(+1.001f, -0.001f, -0.001f), Color.Black * 0.5f),
            };

            var billboardVertices = new[]
            {
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(0.5f, -0.5f, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 1)),
            };

            var selectionIndices = new short[]
            {
                0, 1, 0, 2, 1, 3, 2, 3,
                4, 5, 4, 6, 5, 7, 6, 7,
                0, 4, 1, 5, 2, 6, 3, 7
            };

            selectionLines = new VertexBuffer(manager.GraphicsDevice, VertexPositionColor.VertexDeclaration, selectionVertices.Length);
            selectionLines.SetData(selectionVertices);

            selectionIndexBuffer = new IndexBuffer(manager.GraphicsDevice, DrawElementsType.UnsignedShort, selectionIndices.Length);
            selectionIndexBuffer.SetData(selectionIndices);

            billboardVertexbuffer = new VertexBuffer(manager.GraphicsDevice, VertexPositionTexture.VertexDeclaration, billboardVertices.Length);
            billboardVertexbuffer.SetData(billboardVertices);


            sunEffect = new BasicEffect(manager.GraphicsDevice)
            {
                TextureEnabled = true
            };

            selectionEffect = new BasicEffect(manager.GraphicsDevice)
            {
                VertexColorEnabled = true
            };

            MiniMapTexture = new RenderTarget2D(manager.GraphicsDevice, 128, 128, PixelInternalFormat.Rgb8); // , false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            miniMapProjectionMatrix = Matrix.CreateOrthographicOffCenter(-32, 32, -32, 32, -1, 1000);
            miniMapProjectionMatrix.M22 *= -1;
            ShadowMap = new RenderTarget2D(manager.GraphicsDevice, 8192, 8192, PixelInternalFormat.DepthComponent32);
            
            
            ShadowMap.SamplerState = new SamplerState()
            {
                AddressU = TextureWrapMode.ClampToEdge, AddressV = TextureWrapMode.ClampToEdge, TextureCompareMode = TextureCompareMode.CompareRefToTexture, TextureCompareFunction = TextureCompareFunc.LessOrEequal,
            };


            Manager.Game.KeyMapper.AddAction("octoawesome:freezeLight", type =>
            {
                if (type == KeyMapper.KeyType.Up)
                {
                    updateShadowMap = true;
                }
            });
        }

        protected override void OnDrawContent(SpriteBatch batch, Rectangle contentArea, GameTime gameTime, float alpha)
        {
            if (ControlTexture != null)
                batch.Draw(ControlTexture, contentArea, Color.White * alpha);
        }

        public override void OnResolutionChanged()
        {
            base.OnResolutionChanged();

            if (ControlTexture != null)
            {
                ControlTexture.Dispose();
                ControlTexture = null;
            }

            Manager.Game.Camera.RecreateProjection();
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (disposed || player?.CurrentEntity == null)
                return;

            sunPosition += (float)gameTime.ElapsedGameTime.TotalMinutes * MathHelper.TwoPi;

            Index3 centerblock = player.Position.Position.GlobalBlockIndex;
            Index3 renderOffset = player.Position.Position.ChunkIndex * Chunk.CHUNKSIZE;

            Index3? selected;
            Axis? selectedAxis;
            Vector3? selectionPoint;

            var selBlock = GetSelectedBlock(centerblock, renderOffset, out selected, out selectedAxis, out selectionPoint);
            var funcBlock = GetSelectedFunctionalBlock(centerblock, renderOffset, out var selectedFunc, out var selectedFuncAxis, out var selectionFuncPoint);



            //TODO Distance check, so entity doesnt always get selected if a block is in front of it
            if (selectedFunc.HasValue && selectionFuncPoint.HasValue)
            {
                selected = selectedFunc;
                selectionPoint = selectionFuncPoint;
                selectedAxis = selectedFuncAxis;
                player.Selection = funcBlock;
            }
            else
            {
                player.Selection = selBlock;
            }

            if (selected.HasValue && selectionPoint.HasValue)
            {
                player.SelectedBox = selected;
                switch (selectedAxis)
                {
                    case Axis.X: player.SelectedSide = (camera.PickRay.Direction.X > 0 ? OrientationFlags.SideWest : OrientationFlags.SideEast); break;
                    case Axis.Y: player.SelectedSide = (camera.PickRay.Direction.Y > 0 ? OrientationFlags.SideSouth : OrientationFlags.SideNorth); break;
                    case Axis.Z: player.SelectedSide = (camera.PickRay.Direction.Z > 0 ? OrientationFlags.SideBottom : OrientationFlags.SideTop); break;
                }

                player.SelectedPoint = new Vector2();
                switch (player.SelectedSide)
                {
                    case OrientationFlags.SideWest:
                        player.SelectedPoint = new Vector2(1f - selectionPoint.Value.Y, 1f - selectionPoint.Value.Z);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner011, OrientationFlags.Corner001, OrientationFlags.Corner010, OrientationFlags.Corner000);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeWestTop, OrientationFlags.EdgeWestBottom, OrientationFlags.EdgeNorthWest, OrientationFlags.EdgeSouthWest);
                        break;
                    case OrientationFlags.SideEast:
                        player.SelectedPoint = new Vector2(selectionPoint.Value.Y, 1f - selectionPoint.Value.Z);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner101, OrientationFlags.Corner111, OrientationFlags.Corner100, OrientationFlags.Corner110);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeEastTop, OrientationFlags.EdgeEastBottom, OrientationFlags.EdgeSouthEast, OrientationFlags.EdgeNorthEast);
                        break;
                    case OrientationFlags.SideTop:
                        player.SelectedPoint = new Vector2(selectionPoint.Value.X, 1f - selectionPoint.Value.Y);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner011, OrientationFlags.Corner111, OrientationFlags.Corner001, OrientationFlags.Corner101);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeNorthTop, OrientationFlags.EdgeSouthTop, OrientationFlags.EdgeWestTop, OrientationFlags.EdgeEastTop);
                        break;
                    case OrientationFlags.SideBottom:
                        player.SelectedPoint = new Vector2(selectionPoint.Value.X, selectionPoint.Value.Y);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner000, OrientationFlags.Corner100, OrientationFlags.Corner010, OrientationFlags.Corner110);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeSouthBottom, OrientationFlags.EdgeNorthBottom, OrientationFlags.EdgeWestBottom, OrientationFlags.EdgeEastBottom);
                        break;
                    case OrientationFlags.SideNorth:
                        player.SelectedPoint = new Vector2(1f - selectionPoint.Value.X, 1f - selectionPoint.Value.Z);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner111, OrientationFlags.Corner011, OrientationFlags.Corner110, OrientationFlags.Corner010);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeNorthTop, OrientationFlags.EdgeNorthBottom, OrientationFlags.EdgeNorthEast, OrientationFlags.EdgeNorthWest);
                        break;
                    case OrientationFlags.SideSouth:
                        player.SelectedPoint = new Vector2(selectionPoint.Value.X, 1f - selectionPoint.Value.Z);
                        player.SelectedCorner = FindCorner(player.SelectedPoint.Value, OrientationFlags.Corner001, OrientationFlags.Corner101, OrientationFlags.Corner000, OrientationFlags.Corner100);
                        player.SelectedEdge = FindEdge(player.SelectedPoint.Value, OrientationFlags.EdgeSouthTop, OrientationFlags.EdgeSouthBottom, OrientationFlags.EdgeSouthWest, OrientationFlags.EdgeSouthEast);
                        break;
                }

                player.SelectedPoint = new Vector2(
                    Math.Min(1f, Math.Max(0f, player.SelectedPoint.Value.X)),
                    Math.Min(1f, Math.Max(0f, player.SelectedPoint.Value.Y)));
            }
            else
            {
                player.SelectedBox = null;
                player.SelectedPoint = null;
                player.SelectedSide = OrientationFlags.None;
                player.SelectedEdge = OrientationFlags.None;
                player.SelectedCorner = OrientationFlags.None;
            }

            Index2 destinationChunk = new Index2(player.Position.Position.ChunkIndex);

            // Nur ausführen wenn der Spieler den Chunk gewechselt hat
            if (destinationChunk != currentChunk)
            {
                fillResetEvent.Set();
            }

            base.OnUpdate(gameTime);
        }

        private BlockInfo GetSelectedBlock(Index3 centerblock, Index3 renderOffset, out Index3? selected, out Axis? selectedAxis, out Vector3? selectionPoint)
        {
            selected = null;
            selectedAxis = null;
            selectionPoint = null;
            float bestDistance = 9999;
            BlockInfo block = default;
            //var pickEndPost = centerblock + (camera.PickRay.Position + (camera.PickRay.Direction * Player.SELECTIONRANGE));
            //var pickStartPos = centerblock + camera.PickRay.Position;
            for (int z = -Player.SELECTIONRANGE; z < Player.SELECTIONRANGE; z++)
            {
                for (int y = -Player.SELECTIONRANGE; y < Player.SELECTIONRANGE; y++)
                {
                    for (int x = -Player.SELECTIONRANGE; x < Player.SELECTIONRANGE; x++)
                    {
                        Index3 range = new Index3(x, y, z);
                        Index3 pos = range + centerblock;
                        var localBlock = localChunkCache.GetBlockInfo(pos);
                        if (localBlock.Block == 0)
                            continue;
                        IBlockDefinition blockDefinition = Manager.Game.DefinitionManager.GetBlockDefinitionByIndex(localBlock.Block);

                        float? distance = Block.Intersect(blockDefinition.GetCollisionBoxes(localChunkCache, pos.X, pos.Y, pos.Z), pos - renderOffset, camera.PickRay, out Axis? collisionAxis);

                        if (distance.HasValue && distance.Value < bestDistance)
                        {
                            pos.NormalizeXY(planet.Size * Chunk.CHUNKSIZE);
                            selected = pos;
                            //var futureselected = PythonBresenham((int)pickStartPos.X, (int)pickStartPos.Y, (int)pickStartPos.Z, (int)pickEndPost.X, (int)pickEndPost.Y, (int)pickEndPost.Z);
                            //if (futureselected is not null)
                            //{
                            //    futureselected.Value.NormalizeXY(planet.Size * Chunk.CHUNKSIZE);

                            //    Debug.WriteLine($"Old Selection: {selected}, New Selection: {futureselected}");

                            //    selected = futureselected;
                            //}
                            selectedAxis = collisionAxis;
                            bestDistance = distance.Value;
                            selectionPoint = (camera.PickRay.Position + (camera.PickRay.Direction * distance)) - (selected - renderOffset);
                            block = localBlock;
                        }

                    }
                }
            }
            return block;
        }

        private FunctionalBlock GetSelectedFunctionalBlock(Index3 centerblock, Index3 renderOffset, out Index3? selected, out Axis? selectedAxis, out Vector3? selectionPoint)
        {
            selected = null;
            selectedAxis = null;
            selectionPoint = null;
            float bestDistance = 9999;
            FunctionalBlock functionalBlock = null;

            //Index3 centerblock = player.Position.Position.GlobalBlockIndex;
            //Index3 renderOffset = player.Position.Position.ChunkIndex * Chunk.CHUNKSIZE;
            foreach (var funcBlock in Manager.Game.Simulation.Simulation.FunctionalBlocks)
            {
                if (!funcBlock.ContainsComponent<PositionComponent>() || !funcBlock.ContainsComponent<BoxCollisionComponent>())
                    continue;

                var posComponent = funcBlock.GetComponent<PositionComponent>();
                var boxCollisionComponent = funcBlock.GetComponent<BoxCollisionComponent>();

                if (posComponent.Position.GlobalPosition.X - Player.SELECTIONRANGE < centerblock.X
                    && posComponent.Position.GlobalPosition.X + Player.SELECTIONRANGE > centerblock.X
                    && posComponent.Position.GlobalPosition.Y - Player.SELECTIONRANGE < centerblock.Y
                    && posComponent.Position.GlobalPosition.Y + Player.SELECTIONRANGE > centerblock.Y
                    && posComponent.Position.GlobalPosition.Z - Player.SELECTIONRANGE < centerblock.Z
                    && posComponent.Position.GlobalPosition.Z + Player.SELECTIONRANGE > centerblock.Z)
                {

                    float? distance = Block.Intersect(boxCollisionComponent.BoundingBoxes, posComponent.Position.GlobalBlockIndex - renderOffset, camera.PickRay, out Axis? collisionAxis);

                    if (distance.HasValue && distance.Value < bestDistance)
                    {
                        posComponent.Position.GlobalBlockIndex.NormalizeXY(planet.Size * Chunk.CHUNKSIZE);
                        selected = posComponent.Position.GlobalBlockIndex;

                        selectedAxis = collisionAxis;
                        bestDistance = distance.Value;
                        selectionPoint = (camera.PickRay.Position + (camera.PickRay.Direction * distance)) - (selected - renderOffset);
                        functionalBlock = funcBlock;
                    }
                }
            }
            return functionalBlock;

        }

        private readonly AutoResetEvent fillResetEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent[] additionalFillResetEvents;
        private readonly AutoResetEvent forceResetEvent = new AutoResetEvent(false);

        public BoundingBox BoundingBoxFromFrustum(BoundingFrustum frustum)
        {
            var viewInvert = Matrix.Invert(frustum.Matrix);
            return CreateTransformedAABB(new BoundingBox(-Vector3.One, Vector3.One), ref viewInvert);
        }

        public unsafe BoundingBox CreateTransformedAABB(BoundingBox box, ref Matrix transformation)
        {
            var corners = stackalloc Vector4[8];
            corners[0] = new Vector4(box.Min.X, box.Min.Y, box.Min.Z, 1.0f);
            corners[1] = new Vector4(box.Max.X, box.Min.Y, box.Min.Z, 1.0f);
            corners[2] = new Vector4(box.Min.X, box.Max.Y, box.Min.Z, 1.0f);
            corners[3] = new Vector4(box.Max.X, box.Max.Y, box.Min.Z, 1.0f);
            corners[4] = new Vector4(box.Min.X, box.Min.Y, box.Max.Z, 1.0f);
            corners[5] = new Vector4(box.Max.X, box.Min.Y, box.Max.Z, 1.0f);
            corners[6] = new Vector4(box.Min.X, box.Max.Y, box.Max.Z, 1.0f);
            corners[7] = new Vector4(box.Max.X, box.Max.Y, box.Max.Z, 1.0f);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var tmp = Vector4.Transform(transformation, corners[i]);
                var perspective = new Vector3(tmp.X / tmp.W, tmp.Y / tmp.W, tmp.Z / tmp.W);
                min = Vector3.Min(perspective, min);
                max = Vector3.Max(perspective, max);
            }

            return new BoundingBox(min, max);
        }
        
        Matrix CalculateCropMatrix(Vector3 light, Vector3 lightDir, BoundingBox[] casters, BoundingBox[] receivers,
            CameraComponent camera)
        {
            var splitFrustum = camera.Frustum;
            
            // Bounding boxes
            BoundingBox receiverBB = default, casterBB = default, splitBB;
            Matrix lightViewProjMatrix = Matrix.CreateOrthographic(ShadowMap.Width, ShadowMap.Height, 0.1f, 100) * Matrix.CreateLookAt(light - lightDir*500, light + lightDir*500, Vector3.UnitX); // ; // TODO: use actual light matrix
            // Merge all bounding boxes of casters into a bigger "casterBB".
            Matrix tmp = lightViewProjMatrix;
            for (int i = 0; i < casters.Length; i++)
            {
                BoundingBox bb = CreateTransformedAABB(casters[i], ref lightViewProjMatrix);
                BoundingBox.CreateMerged(ref casterBB, ref bb, out casterBB);
            }

            // Merge all bounding boxes of receivers into a bigger "receiverBB".
            for (int i = 0; i < receivers.Length; i++)
            {
                var bb = CreateTransformedAABB(receivers[i], ref lightViewProjMatrix);
                BoundingBox.CreateMerged(ref receiverBB, ref bb, out receiverBB);
            }
            // Find the bounding box of the current split

            // in the light's clip space.
            splitBB = CreateTransformedAABB(BoundingBoxFromFrustum(splitFrustum), ref lightViewProjMatrix);
            // Scene-dependent bounding volume
            BoundingBox cropBB = default;

            cropBB.Min.X = Math.Max(Math.Max(casterBB.Min.X, receiverBB.Min.X), splitBB.Min.X);
            cropBB.Max.X = Math.Min(Math.Min(casterBB.Max.X, receiverBB.Max.X), splitBB.Max.X);
            cropBB.Min.Y = Math.Max(Math.Max(casterBB.Min.Y, receiverBB.Min.Y), splitBB.Min.Y);
            cropBB.Max.Y = Math.Min(Math.Min(casterBB.Max.Y, receiverBB.Max.Y), splitBB.Max.Y);
            cropBB.Min.Z = Math.Min(casterBB.Min.Z, splitBB.Min.Z);
            cropBB.Max.Z = Math.Min(receiverBB.Max.Z, splitBB.Max.Z);

            // Create the crop matrix.
            return BoundingBoxToProjection(cropBB) * lightViewProjMatrix;
        }

        private static Matrix BoundingBoxToProjection(BoundingBox cropBB)
        {
            var scaleX = 2.0f / (cropBB.Max.X - cropBB.Min.X);
            var scaleY = 2.0f / (cropBB.Max.Y - cropBB.Min.Y);
            var scaleZ = 2.0f / (cropBB.Max.Z - cropBB.Min.Z);
            var offsetX = -0.5f * (cropBB.Max.X + cropBB.Min.X) * scaleX;
            var offsetY = -0.5f * (cropBB.Max.Y + cropBB.Min.Y) * scaleY;
            var offsetZ = -0.5f * (cropBB.Max.Z + cropBB.Min.Z) * scaleZ;
            return new Matrix(scaleX, 0.0f, 0.0f, 0.0f,
                              0.0f, scaleY, 0.0f, 0.0f,
                              0.0f, 0.0f, scaleZ, 0.0f,
                              offsetX, offsetY, offsetZ, 1.0f);
        }

        public BoundingBox[] GetCasters()
        {
            var chunkOffset = camera.CameraChunk;
            
            var casters = new List<BoundingBox>();
            casters.Add(new BoundingBox(
                new Vector3(
                    (- 4) * Chunk.CHUNKSIZE_X,
                    (- 4) * Chunk.CHUNKSIZE_Y,
                    (-chunkOffset.Z) * Chunk.CHUNKSIZE_Z),
                new Vector3(
                    (+ 4) * Chunk.CHUNKSIZE_X,
                    (+ 4) * Chunk.CHUNKSIZE_Y,
                    (planet.Size.Z - chunkOffset.Z) * Chunk.CHUNKSIZE_Z)));

            for (int i = 0; i < entities.Entities.Count; i++)
            {
                var p = entities.Entities[i].Components.GetComponent<PositionComponent>();

                var offset = p.Position.ChunkIndex.ShortestDistanceXY(chunkOffset, new Index2(planet.Size));
                var viewDist = 1 << VIEWRANGE;
                if (offset.X > viewDist || offset.X < -viewDist || offset.Y > viewDist || offset.Y < -viewDist)
                {
                    continue;
                }
                var pB = entities.Entities[i].Components.GetComponent<BodyComponent>();
                var size = new Vector3(1,1,1);
                if (pB != null)
                {
                    size = new Vector3(pB.Radius, pB.Radius, pB.Height);
                }
                var offsetBlocks = new Vector3(offset.X * Chunk.CHUNKSIZE_X, offset.Y * Chunk.CHUNKSIZE_Y, offset.Z * Chunk.CHUNKSIZE_Z);
                
                casters.Add(new BoundingBox(p.Position.LocalPosition - new Vector3(size.X, size.Y), p.Position.LocalPosition + size));
            }

            return casters.ToArray();
        }

        private Matrix cropMatrix;
        protected override void OnPreDraw(GameTime gameTime)
        {
            if (player?.CurrentEntity == null)
                return;

            if (ControlTexture == null)
                ControlTexture = new RenderTarget2D(Manager.GraphicsDevice, ActualClientArea.Width, ActualClientArea.Height, PixelInternalFormat.Rgb8);


            float octoDaysPerEarthDay = 360f;
            float inclinationVariance = MathHelper.Pi / 3f;

            float playerPosX = player.Position.Position.GlobalPosition.X / (planet.Size.X * Chunk.CHUNKSIZE_X) * MathHelper.TwoPi;
            float playerPosY = player.Position.Position.GlobalPosition.Y / (planet.Size.Y * Chunk.CHUNKSIZE_Y) * MathHelper.TwoPi;

            TimeSpan diff = DateTime.UtcNow - new DateTime(1888, 8, 8);

            float inclination = ((float)Math.Sin(playerPosY) * inclinationVariance) + MathHelper.Pi / 6f;
            sunTransform =
                Matrix.CreateRotationX(inclination) *
                Matrix.CreateRotationY((((float)gameTime.TotalGameTime.TotalMinutes * MathHelper.TwoPi) + playerPosX) * -1); 
                //Matrix.CreateRotationY((float)(MathHelper.TwoPi - ((diff.TotalDays * octoDaysPerEarthDay * MathHelper.TwoPi * 5) % MathHelper.TwoPi)));

            Vector3 sunDirection = Vector3.Transform(sunTransform, new Vector3(0, 0, 1).Normalized());

            cropMatrix = CalculateCropMatrix(player.Position.Position.LocalPosition + (-sunDirection * 10),
                sunDirection * 10, GetCasters(), GetCasters(), camera);
            
            simpleShader.Ambient.DiffuseColor = new Color(190, 190, 190);
            simpleShader.Ambient.DiffuseIntensity = 0.6f;
            simpleShader.Ambient.DiffuseDirection = sunDirection;

            Index3 chunkOffset = camera.CameraChunk;
            Color background = new Color(181, 224, 255);

            if (camera.View == new Matrix())
                return;
            DrawMiniMap(background, chunkOffset);

            DrawShadowMap(background, chunkOffset);

            Manager.GraphicsDevice.SetRenderTarget(ControlTexture);
            Manager.GraphicsDevice.Clear(background);

            Manager.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Manager.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            DrawSun(gameTime, chunkOffset, sunDirection);

            DrawWorld(gameTime, camera.Projection, camera.View, chunkOffset, sunDirection);

            DrawSelectionBox(chunkOffset);

            Manager.GraphicsDevice.Debug.RenderBoundingFrustum(new BoundingFrustum(cropMatrix), camera.Projection, camera.View,
                Matrix.CreateTranslation(
                    chunkOffset.X * Chunk.CHUNKSIZE_X,
                    chunkOffset.Y * Chunk.CHUNKSIZE_Y,
                    chunkOffset.Z * Chunk.CHUNKSIZE_Z));
            
            Manager.GraphicsDevice.SetRenderTarget(null);
        }

        private void DrawShadowMap(Color background, Index3 chunkOffset)
        {
            Manager.GraphicsDevice.SetRenderTarget(ShadowMap);
            Manager.GraphicsDevice.Clear(ClearBufferMask.DepthBufferBit);
            Manager.GraphicsDevice.BlendState = BlendState.Opaque;
            Manager.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            DrawWorldShadow(camera.Projection, camera.View, chunkOffset);

            Manager.GraphicsDevice.SetRenderTarget(null);
        }

        private void DrawMiniMap(Color background, Index3 chunkOffset)
        {
            Manager.GraphicsDevice.SetRenderTarget(MiniMapTexture);
            Manager.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Manager.GraphicsDevice.Clear(background);
            Manager.GraphicsDevice.IndexBuffer = ChunkRenderer.IndexBuffer;
            var viewProj = miniMapProjectionMatrix * camera.MinimapView;

            foreach (var renderer in chunkRenderer)
            {
                if (!renderer.ChunkPosition.HasValue || !renderer.Loaded || !renderer.CanRender)
                    continue;

                Index3 shift = renderer.GetShift(chunkOffset, planet);

                int range = 6;
                if (shift.X >= -range && shift.X <= range &&
                    shift.Y >= -range && shift.Y <= range)
                    renderer.Draw(viewProj, viewProj, shift); // TODO without shadow?
            }
        }
        private int count = 0;
        private void DrawWorld(GameTime gameTime, Matrix projection, Matrix view, Index3 chunkOffset, Vector3 sunDirection)
        {
            if (count < 1)
            {
                Manager.FreeMouse();
                var text = ShadowMap;
                simpleShader.Ambient.ShadowMap = ShadowMap;
                count++;
                Manager.CaptureMouse();
            }
            //
            
            var viewProjC = projection * view;

            Manager.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Manager.GraphicsDevice.BlendState = BlendState.Opaque;
            Manager.GraphicsDevice.IndexBuffer = ChunkRenderer.IndexBuffer;
            DrawChunks(chunkOffset, viewProjC, cropMatrix);
            
            entities.SetLightEnvironment(sunDirection);
            entities.Draw(gameTime, view, projection, chunkOffset, new Index2(planet.Size.X, planet.Size.Z));
        }
        private void DrawWorldShadow(Matrix projection, Matrix view, Index3 chunkOffset)
        {

            Manager.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            Manager.GraphicsDevice.IndexBuffer = ChunkRenderer.IndexBuffer;
            DrawChunksShadow(chunkOffset, cropMatrix);

            entities.DrawShadow(cropMatrix, Matrix.Identity, chunkOffset, new Index2(planet.Size.X, planet.Size.Z));
        }

        private void DrawSun(GameTime gameTime, Index3 chunkOffset, Vector3 sunDirection)
        {
            // Draw Sun
        
            Manager.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            sunEffect.CurrentTechnique.Passes[0].Apply();
            sunEffect.Texture = sunTexture;
            Matrix billboard = Matrix.Invert(camera.View);
            var playerPos = player.Position.Position.LocalPosition;
            billboard.Translation = new Vector3(playerPos.X, playerPos.Y, playerPos.Z) + (sunDirection * -10);

            entities.Draw(gameTime, camera.View, camera.Projection, chunkOffset, new Index2(planet.Size.X, planet.Size.Z));
            //billboard =  Matrix.CreateTranslation(player.Position.Position.LocalPosition + (sunDirection * -10));//Matrix.CreateScaling(new Vector3(100f)));
            sunEffect.World = billboard;
            sunEffect.View = camera.View;
            sunEffect.Projection = camera.Projection;


            Manager.GraphicsDevice.VertexBuffer = billboardVertexbuffer;
            Manager.GraphicsDevice.DrawPrimitives(PrimitiveType.Triangles, 0, 6);
        }

        private void DrawSelectionBox(Index3 chunkOffset)
        {
            if (player.SelectedBox.HasValue)
            {
                // Index3 offset = player.ActorHost.Position.ChunkIndex * Chunk.CHUNKSIZE;
                Index3 offset = camera.CameraChunk * Chunk.CHUNKSIZE;
                Index3 planetSize = planet.Size * Chunk.CHUNKSIZE;
                Index3 relativePosition = new Index3(
                    Index2.ShortestDistanceOnAxis(offset.X, player.SelectedBox.Value.X, planetSize.X),
                    Index2.ShortestDistanceOnAxis(offset.Y, player.SelectedBox.Value.Y, planetSize.Y),
                    player.SelectedBox.Value.Z - offset.Z);

                Vector3 selectedBoxPosition = new Vector3(
                    player.SelectedBox.Value.X - (chunkOffset.X * Chunk.CHUNKSIZE_X),
                    player.SelectedBox.Value.Y - (chunkOffset.Y * Chunk.CHUNKSIZE_Y),
                    player.SelectedBox.Value.Z - (chunkOffset.Z * Chunk.CHUNKSIZE_Z));
                // selectionEffect.World = Matrix.CreateTranslation(selectedBoxPosition);
                selectionEffect.World = Matrix.CreateTranslation(relativePosition);
                selectionEffect.View = camera.View;
                selectionEffect.Projection = camera.Projection;
                Manager.GraphicsDevice.VertexBuffer = selectionLines;
                Manager.GraphicsDevice.IndexBuffer = selectionIndexBuffer;
                foreach (var pass in selectionEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Manager.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.Lines, 0, 0, 8, 0, 12);
                    //Manager.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.Lines, selectionLines, 0, 8, selectionIndeces, 0, 12);
                }
            }
        }


        private void DrawChunks(Index3 chunkOffset, Matrix viewProj, Matrix shadowViewProj)
        {
            var spherePos = camera.PickRay.Position + (camera.PickRay.Direction * sphereRadius);

            foreach (var renderer in chunkRenderer)
            {
                if (!renderer.ChunkPosition.HasValue || !renderer.Loaded || !renderer.CanRender)
                    continue;

                Index3 shift = renderer.GetShift(chunkOffset, planet);

                var chunkPos = new Vector3(
                    (shift.X * Chunk.CHUNKSIZE_X) + (Chunk.CHUNKSIZE_X / 2),
                    (shift.Y * Chunk.CHUNKSIZE_Y) + (Chunk.CHUNKSIZE_Y / 2),
                    (shift.Z * Chunk.CHUNKSIZE_Z) + (Chunk.CHUNKSIZE_Z / 2));

                var frustumDist = spherePos - chunkPos;
                if (frustumDist.LengthSquared < sphereRadiusSquared)
                    renderer.Draw(viewProj, shadowViewProj, shift);
            }
        }
        private void DrawChunksShadow(Index3 chunkOffset, Matrix viewProj)
        {
            //var spherePos = camera.PickRay.Position + (camera.PickRay.Direction * sphereRadius);

            foreach (var renderer in chunkRenderer)
            {
                if (!renderer.ChunkPosition.HasValue || !renderer.Loaded || !renderer.CanRender || renderer.VertexCount == 0)
                    continue;

                Index3 shift = renderer.GetShift(chunkOffset, planet);

                var chunkPos = new Vector3(
                    (shift.X * Chunk.CHUNKSIZE_X) + (Chunk.CHUNKSIZE_X / 2),
                    (shift.Y * Chunk.CHUNKSIZE_Y) + (Chunk.CHUNKSIZE_Y / 2),
                    (shift.Z * Chunk.CHUNKSIZE_Z) + (Chunk.CHUNKSIZE_Z / 2));

                //var frustumDist = spherePos - chunkPos;
                //if (frustumDist.LengthSquared < sphereRadiusSquared)
                    renderer.DrawShadow(viewProj, shift);
            }
        }

        private void FillChunkRenderer()
        {
            if (player?.CurrentEntity == null)
                return;

            Index2 destinationChunk = new Index2(player.Position.Position.ChunkIndex);

            // Nur ausführen wenn der Spieler den Chunk gewechselt hat
            if (destinationChunk != currentChunk)
            {
                localChunkCache.SetCenter(
                    new Index2(player.Position.Position.ChunkIndex),
                    b =>
                    {
                        if (b)
                        {
                            fillResetEvent.Set();
                            OnCenterChanged?.Invoke(this, System.EventArgs.Empty);
                        }
                    });

                for (int x = 0; x < Span; x++)
                {
                    for (int y = 0; y < Span; y++)
                    {
                        Index2 local = new Index2(x - SpanOver2, y - SpanOver2) + destinationChunk;
                        local.NormalizeXY(planet.Size);

                        int virtualX = local.X & Mask;
                        int virtualY = local.Y & Mask;

                        int rendererIndex = virtualX +
                            (virtualY << VIEWRANGE);

                        for (int z = 0; z < planet.Size.Z; z++)
                        {
                            chunkRenderer[rendererIndex, z].SetChunk(localChunkCache, new Index3(local.X, local.Y, z), player.Position.Planet);
                        }
                    }
                }

                Index3 comparationIndex = player.Position.Position.ChunkIndex;
                orderedChunkRenderer.Sort((x, y) =>
                {
                    if (!x.ChunkPosition.HasValue) return 1;
                    if (!y.ChunkPosition.HasValue) return -1;

                    Index3 distX = comparationIndex.ShortestDistanceXYZ(x.ChunkPosition.Value, planet.Size);
                    Index3 distY = comparationIndex.ShortestDistanceXYZ(y.ChunkPosition.Value, planet.Size);
                    return distX.LengthSquared().CompareTo(distY.LengthSquared());
                });

                currentChunk = destinationChunk;
            }

            foreach (var e in additionalFillResetEvents)
                e.Set();

            RegenerateAll(0);
        }

        private void RegenerateAll(int start)
        {
            for (var index = start; index < orderedChunkRenderer.Count; index += _fillIncrement)
            {
                var renderer = orderedChunkRenderer[index];
                if (renderer.NeedsUpdate)
                {
                    renderer.RegenerateVertexBuffer();
                }
            }
        }

        private void BackgroundLoop(object state)
        {
            var token = state is CancellationToken stateToken ? stateToken : CancellationToken.None;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                fillResetEvent.WaitOne();
                FillChunkRenderer();
            }
        }

        private void AdditionalFillerBackgroundLoop(object state)
        {
            var (@event, n, token) = ((AutoResetEvent Event, int N, CancellationToken Token))state;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                @event.WaitOne();
                RegenerateAll(n + 1);
            }
        }

        private void ForceUpdateBackgroundLoop(object state)
        {
            var token = state is CancellationToken stateToken ? stateToken : CancellationToken.None;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                forceResetEvent.WaitOne();

                while (!forcedRenders.IsEmpty)
                {
                    while (forcedRenders.TryDequeue(out ChunkRenderer r))
                    {
                        r.RegenerateVertexBuffer();
                    }
                }
            }
        }

        #region Converter

        private static OrientationFlags FindEdge(Vector2 point, OrientationFlags upper, OrientationFlags lower, OrientationFlags left, OrientationFlags right)
        {
            if (point.X > point.Y)
            {
                if (1f - point.X > point.Y) return upper;
                else return right;
            }
            else
            {
                if (1f - point.X > point.Y) return left;
                else return lower;
            }
        }

        private static OrientationFlags FindCorner(Vector2 point, OrientationFlags upperLeftCorner, OrientationFlags upperRightCorner, OrientationFlags lowerLeftCorner, OrientationFlags lowerRightCorner)
        {
            if (point.X < 0.5f)
            {
                if (point.Y < 0.5f) return upperLeftCorner;
                else return lowerLeftCorner;
            }
            else
            {
                if (point.Y < 0.5f) return upperRightCorner;
                else return lowerRightCorner;
            }
        }

        #endregion

        private readonly ConcurrentQueue<ChunkRenderer> forcedRenders = new ConcurrentQueue<ChunkRenderer>();
        private bool disposed;

        public void Enqueue(ChunkRenderer chunkRenderer1)
        {
            forcedRenders.Enqueue(chunkRenderer1);
            forceResetEvent.Set();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            foreach (var cr in chunkRenderer)
                cr.Dispose();

            foreach (var cr in orderedChunkRenderer)
                cr.Dispose();

            foreach (var renderer in chunkRenderer)
                renderer.SetChunk(null, null, null);

            chunkRenderer = null;
            orderedChunkRenderer.Clear();

            localChunkCache = null;

            selectionIndexBuffer.Dispose();
            selectionLines.Dispose();
            billboardVertexbuffer.Dispose();

            player = null;
            camera = null;
            assets = null;
            entities = null;
            planet = null;

            sunEffect.Dispose();
            selectionEffect.Dispose();

            blockTextures.Dispose();
            sunTexture.Dispose();
        }

        private Index3? CheckPosition(int x, int y, int z)
        {
            Index3 pos = new Index3(x, y, z);
            ushort block = localChunkCache.GetBlock(pos);
            return block != 0 ? pos : null;
        }

        private Index3? PythonBresenham(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int dz = Math.Abs(z1 - z0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int sz = z0 < z1 ? 1 : -1;

            if (dx >= dy && dx >= dz)
            {
                var p1 = 2 * dy - dx;
                var p2 = 2 * dz - dx;

                while (x0 != x1)
                {
                    x0 += sx;
                    if (p1 >= 0)
                    {
                        y0 += sy;
                        p1 -= 2 * dx;
                    }
                    if (p2 >= 0)
                    {
                        z0 += sz;
                        p2 -= 2 * dx;
                    }
                    p1 += 2 * dy;
                    p2 += 2 * dz;

                    var pos = CheckPosition(x0, y0, z0);

                    if (pos.HasValue)
                        return pos;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                var p1 = 2 * dx - dy;
                var p2 = 2 * dz - dy;
                while (y0 != y1)
                {
                    y0 += sy;
                    if (p1 >= 0)
                    {
                        x0 += sx;
                        p1 -= 2 * dy;
                    }
                    if (p2 >= 0)
                    {
                        z0 += sz;
                        p2 -= 2 * dy;
                    }
                    p1 += 2 * dx;
                    p2 += 2 * dz;

                    var pos = CheckPosition(x0, y0, z0);

                    if (pos.HasValue)
                        return pos;
                }
            }
            else
            {
                var p1 = 2 * dy - dz;
                var p2 = 2 * dx - dz;

                while (z0 != z1)
                {
                    z0 += sz;
                    if (p1 >= 0)
                    {
                        y0 += sy;
                        p1 -= 2 * dz;
                    }
                    if (p2 >= 0)
                    {
                        x0 += sx;
                        p2 -= 2 * dz;
                    }
                    p1 += 2 * dy;
                    p2 += 2 * dx;

                    var pos = CheckPosition(x0, y0, z0);

                    if (pos.HasValue)
                        return pos;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x0">StartPosition X</param>
        /// <param name="y0">StartPosition Y</param>
        /// <param name="z0">StartPosition Z</param>
        /// <param name="x1">EndPosition X</param>
        /// <param name="y1">EndPosition Y</param>
        /// <param name="z1">EndPosition Z</param>
        private Index3? SimpleBresenham(int x0, int y0, int z0, int x1, int y1, int z1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int dz = Math.Abs(z1 - z0), sz = z0 < z1 ? 1 : -1;
            int err = dx + dy + dz, e2; /* error value e_xy */

            while (true)
            {
                Index3 pos = new Index3(x0, y0, z0);
                ushort block = localChunkCache.GetBlock(pos);
                var isBlock = block != 0;

                if (isBlock)
                    return pos;

                if (x0 == x1 && y0 == y1 && z0 == z1)
                    return null;

                e2 = 2 * err;
                if (e2 > dy)
                {
                    err += dy;
                    x0 += sx;
                } /* e_xy+e_x > 0 */
                if (e2 > dx)
                {
                    err += dx;
                    y0 += sy;
                } /* e_xy+e_y < 0 */
                if (e2 > dz)
                {
                    err += dz;
                    z0 += sz;
                }
            }
        }
    }
}
