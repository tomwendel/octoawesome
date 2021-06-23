using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Database.Tests
{
    [TestOf(typeof(Key<>))]
    public class KeyTests
    {
        [Test]
        public void EmptyTest()
        {
            var key = Key<DemoClass>.Empty;
            var bytes = key.GetBytes();
        }

        public class DemoClass : ITag
        {
            public int Length => 0;

            public void FromBytes(byte[] array, int startIndex)
            {
            }

            public byte[] GetBytes() => Array.Empty<byte>();

            public void WriteBytes(Span<byte> span)
            {
            }
        }
    }
}
