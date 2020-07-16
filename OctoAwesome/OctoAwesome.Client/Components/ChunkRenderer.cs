using engenious;
using engenious.Graphics;

using OctoAwesome.Client.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace OctoAwesome.Client.Components
{
    internal sealed class ChunkRenderer : IDisposable
    {
        private readonly Effect simple;
        private readonly GraphicsDevice graphicsDevice;

        private readonly Texture2DArray textures;
        private readonly Dispatcher dispatcher;

        /// <summary>
        /// Referenz auf den aktuellen Chunk (falls vorhanden)
        /// </summary>
        private IChunk chunk;
        private bool loaded = false;

        private VertexBuffer vb;
        private static IndexBuffer ib;
        private int vertexCount;
        private int indexCount;
        private ILocalChunkCache _manager;
        private readonly int textureColumns;
        private readonly float textureWidth;
        private readonly float textureGap;
        private readonly ReadOnlyDictionary<IBlockDefinition, int> textureOffsets;
        int definitionIndex = 0;


        private readonly SceneControl _sceneControl;
        private readonly IDefinitionManager definitionManager;

        public static RasterizerState rastState = new RasterizerState() { FillMode = PolygonMode.Fill, CullMode = CullMode.CounterClockwise };

        public static bool AmbientOcclusion = true;

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

        public bool DispatchRequired => Thread.CurrentThread.ManagedThreadId != dispatcher.Thread.ManagedThreadId;

        public ChunkRenderer(SceneControl sceneControl, IDefinitionManager definitionManager, Effect simpleShader, GraphicsDevice graphicsDevice, Matrix projection, Texture2DArray textures)
        {
            _sceneControl = sceneControl;
            this.definitionManager = definitionManager;
            this.graphicsDevice = graphicsDevice;
            this.textures = textures;
            dispatcher = Dispatcher.CurrentDispatcher;
            simple = simpleShader;
            GenerateIndexBuffer();
            textureColumns = textures.Width / SceneControl.TEXTURESIZE;
            textureWidth = 1f / textureColumns;
            var texelSize = 1f / SceneControl.TEXTURESIZE;
            textureGap = texelSize / 2;
            // BlockTypes sammlen
            // Dictionary<Type, BlockDefinition> definitionMapping = new Dictionary<Type, BlockDefinition>();
            var definitionIndex = 0;
            var localTextureOffsets = new Dictionary<IBlockDefinition, int>();
            foreach (var definition in definitionManager.GetBlockDefinitions())
            {
                int textureCount = definition.Textures.Count();
                localTextureOffsets.Add(definition, definitionIndex);
                // definitionMapping.Add(definition.GetBlockType(), definition);
                definitionIndex += textureCount;
            }
            textureOffsets = new ReadOnlyDictionary<IBlockDefinition, int>(localTextureOffsets);
        }

        public void SetChunk(ILocalChunkCache manager, int x, int y, int z)
        {
            var newPosition = new Index3(x, y, z);

            if (_manager == manager && newPosition == ChunkPosition)
            {
                NeedsUpdate = !loaded;
                return;
            }

            _manager = manager;
            ChunkPosition = newPosition;

            if (chunk != null)
            {
                chunk.Changed -= OnChunkChanged;
                chunk = null;
            }

            loaded = false;
            NeedsUpdate = true;
        }

        public bool NeedsUpdate = false;


        private void OnChunkChanged(IChunk c)
        {
            NeedsUpdate = true;
            _sceneControl.Enqueue(this);
        }

        public void Draw(Matrix view, Matrix projection, Index3 shift)
        {
            if (!loaded)
            {
                return;
            }

            Matrix worldViewProj = projection * view * Matrix.CreateTranslation(
                shift.X * Chunk.CHUNKSIZE_X,
                shift.Y * Chunk.CHUNKSIZE_Y,
                shift.Z * Chunk.CHUNKSIZE_Z);

            simple.Parameters["WorldViewProj"].SetValue(worldViewProj);
            simple.Parameters["BlockTextures"].SetValue(textures);

            simple.Parameters["AmbientIntensity"].SetValue(0.4f);
            simple.Parameters["AmbientColor"].SetValue(Color.White.ToVector4());

            lock (this)
            {
                if (vb == null)
                {
                    return;
                }

                graphicsDevice.RasterizerState = rastState;

                //graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                graphicsDevice.VertexBuffer = vb;
                graphicsDevice.IndexBuffer = ib;

                foreach (var pass in simple.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.Triangles, 0, 0, vertexCount, 0, indexCount / 3);
                }
            }
        }
        private readonly object ibLock = new object();
        private Index3? _chunkPosition;

        public void GenerateIndexBuffer()
        {
            lock (ibLock)
            {
                if (ib != null)
                {
                    return;
                }

                ib = new IndexBuffer(graphicsDevice, DrawElementsType.UnsignedInt, Chunk.CHUNKSIZE_X * Chunk.CHUNKSIZE_Y * Chunk.CHUNKSIZE_Z * 6 * 6);
                List<int> indices = new List<int>(ib.IndexCount);
                for (int i = 0; i < ib.IndexCount * 2 / 3; i += 4)
                {
                    indices.Add(i + 0);
                    indices.Add(i + 1);
                    indices.Add(i + 3);

                    indices.Add(i + 0);
                    indices.Add(i + 3);
                    indices.Add(i + 2);
                }
                ib.SetData(indices.ToArray());
            }

        }

        static int VertexAO(int side1, int side2, int corner)
        =>
            ((side1 & side2) ^ 1) * (3 - (side1 + side2 + corner));

        static int IsSolidWall(Wall wall, uint solidWall)
            => ((int)solidWall >> (int)wall) & 1;

        public bool RegenerateVertexBuffer()
        {
            if (!ChunkPosition.HasValue)
            {
                return false;
            }

            // Chunk nachladen
            if (this.chunk == null)
            {
                this.chunk = _manager.GetChunk(ChunkPosition.Value);
                if (this.chunk == null)
                {
                    //Thread.Sleep(10);
                    //RegenerateVertexBuffer();
                    //NeedsUpdate = false;
                    return false;
                }

                this.chunk.Changed += OnChunkChanged;
            }

            var vertices = new List<VertexPositionNormalTextureLight>();

            var chunk = this.chunk;

            IBlockDefinition[] blockDefinitions = new IBlockDefinition[27];

            Vector2[] uvOffsets = new[]
                {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            var chunkPos = ChunkPosition.Value;
            for (int z = Chunk.CHUNKSIZE_Z - 1; z >= 0; z -= Chunk.CHUNKSIZE_Z - 1)
            {
                for (int y = 0; y < Chunk.CHUNKSIZE_Y; y++)
                {
                    for (int x = 0; x < Chunk.CHUNKSIZE_X; x++)
                    {
                        GenerateVertices(chunk, vertices, uvOffsets, z, y, x, chunkPos, blockDefinitions, true);
                    }
                }
            }
            for (int z = Chunk.CHUNKSIZE_Z - 1; z >= 0; z--)
            {
                for (int y = 0; y < Chunk.CHUNKSIZE_Y; y += Chunk.CHUNKSIZE_Y - 1)
                {
                    for (int x = 0; x < Chunk.CHUNKSIZE_X; x++)
                    {
                        GenerateVertices(chunk, vertices,  uvOffsets, z, y, x, chunkPos, blockDefinitions, true);
                    }
                }
            }
            for (int z = Chunk.CHUNKSIZE_Z - 1; z >= 0; z--)
            {
                for (int y = 0; y < Chunk.CHUNKSIZE_Y; y++)
                {
                    for (int x = 0; x < Chunk.CHUNKSIZE_X; x += Chunk.CHUNKSIZE_X - 1)
                    {
                        GenerateVertices(chunk, vertices,  uvOffsets, z, y, x, chunkPos, blockDefinitions, true);
                    }
                }
            }

            for (int z = Chunk.CHUNKSIZE_Z - 1; z >= 1; z--)
            {
                for (int y = 1; y < Chunk.CHUNKSIZE_Y - 1; y++)
                {
                    for (int x = 1; x < Chunk.CHUNKSIZE_X - 1; x++)
                    {
                        GenerateVertices(chunk, vertices,  uvOffsets, z, y, x,  chunkPos, blockDefinitions, false);
                    }
                }
            }

            vertexCount = vertices.Count;
            indexCount = vertexCount / 4 * 6;


            if (vertexCount > 0)
            {
                Dispatch(() =>
                {
                    if (vb == null || ib == null)
                    {
                        vb = new VertexBuffer(graphicsDevice, VertexPositionNormalTextureLight.VertexDeclaration, vertexCount + 2);
                    }
                    if (vertexCount + 2 > vb.VertexCount)
                    {
                        vb.Resize(vertexCount + 2);
                    }

                    vb.SetData(vertices.ToArray());
                });
            }


            lock (this)
            {
                //Todo: Unschön
                if (chunk != null && (!ChunkPosition.HasValue || chunk.Index != ChunkPosition.Value))
                {
                    return loaded;
                }
                loaded = true;
                NeedsUpdate |= chunk != this.chunk;
                return !NeedsUpdate;
            }
        }

        private uint AmbientToBrightness(uint ambient)
        {
            return AmbientOcclusion ? (0xFFFFFF / 2) + (0xFFFFFF / 6 * ambient) : (0xFFFFFF);
        }

        private unsafe void GenerateVertices(IChunk chunk, List<VertexPositionNormalTextureLight> vertices, Vector2[] uvOffsets, int z, int y, int x, Index3 chunkPosition, IBlockDefinition[] blockDefinitions, bool getFromManager)
        {
            // Textur-Koordinate "berechnen"
            ushort block = chunk.GetBlock(x, y, z);

            if (block == 0)
            {
                return;
            }

            IBlockDefinition blockDefinition = (IBlockDefinition)definitionManager.GetDefinitionByIndex(block);

            if (blockDefinition == null)
            {
                return;
            }

            if (!textureOffsets.TryGetValue(blockDefinition, out var textureIndex))
            {
                return;
            }
            //Vector2 textureSize = new Vector2(textureWidth - textureSizeGap, textureWidth - textureSizeGap);
            var blocks = stackalloc ushort[27];


            ushort topBlock, bottomBlock, southBlock, northBlock, westBlock, eastBlock;
            if (getFromManager)
            {
                for (int zOffset = 0; zOffset <= 2; zOffset++)
                {
                    for (int yOffset = 0; yOffset <= 2; yOffset++)
                    {
                        for (int xOffset = 0; xOffset <= 2; xOffset++)
                        {
                            Index3 blockOffset = new Index3(x + xOffset - 1, y + yOffset - 1, z + zOffset - 1);
                            blocks[(((zOffset * 3) + yOffset) * 3) + xOffset] = _manager.GetBlock((chunkPosition * Chunk.CHUNKSIZE) + blockOffset);
                        }
                    }
                }
            }
            else
            {
                for (int zOffset = 0; zOffset <= 2; zOffset++)
                {
                    for (int yOffset = 0; yOffset <= 2; yOffset++)
                    {
                        for (int xOffset = 0; xOffset <= 2; xOffset++)
                        {
                            Index3 blockOffset = new Index3(x + xOffset - 1, y + yOffset - 1, z + zOffset - 1);
                            blocks[(((zOffset * 3) + yOffset) * 3) + xOffset] = chunk.GetBlock((chunkPosition * Chunk.CHUNKSIZE) + blockOffset);
                        }
                    }
                }
            }

            topBlock = blocks[(((2 * 3) + 1) * 3) + 1];
            bottomBlock = blocks[(((0 * 3) + 1) * 3) + 1];
            southBlock = blocks[(((1 * 3) + 2) * 3) + 1];
            northBlock = blocks[(((1 * 3) + 0) * 3) + 1];
            westBlock = blocks[(((1 * 3) + 1) * 3) + 0];
            eastBlock = blocks[(((1 * 3) + 1) * 3) + 2];

            for (int zOffset = 0; zOffset <= 2; zOffset++)
            {
                for (int yOffset = 0; yOffset <= 2; yOffset++)
                {
                    for (int xOffset = 0; xOffset <= 2; xOffset++)
                    {
                        Index3 blockOffset = new Index3(x + xOffset - 1, y + yOffset - 1, z + zOffset - 1);
                        blockDefinitions[(((zOffset * 3) + yOffset) * 3) + xOffset] = (IBlockDefinition)definitionManager.GetDefinitionByIndex(blocks[(((zOffset * 3) + yOffset) * 3) + xOffset]);
                    }
                }
            }

            IBlockDefinition topBlockDefintion = blockDefinitions[(((2 * 3) + 1) * 3) + 1];
            IBlockDefinition bottomBlockDefintion = blockDefinitions[(((0 * 3) + 1) * 3) + 1];
            IBlockDefinition southBlockDefintion = blockDefinitions[(((1 * 3) + 2) * 3) + 1];
            IBlockDefinition northBlockDefintion = blockDefinitions[(((1 * 3) + 0) * 3) + 1];
            IBlockDefinition westBlockDefintion = blockDefinitions[(((1 * 3) + 1) * 3) + 0];
            IBlockDefinition eastBlockDefintion = blockDefinitions[(((1 * 3) + 1) * 3) + 2];
            var globalX = x + chunk.Index.X * Chunk.CHUNKSIZE_X;
            var globalY = y + chunk.Index.Y * Chunk.CHUNKSIZE_Y;
            var globalZ = z + chunk.Index.Z * Chunk.CHUNKSIZE_Z;
            ushort cornerBlock;
            int side1, side2;
            IBlockDefinition side1Def, side2Def;
            // Top
            if (topBlock == 0 || (!topBlockDefintion.IsSolidWall(Wall.Bottom) && topBlock != block))
            {
                var top = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Top, _manager, globalX, globalY, globalZ));

                int rotation = -blockDefinition.GetTextureRotation(Wall.Top, _manager, globalX, globalY, globalZ);

                var wertYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 0, (((2 * 3) + 1) * 3) + 0, Wall.Right, (((2 * 3) + 2) * 3) + 1, Wall.Back);
                var wertXYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 2, (((2 * 3) + 2) * 3) + 1, Wall.Left, (((2 * 3) + 1) * 3) + 2, Wall.Back);
                var wertZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 0, (((2 * 3) + 1) * 3) + 0, Wall.Right, (((2 * 3) + 0) * 3) + 1, Wall.Front);
                var wertXZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 2, (((2 * 3) + 1) * 3) + 2, Wall.Left, (((2 * 3) + 0) * 3) + 1, Wall.Front);

                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1), new Vector3(0, 0, 1), uvOffsets[(6 + rotation) % 4], top, AmbientToBrightness(wertYZ));

                var vertXYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 1), new Vector3(0, 0, 1), uvOffsets[(7 + rotation) % 4], top, AmbientToBrightness(wertXYZ));
                var vertZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 1), new Vector3(0, 0, 1), uvOffsets[(5 + rotation) % 4], top, AmbientToBrightness(wertZ));

                var vertXZ = new VertexPositionNormalTextureLight(
                    new Vector3(x + 1, y + 0, z + 1), new Vector3(0, 0, 1), uvOffsets[(4 + rotation) % 4], top, AmbientToBrightness(wertXZ));

                if (wertXYZ + wertZ <= wertYZ + wertXZ)
                {
                    vertices.Add(vertYZ);
                    vertices.Add(vertXYZ);
                    vertices.Add(vertZ);
                    vertices.Add(vertXZ);
                }
                else
                {
                    vertices.Add(vertZ);
                    vertices.Add(vertYZ);
                    vertices.Add(vertXZ);
                    vertices.Add(vertXYZ);
                }
            }


            // Unten
            if (bottomBlock == 0 || (!bottomBlockDefintion.IsSolidWall(Wall.Top) && bottomBlock != block))
            {
                var bottom = (byte)(textureIndex + blockDefinition.GetTextureIndex(Wall.Bottom, _manager, globalX, globalY, globalZ));
                int rotation = -blockDefinition.GetTextureRotation(Wall.Bottom, _manager, globalX, globalY, globalZ);

                var wertY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 0, (((0 * 3) + 1) * 3) + 0, Wall.Right, (((0 * 3) + 2) * 3) + 1, Wall.Back);
                var wertXY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 2, (((0 * 3) + 2) * 3) + 1, Wall.Left, (((0 * 3) + 1) * 3) + 2, Wall.Back);
                var wert = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 0, (((0 * 3) + 1) * 3) + 0, Wall.Right, (((0 * 3) + 0) * 3) + 1, Wall.Front);
                var wertX = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 2, (((0 * 3) + 1) * 3) + 2, Wall.Left, (((0 * 3) + 0) * 3) + 1, Wall.Front);
                var vertXY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 0), new Vector3(0, 0, -1), uvOffsets[(6 + rotation) % 4], bottom, AmbientToBrightness(wertXY));
                var vertY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 0), new Vector3(0, 0, -1), uvOffsets[(7 + rotation) % 4], bottom, AmbientToBrightness(wertY));
                var vertX = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 0), new Vector3(0, 0, -1), uvOffsets[(5 + rotation) % 4], bottom, AmbientToBrightness(wertX));
                var vert = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 0), new Vector3(0, 0, -1), uvOffsets[(4 + rotation) % 4], bottom, AmbientToBrightness(wert));

                if (wert + wertXY <= wertY + wertX)
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

                var wertY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 0, (((1 * 3) + 2) * 3) + 0, Wall.Right, (((0 * 3) + 2) * 3) + 1, Wall.Front);
                var wertXY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 2, (((1 * 3) + 2) * 3) + 2, Wall.Left, (((0 * 3) + 2) * 3) + 1, Wall.Front);
                var wertYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 0, (((1 * 3) + 2) * 3) + 0, Wall.Right, (((2 * 3) + 2) * 3) + 1, Wall.Back);
                var wertXYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 2, (((2 * 3) + 2) * 3) + 1, Wall.Left, (((1 * 3) + 2) * 3) + 2, Wall.Back);

                var vertY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 0), new Vector3(0, 1, 0), uvOffsets[(6 + rotation) % 4], front, AmbientToBrightness(wertY));
                var vertXY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 0), new Vector3(0, 1, 0), uvOffsets[(7 + rotation) % 4], front, AmbientToBrightness(wertXY));
                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1), new Vector3(0, 1, 0), uvOffsets[(5 + rotation) % 4], front, AmbientToBrightness(wertYZ));
                var vertXYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 1), new Vector3(0, 1, 0), uvOffsets[(4 + rotation) % 4], front, AmbientToBrightness(wertXYZ));


                if (wertY + wertXYZ >= wertYZ + wertXY)
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

                var wertZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 0, (((1 * 3) + 0) * 3) + 0, Wall.Right, (((2 * 3) + 0) * 3) + 1, Wall.Back);
                var wertXZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 2, (((2 * 3) + 0) * 3) + 1, Wall.Left, (((1 * 3) + 0) * 3) + 2, Wall.Back);
                var wert = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 0, (((1 * 3) + 0) * 3) + 0, Wall.Right, (((0 * 3) + 0) * 3) + 1, Wall.Front);
                var wertX = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 2, (((1 * 3) + 0) * 3) + 2, Wall.Left, (((0 * 3) + 0) * 3) + 1, Wall.Front);


                var vertZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 1), new Vector3(0, -1, 0), uvOffsets[(4 + rotation) % 4], back, AmbientToBrightness(wertZ));
                var vertXZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 1), new Vector3(0, -1, 0), uvOffsets[(5 + rotation) % 4], back, AmbientToBrightness(wertXZ));
                var vert = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 0), new Vector3(0, -1, 0), uvOffsets[(7 + rotation) % 4], back, AmbientToBrightness(wert));
                var vertX = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 0), new Vector3(0, -1, 0), uvOffsets[(6 + rotation) % 4], back, AmbientToBrightness(wertX));

                if (wert + wertXZ <= wertZ + wertX)
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

                var wertY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 0, (((1 * 3) + 2) * 3) + 0, Wall.Left, (((0 * 3) + 1) * 3) + 0, Wall.Front);
                var wertYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 0, (((2 * 3) + 1) * 3) + 0, Wall.Left, (((1 * 3) + 2) * 3) + 0, Wall.Back);
                var wert = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 0, (((1 * 3) + 0) * 3) + 0, Wall.Right, (((0 * 3) + 1) * 3) + 0, Wall.Front);
                var wertZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 0, (((1 * 3) + 0) * 3) + 0, Wall.Right, (((2 * 3) + 1) * 3) + 0, Wall.Back);

                var vertY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 0), new Vector3(-1, 0, 0), uvOffsets[(7 + rotation) % 4], left, AmbientToBrightness(wertY));
                var vertYZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 1, z + 1), new Vector3(-1, 0, 0), uvOffsets[(4 + rotation) % 4], left, AmbientToBrightness(wertYZ));
                var vert = new VertexPositionNormalTextureLight(
                       new Vector3(x + 0, y + 0, z + 0), new Vector3(-1, 0, 0), uvOffsets[(6 + rotation) % 4], left, AmbientToBrightness(wert));
                var vertZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 0, y + 0, z + 1), new Vector3(-1, 0, 0), uvOffsets[(5 + rotation) % 4], left, AmbientToBrightness(wertZ));

                if (wert + wertYZ <= wertZ + wertY)
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

                int rotation = -blockDefinition.GetTextureRotation(Wall.Right, _manager, globalX, globalY, globalZ);

                var wertXYZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 2) * 3) + 2, (((2 * 3) + 1) * 3) + 2, Wall.Left, (((1 * 3) + 2) * 3) + 2, Wall.Back);
                var wertXY = VertexAO(blockDefinitions, blocks, (((0 * 3) + 2) * 3) + 2, (((1 * 3) + 2) * 3) + 2, Wall.Left, (((0 * 3) + 1) * 3) + 2, Wall.Front);
                var wertXZ = VertexAO(blockDefinitions, blocks, (((2 * 3) + 0) * 3) + 2, (((1 * 3) + 0) * 3) + 2, Wall.Right, (((2 * 3) + 1) * 3) + 2, Wall.Back);
                var wertX = VertexAO(blockDefinitions, blocks, (((0 * 3) + 0) * 3) + 2, (((1 * 3) + 0) * 3) + 2, Wall.Right, (((0 * 3) + 1) * 3) + 2, Wall.Front);

                var vertXYZ = new VertexPositionNormalTextureLight(
                      new Vector3(x + 1, y + 1, z + 1), new Vector3(1, 0, 0), uvOffsets[(5 + rotation) % 4], right, AmbientToBrightness(wertXYZ));
                var vertXY = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 1, z + 0), new Vector3(1, 0, 0), uvOffsets[(6 + rotation) % 4], right, AmbientToBrightness(wertXY));
                var vertXZ = new VertexPositionNormalTextureLight(
                        new Vector3(x + 1, y + 0, z + 1), new Vector3(1, 0, 0), uvOffsets[(4 + rotation) % 4], right, AmbientToBrightness(wertXZ));
                var vertX = new VertexPositionNormalTextureLight(
                       new Vector3(x + 1, y + 0, z + 0), new Vector3(1, 0, 0), uvOffsets[(7 + rotation) % 4], right, AmbientToBrightness(wertX));

                if (wertX + wertXYZ >= wertXZ + wertXY)
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

        private static unsafe uint VertexAO(IBlockDefinition[] blockDefinitions, ushort* blocks, int cornerBlockIndex, int side1Index, Wall side1Wall, int side2Index, Wall side2Wall)
        {
            var cornerBlock = blockDefinitions[cornerBlockIndex]?.SolidWall ?? 0;
            var side1Def = blockDefinitions[side1Index];
            var side2Def = blockDefinitions[side2Index];
            var side1 = side1Def != null ? IsSolidWall(side1Wall, side1Def.SolidWall) : 0;
            var side2 = side2Def != null ? IsSolidWall(side2Wall, side2Def.SolidWall) : 0;

            return (uint)VertexAO(side1, side2, cornerBlock == 0 ? 0 : 1);
        }

        public void Dispose()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if (chunk != null)
            {
                chunk.Changed -= OnChunkChanged;
                chunk = null;
            }

        }

        private void Dispatch(Action action)
        {
            if (DispatchRequired)
            {
                dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
