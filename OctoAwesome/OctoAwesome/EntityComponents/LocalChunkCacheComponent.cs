using OctoAwesome.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.EntityComponents
{
    public sealed class LocalChunkCacheComponent : Component, IEntityComponent
    {
        public ILocalChunkCache LocalChunkCache { get; set; }

        public LocalChunkCacheComponent()
        {
        }
        public LocalChunkCacheComponent(IGlobalChunkCache globalChunkCache, int dimensions,int range)
        {
            LocalChunkCache = new LocalChunkCache(globalChunkCache, dimensions, range);
        }
    }
}
