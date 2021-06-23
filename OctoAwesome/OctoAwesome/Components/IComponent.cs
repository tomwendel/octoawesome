using OctoAwesome.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Components
{
    public interface IComponent : ISerializable
    {
        bool Sendable { get; set; }
        bool Enabled { get; set; }
        bool Serializeable { get; set; }

    }
}
