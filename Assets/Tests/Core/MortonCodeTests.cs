using System.Collections.Generic;
using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
{
    public class MortonCodeTests
    {
        const uint _MAX_COORD = 0xFF;

        const uint _NUM_CHILDS = 8;

        [Test]
        public void DecodedAfterConstructionReturnsOriginalCoords(
            [Random(0u, _MAX_COORD, 2)] uint x,
            [Random(0u, _MAX_COORD, 2)] uint y,
            [Random(0u, _MAX_COORD, 2)] uint z
        )
        {
            var (dx, dy, dz) = new MortonCode(x, y, z).Decoded;
            Assert.AreEqual((x, y, z), (dx, dy, dz));
        }

        [Test]
        public void ConstructionAfterDecodedReturnsOriginalCode(
            [Random(0u, _MAX_COORD, 2)] uint x,
            [Random(0u, _MAX_COORD, 2)] uint y,
            [Random(0u, _MAX_COORD, 2)] uint z
        )
        {
            var original = new MortonCode(x, y, z);
            var (dx, dy, dz) = original.Decoded;
            Assert.AreEqual(original, new MortonCode(dx, dy, dz));
        }

        [Test]
        public void ParentCodeOfChildIsOriginal(
            [Random(0u, _MAX_COORD, 2)] uint x,
            [Random(0u, _MAX_COORD, 2)] uint y,
            [Random(0u, _MAX_COORD, 2)] uint z
        )
        {
            var code = new MortonCode(x, y, z);

            for (var i = 0U; i < _NUM_CHILDS; i++)
            {
                Assert.AreEqual(code, code.ChildCode(i).ParentCode);
            }
        }

        [Test]
        public void ChildCodesAreAllDistinct(
            [Random(0u, _MAX_COORD, 2)] uint x,
            [Random(0u, _MAX_COORD, 2)] uint y,
            [Random(0u, _MAX_COORD, 2)] uint z
        )
        {
            var code = new MortonCode(x, y, z);
            var children = new HashSet<MortonCode>();

            for (var i = 0U; i < 8; i++)
            {
                children.Add(code.ChildCode(i));
            }

            Assert.AreEqual(8, children.Count, "All 8 children must be distinct");
        }
    }
}
