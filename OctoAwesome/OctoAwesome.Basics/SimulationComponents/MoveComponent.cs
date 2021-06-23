using System;
using engenious;
using OctoAwesome.Basics.EntityComponents;
using System.Linq;
using OctoAwesome.EntityComponents;
using engenious.Helper;
using OctoAwesome.Components;

namespace OctoAwesome.Basics.SimulationComponents
{
    public sealed class MoveComponent : SimulationComponent<
        Entity,
        SimulationComponentRecord<Entity, MoveableComponent, PositionComponent>,
        MoveableComponent,
        PositionComponent>
    {
        protected override SimulationComponentRecord<Entity, MoveableComponent, PositionComponent> OnAdd(Entity entity)
        {
            var poscomp = entity.Components.GetComponent<PositionComponent>();
            var movecomp = entity.Components.GetComponent<MoveableComponent>();
            var cache = entity.Components.GetComponent<LocalChunkCacheComponent>().LocalChunkCache;

            var planet = cache.Planet;
            poscomp.Position.NormalizeChunkIndexXY(planet.Size);
            cache.SetCenter(new Index2(poscomp.Position.ChunkIndex));
            return new SimulationComponentRecord<Entity, MoveableComponent, PositionComponent>(entity, movecomp, poscomp);
        }

        protected override void UpdateValue(GameTime gameTime, SimulationComponentRecord<Entity, MoveableComponent, PositionComponent> value)
        {
            var entity = value.Value;
            var movecomp = value.Component1;
            var poscomp = value.Component2;

            if (movecomp is null || poscomp is null)
                return;

            if (entity.Id == Guid.Empty)
                return;

            //TODO:Sehr unschön

            if (entity.Components.ContainsComponent<BoxCollisionComponent>())
            {
                CheckBoxCollision(gameTime, entity, movecomp, poscomp);
            }

            var cache = entity.Components.GetComponent<LocalChunkCacheComponent>().LocalChunkCache;

            var newposition = poscomp.Position + movecomp.PositionMove;
            newposition.NormalizeChunkIndexXY(cache.Planet.Size);
            if (poscomp.Position.ChunkIndex != newposition.ChunkIndex)
            {
                var result = cache.SetCenter(new Index2(poscomp.Position.ChunkIndex));
                if (result)
                    poscomp.Position = newposition;
            }
            else
            {
                poscomp.Position = newposition;
            }

            // Fix fluctuations for direction because of external forces
            var tmp = movecomp.PositionMove;
            if (Math.Abs(tmp.X) < 0.01)
            {
                tmp.X = 0;
            }
            if (Math.Abs(tmp.Y) < 0.01)
            {
                tmp.Y = 0;
            }
            movecomp.PositionMove = tmp;

            //Direction
            if (movecomp.PositionMove.LengthSquared != 0)
            {
                poscomp.Direction = (float)MathHelper.WrapAngle((float)Math.Atan2(movecomp.PositionMove.Y, movecomp.PositionMove.X));
            }
        }

        private void CheckBoxCollision(GameTime gameTime, Entity entity, MoveableComponent movecomp, PositionComponent poscomp)
        {
            if (!entity.Components.ContainsComponent<BodyComponent>())
                return;

            BodyComponent bc = entity.Components.GetComponent<BodyComponent>();


            Coordinate position = poscomp.Position;

            Vector3 move = movecomp.PositionMove;

            //Blocks finden die eine Kollision verursachen könnten
            int minx = (int)Math.Floor(Math.Min(
                position.BlockPosition.X - bc.Radius,
                position.BlockPosition.X - bc.Radius + movecomp.PositionMove.X));
            int maxx = (int)Math.Ceiling(Math.Max(
                position.BlockPosition.X + bc.Radius,
                position.BlockPosition.X + bc.Radius + movecomp.PositionMove.X));
            int miny = (int)Math.Floor(Math.Min(
                position.BlockPosition.Y - bc.Radius,
                position.BlockPosition.Y - bc.Radius + movecomp.PositionMove.Y));
            int maxy = (int)Math.Ceiling(Math.Max(
                position.BlockPosition.Y + bc.Radius,
                position.BlockPosition.Y + bc.Radius + movecomp.PositionMove.Y));
            int minz = (int)Math.Floor(Math.Min(
                position.BlockPosition.Z,
                position.BlockPosition.Z + movecomp.PositionMove.Z));
            int maxz = (int)Math.Ceiling(Math.Max(
                position.BlockPosition.Z + bc.Height,
                position.BlockPosition.Z + bc.Height + movecomp.PositionMove.Z));

            //Beteiligte Flächen des Spielers
            var playerplanes = CollisionPlane.GetEntityCollisionPlanes(bc.Radius, bc.Height, movecomp.Velocity, poscomp.Position);

            bool abort = false;

            var cache = entity.Components.GetComponent<LocalChunkCacheComponent>().LocalChunkCache;

            for (int z = minz; z <= maxz && !abort; z++)
            {
                for (int y = miny; y <= maxy && !abort; y++)
                {
                    for (int x = minx; x <= maxx && !abort; x++)
                    {
                        move = movecomp.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                        Index3 pos = new Index3(x, y, z);
                        Index3 blockPos = pos + position.GlobalBlockIndex;
                        ushort block = cache.GetBlock(blockPos);
                        if (block == 0)
                            continue;

                        var poolingList = new engenious.Utility.PoolingList<CollisionPlane>();
                        foreach (var item in CollisionPlane.GetBlockCollisionPlanes(pos, movecomp.Velocity))
                        {
                            poolingList.Add(item);
                        }

                        foreach (var playerPlane in playerplanes)
                        {
                            foreach (var blockPlane in poolingList)
                            {
                                if (!CollisionPlane.Intersect(blockPlane, playerPlane))
                                    continue;
                                    
                                var distance = CollisionPlane.GetDistance(blockPlane, playerPlane);

                                if (!CollisionPlane.CheckDistance(distance, move))
                                    continue;

                                var subvelocity = (distance / (float)gameTime.ElapsedGameTime.TotalSeconds);
                                var diff = movecomp.Velocity - subvelocity;

                                float vx;
                                float vy;
                                float vz;

                                if (blockPlane.normal.X != 0 && (movecomp.Velocity.X > 0 && diff.X >= 0 && subvelocity.X >= 0 || movecomp.Velocity.X < 0 && diff.X <= 0 && subvelocity.X <= 0))
                                    vx = subvelocity.X;
                                else
                                    vx = movecomp.Velocity.X;

                                if (blockPlane.normal.Y != 0 && (movecomp.Velocity.Y > 0 && diff.Y >= 0 && subvelocity.Y >= 0 || movecomp.Velocity.Y < 0 && diff.Y <= 0 && subvelocity.Y <= 0))
                                    vy = subvelocity.Y;
                                else
                                    vy = movecomp.Velocity.Y;

                                if (blockPlane.normal.Z != 0 && (movecomp.Velocity.Z > 0 && diff.Z >= 0 && subvelocity.Z >= 0 || movecomp.Velocity.Z < 0 && diff.Z <= 0 && subvelocity.Z <= 0))
                                    vz = subvelocity.Z;
                                else
                                    vz = movecomp.Velocity.Z;

                                if (Math.Abs(vx) < 0.01f)
                                    vx = 0;
                                if (Math.Abs(vy) < 0.01f)
                                    vy = 0;
                                if (Math.Abs(vz) < 0.01f)
                                    vz = 0;

                                movecomp.Velocity = new Vector3(vx, vy, vz);

                                if (vx == 0 && vy == 0 && vz == 0)
                                {
                                    abort = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // TODO: Was ist für den Fall Gravitation = 0 oder im Scheitelpunkt des Sprungs?
            //movecomp.OnGround = Player.Velocity.Z == 0f;

            movecomp.PositionMove = movecomp.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
