using OctoAwesome.Client.Controls;
using System.Collections.Generic;
using engenious.Graphics;
using System.Linq;
using engenious;
using System;
using System.Threading;
using OctoAwesome.Threading;
using OctoAwesome.Client.Cache;
using OctoAwesome.Expressions;
using OctoAwesome.Runtime;
using System.IO;
using OctoAwesome.Serialization;
using System.Collections.ObjectModel;
using OctoAwesome.Definitions;
using engenious.Helper;
using engenious.UserDefined.Shaders;

namespace OctoAwesome.Client.Components
{
    internal sealed class ChunkRenderer : IDisposable
    {
        public VertexBuffer VertexBuffer { get; private set; }
        public static IndexBuffer IndexBuffer { get; private set; }
        public static float OverrideLightLevel { get; set; }
        public static bool WireFrame { get; set; }

        private readonly chunkEffect simple;
        private readonly GraphicsDevice graphicsDevice;

        private readonly Texture2DArray textures;



        private static readonly RasterizerState wireFrameState;
        private static readonly Vector2[] uvOffsets;

        /// <summary>
        /// Referenz auf den aktuellen Chunk (falls vorhanden)
        /// </summary>
        private IChunk centerChunk;
        private readonly IChunk[] chunks;

        private readonly IBlockDefinition[] blockDefinitions;
        private IPlanet planet;
        public bool Loaded { get; set; } = false;

        public int VertexCount { get; private set; }
        private int indexCount;
        private ILocalChunkCache _manager;
        private Index3 _shift;
        private Index3 _cameraPos;

        private Index3? _chunkPosition;
        private readonly SceneControl _sceneControl;
        private readonly IDefinitionManager definitionManager;
        private readonly LockSemaphore semaphore = new(1, 1);
        private readonly object ibLock = new();
        private readonly Dictionary<IBlockDefinition, int> textureOffsets;
        private readonly List<VertexPositionNormalTextureLight> vertices;
        /// <summary>
        /// Adresse des aktuellen Chunks
        /// </summary>
        public Index3? ChunkPosition
        {
            get
            {
                return _chunkPosition;
            }
            private set
            {
                _chunkPosition = value;
                NeedsUpdate = value != null;
            }
        }


        static ChunkRenderer()
        {
            wireFrameState = new RasterizerState() { FillMode = PolygonMode.Line, CullMode = CullMode.CounterClockwise };
            OverrideLightLevel = 0;
            WireFrame = false;
            uvOffsets = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1)
                };
        }

        public ChunkRenderer(SceneControl sceneControl, IDefinitionManager definitionManager, chunkEffect simpleShader, GraphicsDevice graphicsDevice, Matrix projection, Texture2DArray textures)
        {
            _sceneControl = sceneControl;
            this.definitionManager = definitionManager;
            this.graphicsDevice = graphicsDevice;
            this.textures = textures;
            simple = simpleShader;
            GenerateIndexBuffer();

            vertices = new List<VertexPositionNormalTextureLight>();
            // BlockTypes sammlen
            var localBlockDefinitions = definitionManager.BlockDefinitions;
            textureOffsets = new Dictionary<IBlockDefinition, int>(localBlockDefinitions.Length);
            // Dictionary<Type, BlockDefinition> definitionMapping = new Dictionary<Type, BlockDefinition>();
            int definitionIndex = 0;
            foreach (var definition in localBlockDefinitions)
            {
                int textureCount = definition.Textures.Length;
                textureOffsets.Add(definition, definitionIndex);
                // definitionMapping.Add(definition.GetBlockType(), definition);
                definitionIndex += textureCount;
            }
            chunks = new IChunk[27];
            blockDefinitions = new IBlockDefinition[27];

            //simple.Ambient.Pass1.Apply();
            //simple.Ambient.BlockTextures = textures;
            //simple.Ambient.AmbientIntensity = 0.4f;
            //simple.Ambient.AmbientColor = Color.White.ToVector4();

            //dbProvier = new DatabaseProvider(Path.Combine("cache", "chunkverticescache"), null);


        }

        public Index3 GetShift(Index3 chunkOffset, IPlanet planet)
        {
            if (chunkOffset == _cameraPos)
                return _shift;

            _shift = chunkOffset.ShortestDistanceXY(
                _chunkPosition!.Value, new Index2(
                    planet.Size.X,
                    planet.Size.Y));
            _cameraPos = chunkOffset;
            return _shift;
        }

        public void SetChunk(ILocalChunkCache manager, Index3? newPosition, IPlanet planet)
        {
            if (_manager == manager && newPosition == ChunkPosition)
            {
                NeedsUpdate = !Loaded;
                return;
            }

            _manager = manager;
            ChunkPosition = newPosition;

            if (centerChunk != null)
            {
                //CacheCurrentChunkVerticesData();

                centerChunk.Changed -= OnChunkChanged;
                centerChunk = null;
            }
            this.planet = planet;

            Loaded = false;
            NeedsUpdate = true;
        }

        public bool NeedsUpdate = false;


        private void OnChunkChanged(IChunk c)
        {
            NeedsUpdate = true;
            _sceneControl.Enqueue(this);
        }

        public void Draw(Matrix viewProj, Index3 shift)
        {
            if (!Loaded)
                return;

            Matrix worldViewProj = viewProj * Matrix.CreateTranslation(
                shift.X * Chunk.CHUNKSIZE_X,
                shift.Y * Chunk.CHUNKSIZE_Y,
                shift.Z * Chunk.CHUNKSIZE_Z);
            
            simple.Ambient.MainPass.Apply();

            simple.Ambient.OverrideLightLevel = OverrideLightLevel;
            simple.Ambient.WorldViewProj = worldViewProj;
            simple.Ambient.BlockTextures = textures;

            simple.Ambient.AmbientIntensity = 0.4f;
            simple.Ambient.AmbientColor = Color.White.ToVector4();

            lock (this)
            {
                if (VertexBuffer == null)
                    return;

                graphicsDevice.RasterizerState = WireFrame ? wireFrameState : RasterizerState.CullCounterClockwise;
                graphicsDevice.VertexBuffer = VertexBuffer;

                foreach (var pass in simple.Ambient.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.Triangles, 0, 0, VertexCount, 0, indexCount / 3);
                }
            }
        }
        public void DrawShadow(Matrix viewProj, Index3 shift)
        {
            if (!Loaded)
                return;

            Matrix worldViewProj = viewProj * Matrix.CreateTranslation(
                shift.X * Chunk.CHUNKSIZE_X,
                shift.Y * Chunk.CHUNKSIZE_Y,
                shift.Z * Chunk.CHUNKSIZE_Z);

            simple.Shadow.MainPass.Apply();
            simple.Shadow.DepthWorldViewProj = worldViewProj;

            lock (this)
            {
                if (VertexBuffer == null)
                    return;

                graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                graphicsDevice.VertexBuffer = VertexBuffer;

                foreach (var pass in simple.Shadow.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.Triangles, 0, 0, VertexCount, 0, indexCount / 3);
                }
            }
        }

        public void GenerateIndexBuffer()
        {
            lock (ibLock)
            {
                if (IndexBuffer != null)
                    return;

                IndexBuffer = new IndexBuffer(graphicsDevice, DrawElementsType.UnsignedInt, Chunk.CHUNKSIZE_X * Chunk.CHUNKSIZE_Y * Chunk.CHUNKSIZE_Z * 6 * 6);
                List<int> indices = new List<int>(IndexBuffer.IndexCount);
                for (int i = 0; i < IndexBuffer.IndexCount * 2 / 3; i += 4)
                {
                    indices.Add(i + 0);
                    indices.Add(i + 1);
                    indices.Add(i + 3);

                    indices.Add(i + 0);
                    indices.Add(i + 3);
                    indices.Add(i + 2);
                }
                IndexBuffer.SetData(indices.ToArray());
            }

        }
#if DEBUG
        public bool RegenerateVertexBuffer()
#else
        public unsafe bool RegenerateVertexBuffer()
#endif
        {
            if (!ChunkPosition.HasValue)
                return false;
            using (semaphore.Wait())
            {
                IChunk chunk = centerChunk;
                // Chunk nachladen
                if (chunk == null)
                {
                    chunk = _manager.GetChunk(ChunkPosition.Value);
                    if (chunk == null)
                    {
                        return false;
                    }

                    centerChunk = chunk;
                    centerChunk.Changed += OnChunkChanged;
                }
                vertices.Clear();

                var chunkPos = ChunkPosition.Value;
                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                        for (int z = -1; z < 2; z++)
                        {
                            if (x == 0 && y == 0 && z == 0)
                            {
                                chunks[13] = chunk;
                                continue;
                            }
                            Index3 chunkOffset = new Index3(x + chunkPos.X, y + chunkPos.Y, chunkPos.Z + z);
                            chunks[GetIndex(z, y, x)] = _manager.GetChunk(chunkOffset);
                        }

                for (int z = Chunk.CHUNKSIZE_Z - 1; z >= 0; z -= Chunk.CHUNKSIZE.Z - 1)
                {
                    for (int y = 0; y < Chunk.CHUNKSIZE_Y; y++)
                    {
                        for (int x = 0; x < Chunk.CHUNKSIZE_X; x++)
                        {
                            GenerateVertices(chunk, chunks, uvOffsets, x, y, z, chunkPos, blockDefinitions, true);
                        }
                    }
                }

                for (int z = 0; z < Chunk.CHUNKSIZE_Z; z++)
                {
                    for (int y = Chunk.CHUNKSIZE_Y - 1; y >= 0; y -= Chunk.CHUNKSIZE.Y - 1)
                    {
                        for (int x = 0; x < Chunk.CHUNKSIZE_X; x++)
                        {
                            GenerateVertices(chunk, chunks, uvOffsets, x, y, z, chunkPos, blockDefinitions, true);

                        }
                    }
                }

                for (int z = 0; z < Chunk.CHUNKSIZE_Z; z++)
                {
                    for (int y = 0; y < Chunk.CHUNKSIZE_Y; y++)
                    {
                        for (int x = Chunk.CHUNKSIZE_X - 1; x >= 0; x -= Chunk.CHUNKSIZE.X - 1)
                        {
                            GenerateVertices(chunk, chunks, uvOffsets, x, y, z, chunkPos, blockDefinitions, true);
                        }
                    }
                }

                for (int z = 1; z < Chunk.CHUNKSIZE_Z - 1; z++)
                {
                    for (int y = 1; y < Chunk.CHUNKSIZE_Y - 1; y++)
                    {
                        for (int x = 1; x < Chunk.CHUNKSIZE_X - 1; x++)
                        {
                            GenerateVertices(chunk, chunks, uvOffsets, x, y, z, chunkPos, blockDefinitions, true);
                        }
                    }
                }

                return RegisterNewVertices(chunk);
            }
        }
#if DEBUG
        private bool RegisterNewVertices(IChunk chunk)
#else
        private unsafe bool RegisterNewVertices(IChunk chunk)
#endif
        {
            VertexCount = vertices.Count;
            indexCount = vertices.Count * 6 / 4;

            if (VertexCount > 0)
            {
                ThreadingHelper.OnUiThread((t) =>
                {
                    if (VertexBuffer == null || IndexBuffer == null)
                    {
                        VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTextureLight.VertexDeclaration, VertexCount);
                    }
                    if (VertexCount > VertexBuffer.VertexCount)
                        VertexBuffer.Resize(VertexCount);


                    VertexBuffer.SetData(vertices.ToArray());
                }, null);
            }

            lock (this)
            {
                if (chunk != null && chunk.Index != ChunkPosition)
                {
                    return Loaded;
                }

                Loaded = true;
                NeedsUpdate |= chunk != centerChunk;
                return !NeedsUpdate;
            }
        }
#if DEBUG
        private void GenerateVertices(IChunk centerChunk, IChunk[] chunks, Vector2[] uvOffsets, int z, int y, int x, Index3 chunkPosition, IBlockDefinition[] blockDefinitions, bool getFromManager)
#else
        private unsafe void GenerateVertices(IChunk chunk, IChunk[] chunks, int x, int y, int z, IBlockDefinition[] blockDefinitions, bool getFromManager)
#endif
        {
            ushort block = centerChunk.GetBlock(x, y, z);

            var manager = _manager;

            if (block == 0 || manager is null)
                return;


            IBlockDefinition blockDefinition = (IBlockDefinition)definitionManager.GetBlockDefinitionByIndex(block);

            if (blockDefinition is null)
                return;

            int textureIndex;
            if (!textureOffsets.TryGetValue(blockDefinition, out textureIndex))
                return;

            if (vertices.Count == 0)
                vertices.Capacity = 4096;

#if DEBUG
            var blocks = new ushort[27];
#else
            var blocks = stackalloc ushort[27];
#endif
            ushort topBlock, bottomBlock, southBlock, northBlock, westBlock, eastBlock;
            if (getFromManager)
            {

                IChunk chunk;
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            chunk = chunks[GetIndex(IsBorder(z) * OutsiteOfChunkBorderInDirection(z, zOffset), IsBorder(y) * OutsiteOfChunkBorderInDirection(y, yOffset), IsBorder(x) * OutsiteOfChunkBorderInDirection(x, xOffset))];
                            blocks[GetIndex(zOffset, yOffset, xOffset)] = manager.GetBlock((chunkPosition * Chunk.CHUNKSIZE) + new Index3(x + xOffset, y + yOffset, z + zOffset));
                        }
            }
            else
            {
                for (int zOffset = -1; zOffset <= 1; zOffset++)
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                        for (int xOffset = -1; xOffset <= 1; xOffset++)
                        {
                            blocks[GetIndex(zOffset, yOffset, xOffset)] = centerChunk.Blocks[Chunk.GetFlatIndex((chunkPosition * Chunk.CHUNKSIZE) + new Index3(x + xOffset, y + yOffset, z + zOffset))];
                        }
            }



            topBlock = blocks[(((2 * 3) + 1) * 3) + 1];
            bottomBlock = blocks[(((0 * 3) + 1) * 3) + 1];
            southBlock = blocks[(((1 * 3) + 2) * 3) + 1];
            northBlock = blocks[(((1 * 3) + 0) * 3) + 1];
            westBlock = blocks[(((1 * 3) + 1) * 3) + 0];
            eastBlock = blocks[(((1 * 3) + 1) * 3) + 2];

            for (int zOffset = -1; zOffset <= 1; zOffset++)
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                    for (int xOffset = -1; xOffset <= 1; xOffset++)
                    {
                        blockDefinitions[GetIndex(zOffset, yOffset, xOffset)] =
                            definitionManager.GetBlockDefinitionByIndex(blocks[GetIndex(zOffset, yOffset, xOffset)]);
                    }

            IBlockDefinition topBlockDefintion = blockDefinitions[(((2 * 3) + 1) * 3) + 1];
            IBlockDefinition bottomBlockDefintion = blockDefinitions[(((0 * 3) + 1) * 3) + 1];
            IBlockDefinition southBlockDefintion = blockDefinitions[(((1 * 3) + 2) * 3) + 1];
            IBlockDefinition northBlockDefintion = blockDefinitions[(((1 * 3) + 0) * 3) + 1];
            IBlockDefinition westBlockDefintion = blockDefinitions[(((1 * 3) + 1) * 3) + 0];
            IBlockDefinition eastBlockDefintion = blockDefinitions[(((1 * 3) + 1) * 3) + 2];
            var globalX = x + centerChunk.Index.X * Chunk.CHUNKSIZE_X;
            var globalY = y + centerChunk.Index.Y * Chunk.CHUNKSIZE_Y;
            var globalZ = z + centerChunk.Index.Z * Chunk.CHUNKSIZE_Z;

            // Top
            if (topBlock == 0 || (!topBlockDefintion.IsSolidWall(Wall.Bottom) && topBlock != block))
            {
                var top = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Top, _manager, globalX, globalY, globalZ));
                int rotation = -blockDefinition.GetTextureRotation(Wall.Top, _manager, globalX, globalY, globalZ);

                var valueYZ = VertexAO(blockDefinitions, GetIndex(1, 1, -1), GetIndex(1, 0, -1), Wall.Left, GetIndex(1, 1, 0), Wall.Front);
                var valueXYZ = VertexAO(blockDefinitions, GetIndex(1, 1, 1), GetIndex(1, 0, 1), Wall.Left, GetIndex(1, 1, 0), Wall.Front);
                var valueZ = VertexAO(blockDefinitions, GetIndex(1, -1, -1), GetIndex(1, 0, -1), Wall.Left, GetIndex(1, -1, 0), Wall.Front);
                var valueXZ = VertexAO(blockDefinitions, GetIndex(1, -1, 1), GetIndex(1, 0, 1), Wall.Left, GetIndex(1, -1, 0), Wall.Front);

                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1),
                        new Vector3(0, 0, 1),
                        uvOffsets[(6 + rotation) % 4],
                        top,
                        AmbientToBrightness(valueYZ));
                var vertXYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 1),
                        new Vector3(0, 0, 1),
                        uvOffsets[(7 + rotation) % 4],
                        top,
                        AmbientToBrightness(valueXYZ));
                var vertZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 1),
                        new Vector3(0, 0, 1),
                        uvOffsets[(5 + rotation) % 4],
                        top,
                        AmbientToBrightness(valueZ));
                var vertXZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 1),
                        new Vector3(0, 0, 1),
                        uvOffsets[(4 + rotation) % 4],
                        top,
                        AmbientToBrightness(valueXZ));

                if (valueXYZ + valueZ <= valueYZ + valueXZ)
                {
                    vertices.Add(vertYZ);
                    vertices.Add(vertXYZ);
                    vertices.Add(vertZ);
                    vertices.Add(vertXZ);
                }
                else
                {
                    vertices.Add(vertXYZ);
                    vertices.Add(vertXZ);
                    vertices.Add(vertYZ);
                    vertices.Add(vertZ);
                }
            }


            // Unten
            if (bottomBlock == 0 || (!bottomBlockDefintion.IsSolidWall(Wall.Top) && bottomBlock != block))
            {
                var bottom = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Bottom, _manager, globalX, globalY, globalZ));
                var valueY = VertexAO(blockDefinitions, GetIndex(-1, 1, -1), GetIndex(-1, 0, -1), Wall.Left, GetIndex(-1, 1, 0), Wall.Front);
                var valueXY = VertexAO(blockDefinitions, GetIndex(-1, 1, 1), GetIndex(-1, 0, 1), Wall.Left, GetIndex(-1, 1, 0), Wall.Front);
                var value = VertexAO(blockDefinitions, GetIndex(-1, -1, -1), GetIndex(-1, 0, -1), Wall.Left, GetIndex(-1, -1, 0), Wall.Front);
                var valueX = VertexAO(blockDefinitions, GetIndex(-1, -1, 1), GetIndex(-1, 0, 1), Wall.Left, GetIndex(-1, -1, 0), Wall.Front);

                int rotation = -blockDefinition.GetTextureRotation(Wall.Bottom, _manager, globalX, globalY, globalZ);

                var vertXY = new VertexPositionNormalTextureLight(
          new Vector3(x + 1, y + 1, z + 0), new Vector3(0, 0, -1), uvOffsets[(6 + rotation) % 4], bottom, AmbientToBrightness(valueXY));
                var vertY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 0), new Vector3(0, 0, -1), uvOffsets[(7 + rotation) % 4], bottom, AmbientToBrightness(valueY));
                var vertX = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 0), new Vector3(0, 0, -1), uvOffsets[(5 + rotation) % 4], bottom, AmbientToBrightness(valueX));
                var vert = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 0), new Vector3(0, 0, -1), uvOffsets[(4 + rotation) % 4], bottom, AmbientToBrightness(value));

                if (value + valueXY <= valueY + valueX)
                {
                    vertices.Add(vertY);
                    vertices.Add(vert);
                    vertices.Add(vertXY);
                    vertices.Add(vertX);
                }
                else
                {
                    vertices.Add(vertXY);
                    vertices.Add(vertY);
                    vertices.Add(vertX);
                    vertices.Add(vert);
                }
            }


            // South
            if (southBlock == 0 || (!southBlockDefintion.IsSolidWall(Wall.Front) && southBlock != block))
            {
                var front = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Front, _manager, globalX, globalY, globalZ));
                int rotation = -blockDefinition.GetTextureRotation(Wall.Front, _manager, globalX, globalY, globalZ);

                var valueY = VertexAO(blockDefinitions, GetIndex(-1, 1, -1), GetIndex(0, 1, -1), Wall.Right, GetIndex(-1, 1, 0), Wall.Front);
                var valueXY = VertexAO(blockDefinitions, GetIndex(-1, 1, 1), GetIndex(0, 1, 1), Wall.Left, GetIndex(-1, 1, 0), Wall.Front);
                var valueYZ = VertexAO(blockDefinitions, GetIndex(1, 1, -1), GetIndex(0, 1, -1), Wall.Right, GetIndex(1, 1, 0), Wall.Back);
                var valueXYZ = VertexAO(blockDefinitions, GetIndex(1, 1, 1), GetIndex(1, 1, 0), Wall.Left, GetIndex(0, 1, 1), Wall.Back);

                var vertY = new VertexPositionNormalTextureLight(
             new Vector3(x + 0, y + 1, z + 0), new Vector3(0, 1, 0), uvOffsets[(6 + rotation) % 4], front, AmbientToBrightness(valueY));
                var vertXY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 0), new Vector3(0, 1, 0), uvOffsets[(7 + rotation) % 4], front, AmbientToBrightness(valueXY));
                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1), new Vector3(0, 1, 0), uvOffsets[(5 + rotation) % 4], front, AmbientToBrightness(valueYZ));
                var vertXYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 1), new Vector3(0, 1, 0), uvOffsets[(4 + rotation) % 4], front, AmbientToBrightness(valueXYZ));


                if (valueY + valueXYZ >= valueYZ + valueXY)
                {
                    vertices.Add(vertY);
                    vertices.Add(vertXY);
                    vertices.Add(vertYZ);
                    vertices.Add(vertXYZ);
                }
                else
                {
                    vertices.Add(vertXY);
                    vertices.Add(vertXYZ);
                    vertices.Add(vertY);
                    vertices.Add(vertYZ);
                }
            }



            // North
            if (northBlock == 0 || (!northBlockDefintion.IsSolidWall(Wall.Back) && northBlock != block))
            {
                var back = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Back, _manager, globalX, globalY, globalZ));
                int rotation = -blockDefinition.GetTextureRotation(Wall.Back, _manager, globalX, globalY, globalZ);
                var value = VertexAO(blockDefinitions, GetIndex(-1, -1, -1), GetIndex(0, -1, -1), Wall.Right, GetIndex(-1, -1, 0), Wall.Front);
                var valueX = VertexAO(blockDefinitions, GetIndex(-1, -1, 1), GetIndex(0, -1, 1), Wall.Left, GetIndex(-1, -1, 0), Wall.Front);
                var valueZ = VertexAO(blockDefinitions, GetIndex(1, -1, -1), GetIndex(0, -1, -1), Wall.Right, GetIndex(1, -1, 0), Wall.Back);
                var valueXZ = VertexAO(blockDefinitions, GetIndex(1, -1, 1), GetIndex(1, -1, 0), Wall.Left, GetIndex(0, -1, 1), Wall.Back);

                var vertZ = new VertexPositionNormalTextureLight(
            new Vector3(x + 0, y + 0, z + 1), new Vector3(0, -1, 0), uvOffsets[(4 + rotation) % 4], back, AmbientToBrightness(valueZ));
                var vertXZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 1), new Vector3(0, -1, 0), uvOffsets[(5 + rotation) % 4], back, AmbientToBrightness(valueXZ));
                var vert = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 0), new Vector3(0, -1, 0), uvOffsets[(7 + rotation) % 4], back, AmbientToBrightness(value));
                var vertX = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 0), new Vector3(0, -1, 0), uvOffsets[(6 + rotation) % 4], back, AmbientToBrightness(valueX));

                if (value + valueXZ <= valueZ + valueX)
                {
                    vertices.Add(vertZ);
                    vertices.Add(vertXZ);
                    vertices.Add(vert);
                    vertices.Add(vertX);
                }
                else
                {
                    vertices.Add(vertXZ);
                    vertices.Add(vertX);
                    vertices.Add(vertZ);
                    vertices.Add(vert);
                }
            }


            // West
            if (westBlock == 0 || (!westBlockDefintion.IsSolidWall(Wall.Right) && westBlock != block))
            {
                var left = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Left, _manager, globalX, globalY, globalZ));
                int rotation = -blockDefinition.GetTextureRotation(Wall.Left, _manager, globalX, globalY, globalZ);

                var valueY = VertexAO(blockDefinitions, GetIndex(-1, 1, -1), GetIndex(0, 1, -1), Wall.Left, GetIndex(-1, 0, -1), Wall.Front);
                var valueYZ = VertexAO(blockDefinitions, GetIndex(1, 1, -1), GetIndex(1, 0, -1), Wall.Left, GetIndex(0, 1, -1), Wall.Back);
                var value = VertexAO(blockDefinitions, GetIndex(-1, -1, -1), GetIndex(0, -1, -1), Wall.Right, GetIndex(-1, 0, -1), Wall.Front);
                var valueZ = VertexAO(blockDefinitions, GetIndex(1, -1, -1), GetIndex(0, -1, -1), Wall.Right, GetIndex(1, 0, -1), Wall.Back);

                var vertY = new VertexPositionNormalTextureLight(
           new Vector3(x + 0, y + 1, z + 0), new Vector3(-1, 0, 0), uvOffsets[(7 + rotation) % 4], left, AmbientToBrightness(valueY));
                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1), new Vector3(-1, 0, 0), uvOffsets[(4 + rotation) % 4], left, AmbientToBrightness(valueYZ));
                var vert = new VertexPositionNormalTextureLight(
                       new Vector3(x + 0, y + 0, z + 0), new Vector3(-1, 0, 0), uvOffsets[(6 + rotation) % 4], left, AmbientToBrightness(value));
                var vertZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 1), new Vector3(-1, 0, 0), uvOffsets[(5 + rotation) % 4], left, AmbientToBrightness(valueZ));

                if (value + valueYZ <= valueZ + valueY)
                {
                    vertices.Add(vertY);
                    vertices.Add(vertYZ);
                    vertices.Add(vert);
                    vertices.Add(vertZ);
                }
                else
                {
                    vertices.Add(vertYZ);
                    vertices.Add(vertZ);
                    vertices.Add(vertY);
                    vertices.Add(vert);
                }
            }


            // Ost
            if (eastBlock == 0 || (!eastBlockDefintion.IsSolidWall(Wall.Left) && eastBlock != block))
            {
                var right = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Right, _manager, globalX, globalY, globalZ));
                var valueXY = VertexAO(blockDefinitions, GetIndex(-1, 1, 1), GetIndex(0, 1, 1), Wall.Left, GetIndex(-1, 0, 1), Wall.Front);
                var valueXYZ = VertexAO(blockDefinitions, GetIndex(1, 1, 1), GetIndex(1, 0, 1), Wall.Left, GetIndex(0, 1, 1), Wall.Back);
                var valueX = VertexAO(blockDefinitions, GetIndex(-1, -1, 1), GetIndex(0, -1, 1), Wall.Right, GetIndex(-1, 0, 1), Wall.Front);
                var valueXZ = VertexAO(blockDefinitions, GetIndex(1, -1, 1), GetIndex(0, -1, 1), Wall.Right, GetIndex(1, 0, 1), Wall.Back);

                int rotation = -blockDefinition.GetTextureRotation(Wall.Right, _manager, globalX, globalY, globalZ);

                var vertXYZ = new VertexPositionNormalTextureLight(
         new Vector3(x + 1, y + 1, z + 1), new Vector3(1, 0, 0), uvOffsets[(5 + rotation) % 4], right, AmbientToBrightness(valueXYZ));
                var vertXY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 0), new Vector3(1, 0, 0), uvOffsets[(6 + rotation) % 4], right, AmbientToBrightness(valueXY));
                var vertXZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 1), new Vector3(1, 0, 0), uvOffsets[(4 + rotation) % 4], right, AmbientToBrightness(valueXZ));
                var vertX = new VertexPositionNormalTextureLight(
                       new Vector3(x + 1, y + 0, z + 0), new Vector3(1, 0, 0), uvOffsets[(7 + rotation) % 4], right, AmbientToBrightness(valueX));

                if (valueX + valueXYZ >= valueXZ + valueXY)
                {
                    vertices.Add(vertXYZ);
                    vertices.Add(vertXY);
                    vertices.Add(vertXZ);
                    vertices.Add(vertX);
                }
                else
                {
                    vertices.Add(vertXY);
                    vertices.Add(vertX);
                    vertices.Add(vertXYZ);
                    vertices.Add(vertXZ);
                }
            }
        }
        private static int VertexAO(int side1, int side2, int corner)
            => ((side1 & side2) ^ 1) * (3 - (side1 + side2 + corner));

        private uint AmbientToBrightness(uint ambient)
            => (0xFFFFFF / 2) + (0xFFFFFF / 6 * ambient);

        private static /*unsafe */uint VertexAO(IBlockDefinition[] blockDefinitions, int cornerIndex, int side1Index, Wall side1Wall, int side2Index, Wall side2Wall)
        {
            var cornerBlock = blockDefinitions[cornerIndex]?.SolidWall ?? 0;
            var side1Def = blockDefinitions[side1Index];
            var side2Def = blockDefinitions[side2Index];
            var side1 = IsSolidWall(side1Wall, side1Def?.SolidWall ?? 0);
            var side2 = IsSolidWall(side2Wall, side2Def?.SolidWall ?? 0);

            return (uint)VertexAO(side1, side2, cornerBlock == 0 ? 0 : 1);
        }

        private static int GetIndex(int zOffset, int yOffset, int xOffset)
            => ((((zOffset + 1) * 3) + yOffset + 1) * 3) + xOffset + 1;

        private static int IsSolidWall(Wall wall, uint solidWall)
            => ((int)solidWall >> (int)wall) & 1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <returns>0 when not border, 0 becomes -1 and 15 becomes 1</returns>
        private static unsafe int IsBorder(int z)
        {
            var tmp = (uint)(z % (Chunk.CHUNKSIZE_X - 1));
            return (int)~((tmp | (~tmp + 1)) >> 31) & 1;
        }

        /// <summary>
        /// Determines wether the position is inside the chunk borders or outsite and if its in the neagtiv or positiv direction outside the chunk
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int OutsiteOfChunkBorderInDirection(int pos, int offset)
        {
            return ((pos + offset) >> 31) + (((pos + offset) >> Chunk.LimitX) & ~((pos + offset) >> 31));
        }

        public void Dispose()
        {
            //CacheCurrentChunkVerticesData();
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }

            if (centerChunk != null)
            {
                centerChunk.Changed -= OnChunkChanged;
                centerChunk = null;
            }

        }

        private void Dispatch(Action action)
        {

            action();
        }
    }
}
