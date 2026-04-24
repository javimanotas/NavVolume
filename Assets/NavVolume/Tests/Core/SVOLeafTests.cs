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

        [Test]
        public void SameLeafNode_ReturnsTrue_ForSameNode(
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnodeA,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnodeB
        )
        {
            var linkA = new SVOLink(0, node, subnodeA);
            var linkB = new SVOLink(0, node, subnodeB);

            Assert.IsTrue(linkA.SameLeafNode(linkB));
        }

        [Test]
        public void SameLeafNode_ReturnsFalse_ForDifferentNode(
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint nodeA,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint nodeB,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            if (nodeA == nodeB)
            {
                return;
            }

            var linkA = new SVOLink(0, nodeA, subnode);
            var linkB = new SVOLink(0, nodeB, subnode);

            Assert.IsFalse(linkA.SameLeafNode(linkB));
        }

        [Test]
        public void SameLeafNode_ReturnsFalse_ForNonLeafNode(
            [Random(1u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            var linkA = new SVOLink(layer, node, subnode);
            var linkB = new SVOLink(layer, node, subnode);

            Assert.IsFalse(linkA.SameLeafNode(linkB));
        }
    }
}
