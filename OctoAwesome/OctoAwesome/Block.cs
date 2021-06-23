using System;
using engenious;

namespace OctoAwesome
{
    /// <summary>
    /// Helferklasse für die Kollisionserkennung mit Blöcken.
    /// </summary>
    public static class Block
    {
        /// <summary>
        /// Kollisionsmethode, in der die Selektion der Blöcke vom Spieler aus geprüft wird
        /// </summary>
        /// <param name="collisionBoxes">Kollisionsboxen des Blocks</param>
        /// <param name="boxPosition">Die Position, wo sich die BoundingBox befindet</param>
        /// <param name="ray">Der Selektionsstrahl, der vom Spieler ausgeht</param>
        /// <param name="collisionAxis">Welche Achse von der Kollision betroffen ist</param>
        /// <returns>Die Entfernung zwischen der Kollision und der Kollisionsbox der Entität</returns>
        public static float? Intersect(ReadOnlySpan<BoundingBox> collisionBoxes, Index3 boxPosition, Ray ray, out Axis? collisionAxis)
        {
            Vector3 min = new Vector3(1, 1, 1);
            float raylength = Player.SELECTIONRANGE * 2;
            float? minDistance = null;
            bool collided = false;

            foreach (var localBox in collisionBoxes)
            {
                BoundingBox box = new BoundingBox(localBox.Min + boxPosition, localBox.Max + boxPosition);

                float? distance = ray.Intersects(box);

                if (!distance.HasValue)
                    continue;

                if (!minDistance.HasValue || minDistance > distance)
                    minDistance = distance;

                Vector3 boxCorner = new Vector3(
                        ray.Direction.X > 0 ? box.Min.X : box.Max.X,
                        ray.Direction.Y > 0 ? box.Min.Y : box.Max.Y,
                        ray.Direction.Z > 0 ? box.Min.Z : box.Max.Z);

                Vector3 n = (boxCorner - ray.Position) / (ray.Direction * raylength);
                min = new Vector3(Math.Min(min.X, n.X), Math.Min(min.Y, n.Y), Math.Min(min.Z, n.Z));
                collided = true;
            }

            if (!collided)
            {
                collisionAxis = null;
                return null;
            }

            float max = -5f;
            Axis? axis = null;

            // Fall X
            if (min.X < 1f && min.X > max)
            {
                max = min.X;
                axis = Axis.X;
            }

            // Fall Y
            if (min.Y < 1f && min.Y > max)
            {
                max = min.Y;
                axis = Axis.Y;
            }

            // Fall Z
            if (min.Z < 1f && min.Z > max)
            {
                max = min.Z;
                axis = Axis.Z;
            }

            collisionAxis = axis;

            if (axis.HasValue)
                return max * raylength;

            return null;
        }

        /// <summary>
        /// Prüft, ob die Kollisionsbox einer Entität mit den Kollisionsboxen eines Blocks kollidieren
        /// </summary>
        /// <param name="collisionBoxes">Kollisionsboxen des Blocks</param>
        /// <param name="boxPosition">Die Position, wo sich der Block befindet</param>
        /// <param name="player">Die Kollisionsbox einer Entität</param>
        /// <param name="move">Die befegungsrichtung der Entität</param>
        /// <param name="collisionAxis">Welche Achse von der Kollision betroffen ist</param>
        /// <returns>Die Entfernung zwischen der Kollision und der Kollisionsbox der Entität</returns>
        public static float? Intersect(ReadOnlySpan<BoundingBox> collisionBoxes, Index3 boxPosition, BoundingBox player, Vector3 move, out Axis? collisionAxis)
        {
            Vector3 playerCorner = new Vector3(
                        (move.X > 0 ? player.Max.X : player.Min.X),
                        (move.Y > 0 ? player.Max.Y : player.Min.Y),
                        (move.Z > 0 ? player.Max.Z : player.Min.Z));

            Vector3 targetPosition = playerCorner + move;

            Vector3 playerMin = player.Min + move;
            Vector3 playerMax = player.Max + move;

            Vector3 min = new Vector3(1, 1, 1);
            bool collided = false;

            foreach (var localBox in collisionBoxes)
            {
                Vector3 boxMin = localBox.Min + new Vector3(boxPosition.X, boxPosition.Y, boxPosition.Z);
                Vector3 boxMax = localBox.Max + new Vector3(boxPosition.X, boxPosition.Y, boxPosition.Z);

                bool collide =
                    playerMin.X <= boxMax.X && playerMax.X >= boxMin.X &&
                    playerMin.Y <= boxMax.Y && playerMax.Y >= boxMin.Y &&
                    playerMin.Z <= boxMax.Z && playerMax.Z >= boxMin.Z;

                if (!collide)
                    continue;

                Vector3 boxCorner = new Vector3(
                        move.X > 0 ? boxMin.X : boxMax.X,
                        move.Y > 0 ? boxMin.Y : boxMax.Y,
                        move.Z > 0 ? boxMin.Z : boxMax.Z);

                Vector3 n = (boxCorner - playerCorner) / move;
                min = new Vector3(Math.Min(min.X, n.X), Math.Min(min.Y, n.Y), Math.Min(min.Z, n.Z));
                collided = true;
            }

            if (!collided)
            {
                collisionAxis = null;
                return null;
            }
            float max = 0f;
            Axis? axis = null;

            // Fall X
            if (min.X < 1f && min.X > max)
            {
                max = min.X;
                axis = Axis.X;
            }

            // Fall Y
            if (min.Y < 1f && min.Y > max)
            {
                max = min.Y;
                axis = Axis.Y;
            }

            // Fall Z
            if (min.Z < 1f && min.Z > max)
            {
                max = min.Z;
                axis = Axis.Z;
            }

            collisionAxis = axis;

            if (axis.HasValue)
                return max;

            return null;
        }
    }
}
