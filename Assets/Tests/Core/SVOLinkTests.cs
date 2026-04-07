using System;
using NavVolume.Runtime.Core;
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
    }
}
