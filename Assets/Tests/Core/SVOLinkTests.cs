using System;
using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
{
    public class SVOLinkTests
    {
        [Test]
        public void EncodedLayerIdxReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 2)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 2)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 2)] uint subnode
        )
        {
            Assert.AreEqual(layer, new SVOLink(layer, node, subnode).LayerIdx);
        }

        [Test]
        public void EncodedNodeIdxReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 2)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 2)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 2)] uint subnode
        )
        {
            Assert.AreEqual(node, new SVOLink(layer, node, subnode).NodeIdx);
        }

        [Test]
        public void EncodedSubnodeIdxReturnsConstructorValue(
            [Random(0u, SVOLink.MAX_LAYER_ALLOWED, 2)] uint layer,
            [Random(0u, SVOLink.MAX_NODE_ALLOWED, 2)] uint node,
            [Random(0u, SVOLink.MAX_SUBNODE_ALLOWED, 2)] uint subnode
        )
        {
            Assert.AreEqual(subnode, new SVOLink(layer, node, subnode).SubnodeIdx);
        }
    }
}
