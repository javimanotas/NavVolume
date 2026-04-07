using System;
using System.Collections.Generic;
using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
{
    public class MortonCodeTests
    {
        #region Auxiliary

        const uint _MAX_COORD = 0xFF;

        const uint _NUM_CHILDS = 8;

        const int _RESOLUTION = 8;

        int PossibleNeighborDirections => Enum.GetNames(typeof(NeighborDirection)).Length;

        /// <summary>
        /// Each entry describes one of the 6 face directions as: (direction, axisIndex, delta).
        /// </summary>
        static IEnumerable<TestCaseData> NeighborDirectionCases()
        {
            yield return new TestCaseData((int)NeighborDirection.PosX, 0, 1).SetName("PosX");
            yield return new TestCaseData((int)NeighborDirection.NegX, 0, -1).SetName("NegX");
            yield return new TestCaseData((int)NeighborDirection.PosY, 1, 1).SetName("PosY");
            yield return new TestCaseData((int)NeighborDirection.NegY, 1, -1).SetName("NegY");
            yield return new TestCaseData((int)NeighborDirection.PosZ, 2, 1).SetName("PosZ");
            yield return new TestCaseData((int)NeighborDirection.NegZ, 2, -1).SetName("NegZ");
        }

        static NeighborDirection Opposite(NeighborDirection dir) =>
            dir switch
            {
                NeighborDirection.PosX => NeighborDirection.NegX,
                NeighborDirection.NegX => NeighborDirection.PosX,
                NeighborDirection.PosY => NeighborDirection.NegY,
                NeighborDirection.NegY => NeighborDirection.PosY,
                NeighborDirection.PosZ => NeighborDirection.NegZ,
                NeighborDirection.NegZ => NeighborDirection.PosZ,
                _ => throw new System.ArgumentOutOfRangeException(),
            };

        #endregion

        [Test]
        public void DecodedAfterConstruction_ReturnsOriginalCoords(
            [Random(0u, _MAX_COORD, 3)] uint x,
            [Random(0u, _MAX_COORD, 3)] uint y,
            [Random(0u, _MAX_COORD, 3)] uint z
        )
        {
            var (dx, dy, dz) = new MortonCode(x, y, z).Decoded;

            Assert.AreEqual((x, y, z), (dx, dy, dz));
        }

        [Test]
        public void ConstructionAfterDecoded_ReturnsOriginalCode(
            [Random(0u, _MAX_COORD, 3)] uint x,
            [Random(0u, _MAX_COORD, 3)] uint y,
            [Random(0u, _MAX_COORD, 3)] uint z
        )
        {
            var original = new MortonCode(x, y, z);
            var (dx, dy, dz) = original.Decoded;

            Assert.AreEqual(original, new MortonCode(dx, dy, dz));
        }

        [Test]
        public void ParentCodeOfChild_IsOriginal(
            [Random(0u, _MAX_COORD, 3)] uint x,
            [Random(0u, _MAX_COORD, 3)] uint y,
            [Random(0u, _MAX_COORD, 3)] uint z
        )
        {
            var code = new MortonCode(x, y, z);

            for (var i = 0u; i < _NUM_CHILDS; i++)
            {
                Assert.AreEqual(code, code.ChildCode(i).ParentCode);
            }
        }

        [Test]
        public void ChildCodes_AreAllDistinct(
            [Random(0u, _MAX_COORD, 3)] uint x,
            [Random(0u, _MAX_COORD, 3)] uint y,
            [Random(0u, _MAX_COORD, 3)] uint z
        )
        {
            var code = new MortonCode(x, y, z);
            var children = new HashSet<MortonCode>();

            for (var i = 0u; i < 8; i++)
            {
                children.Add(code.ChildCode(i));
            }

            Assert.AreEqual(8, children.Count);
        }

        [Test]
        public void TryGetNeighborCode_OnCentralNode_AllDirectionsValid(
            [Random(1u, _RESOLUTION - 2, 3)] uint x,
            [Random(1u, _RESOLUTION - 2, 3)] uint y,
            [Random(1u, _RESOLUTION - 2, 3)] uint z
        )
        {
            var code = new MortonCode(x, y, z);

            for (var i = 0; i < PossibleNeighborDirections; i++)
            {
                var hasNeighbor = code.TryGetNeighborCode((NeighborDirection)i, _RESOLUTION, out _);

                Assert.IsTrue(hasNeighbor);
            }
        }

        [Test]
        public void TryGetNeighborCode_Twice_ReturnsOriginalCode(
            [Random(0u, _RESOLUTION - 1, 3)] uint x,
            [Random(0u, _RESOLUTION - 1, 3)] uint y,
            [Random(0u, _RESOLUTION - 1, 3)] uint z
        )
        {
            var original = new MortonCode(x, y, z);

            for (var i = 0; i < PossibleNeighborDirections; i++)
            {
                if (
                    !original.TryGetNeighborCode(
                        (NeighborDirection)i,
                        _RESOLUTION,
                        out var neighbor
                    )
                )
                {
                    continue; // skip invalid neighbors (e.g. at boundaries)
                }

                neighbor.TryGetNeighborCode(
                    Opposite((NeighborDirection)i),
                    _RESOLUTION,
                    out var neighborNeighbor
                );

                Assert.AreEqual(original, neighborNeighbor);
            }
        }

        [Test]
        [TestCaseSource(nameof(NeighborDirectionCases))]
        public void TryGetNeighborCode_ForInteriorNode_OffsetsCoordsByOne(
            int dir,
            int axisIndex,
            int delta
        )
        {
            var code = new MortonCode(3, 3, 3);
            code.TryGetNeighborCode((NeighborDirection)dir, _RESOLUTION, out var neighbor);

            var (ox, oy, oz) = code.Decoded;
            var (nx, ny, nz) = neighbor.Decoded;

            var origin = new uint[] { ox, oy, oz };
            var result = new uint[] { nx, ny, nz };

            for (var axis = 0; axis < 3; axis++)
            {
                var expected = axis == axisIndex ? origin[axis] + delta : origin[axis];

                Assert.AreEqual(expected, result[axis]);
            }
        }

        [Test]
        [TestCaseSource(nameof(NeighborDirectionCases))]
        public void TryGetNeighborCode_AtPositiveBoundary_ReturnsFalseForPositiveDirection(
            int dir,
            int axisIndex,
            int delta
        )
        {
            if (delta < 0)
            {
                // only test the positive directions here
                return;
            }

            var maxCoord = _RESOLUTION - 1u;
            var x = axisIndex == 0 ? maxCoord : 1u;
            var y = axisIndex == 1 ? maxCoord : 1u;
            var z = axisIndex == 2 ? maxCoord : 1u;

            var code = new MortonCode(x, y, z);
            var result = code.TryGetNeighborCode((NeighborDirection)dir, _RESOLUTION, out _);

            Assert.IsFalse(result);
        }

        [Test]
        [TestCaseSource(nameof(NeighborDirectionCases))]
        public void TryGetNeighborCode_AtNegativeBoundary_ReturnsFalseForNegativeDirection(
            int dir,
            int axisIndex,
            int delta
        )
        {
            if (delta > 0)
            {
                // only test the negative directions here
                return;
            }

            var x = axisIndex == 0 ? 0u : 1u;
            var y = axisIndex == 1 ? 0u : 1u;
            var z = axisIndex == 2 ? 0u : 1u;

            var code = new MortonCode(x, y, z);
            var result = code.TryGetNeighborCode((NeighborDirection)dir, _RESOLUTION, out _);

            Assert.IsFalse(result);
        }
    }
}
