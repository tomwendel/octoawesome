using engenious;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OctoAwesome.Components;

namespace OctoAwesome.EntityComponents
{
    /// <summary>
    /// HeadComponent
    /// </summary>
    public sealed class HeadComponent : Component, IEntityComponent
    {
        /// <summary>
        /// HeadPosition
        /// </summary>
        public Vector3 Offset { get; set; }

        /// <summary>
        /// Tilt
        /// </summary>
        public float Tilt { get; set; }

        /// <summary>
        /// Angle
        /// </summary>
        public float Angle { get; set; }

        public HeadComponent()
        {

        }


        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(Offset.X);
            writer.Write(Offset.Y);
            writer.Write(Offset.Z);

            writer.Write(Tilt);
            writer.Write(Angle);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            Offset = new Vector3(x, y, z);


            Tilt = reader.ReadSingle();
            Angle = reader.ReadSingle();
        }
    }
}
