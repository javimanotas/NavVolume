using System;
using NavVolume.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
{
    public class SVOLinkTests
    {
        [Test]
        public void EncodedLayerIdx_ReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            Assert.AreEqual(layer, new SVOLink(layer, node, subnode).LayerIdx);
        }

        [Test]
        public void EncodedNodeIdx_ReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            Assert.AreEqual(node, new SVOLink(layer, node, subnode).NodeIdx);
        }

        [Test]
        public void EncodedSubnodeIdx_ReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            Assert.AreEqual(subnode, new SVOLink(layer, node, subnode).SubnodeIdx);
        }

        [Test]
        public void WithSubnode_ReplacesSubnodeIndex(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint initialSubnode,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint newSubnode
        )
        {
            var link = new SVOLink(layer, node, initialSubnode);
            var updatedLink = link.WithSubnode(newSubnode);

            Assert.AreEqual(layer, updatedLink.LayerIdx);
            Assert.AreEqual(node, updatedLink.NodeIdx);
            Assert.AreEqual(newSubnode, updatedLink.SubnodeIdx);
        }

        [Test]
        public void WithoutSubnode_ClearsSubnodeIndex(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            var link = new SVOLink(layer, node, subnode);
            var clearedLink = link.WithoutSubnode();

            Assert.AreEqual(layer, clearedLink.LayerIdx);
            Assert.AreEqual(node, clearedLink.NodeIdx);
            Assert.AreEqual(0u, clearedLink.SubnodeIdx);
        }

        [Test]
        public void IsValid_OnValidLink_ReturnsTrue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 3)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 3)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 3)] uint subnode
        )
        {
            var link = new SVOLink(layer, node, subnode);
            Assert.IsTrue(link.IsValid);
        }

        [Test]
        public void IsValid_OnInvalidLink_ReturnsFalse()
        {
            Assert.IsFalse(SVOLink.Invalid.IsValid);
        }
    }
}
