using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace NavVolume.Tests.Core
{
    public class SVOLeafTests
    {
        [Test]
        public void IsOccupied_AfterSetOccupied_ReturnsTrue()
        {
            for (var x = 0; x < SVOLeaf.GRID_SIZE; x++)
            {
                for (var y = 0; y < SVOLeaf.GRID_SIZE; y++)
                {
                    for (var z = 0; z < SVOLeaf.GRID_SIZE; z++)
                    {
                        var leaf = SVOLeaf.Empty;
                        leaf.SetOccupied(x, y, z);
                        var index = SVOLeaf.SubnodeCoordsToIndex(x, y, z);

                        Assert.IsTrue(leaf.IsOccupied(index));
                    }
                }
            }
        }

        [Test]
        public void IndexToSubnode_ReturnsOriginalCoordinates()
        {
            for (var x = 0; x < SVOLeaf.GRID_SIZE; x++)
            {
                for (var y = 0; y < SVOLeaf.GRID_SIZE; y++)
                {
                    for (var z = 0; z < SVOLeaf.GRID_SIZE; z++)
                    {
                        var index = SVOLeaf.SubnodeCoordsToIndex(x, y, z);
                        var (rx, ry, rz) = SVOLeaf.IndexToSubnodeCoords(index);

                        Assert.AreEqual((x, y, z), (rx, ry, rz));
                    }
                }
            }
        }

        [Test]
        public void IsEmpty_OnEmptyLeaf_ReturnsTrue()
        {
            var leaf = SVOLeaf.Empty;

            Assert.IsTrue(leaf.IsEmpty);
        }

        [Test]
        public void IsEmpty_OnOccupiedLeaf_ReturnsFalse()
        {
            var leaf = SVOLeaf.Empty;
            leaf.SetOccupied(0, 0, 0);

            Assert.IsFalse(leaf.IsEmpty);
        }

        [Test]
        public void IsFull_OnFullLeaf_ReturnsTrue()
        {
            var leaf = SVOLeaf.Empty;

            for (var x = 0; x < SVOLeaf.GRID_SIZE; x++)
            {
                for (var y = 0; y < SVOLeaf.GRID_SIZE; y++)
                {
                    for (var z = 0; z < SVOLeaf.GRID_SIZE; z++)
                    {
                        leaf.SetOccupied(x, y, z);
                    }
                }
            }

            Assert.IsTrue(leaf.IsFull);
        }

        [Test]
        public void IsFull_OnEmptyLeaf_ReturnsFalse()
        {
            var leaf = SVOLeaf.Empty;

            Assert.IsFalse(leaf.IsFull);
        }
    }
}
