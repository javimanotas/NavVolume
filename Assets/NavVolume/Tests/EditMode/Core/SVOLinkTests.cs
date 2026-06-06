using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace NavVolume.Tests.EditMode.Core
{
    public class SVOLinkTests
    {
        #region Node link

        [Test]
        public void NodeLink_IsNode_ReturnsTrueAndOutputsLayer(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var link = SVOLink.NodeLink(layer, offset);

            Assert.IsTrue(link.IsNode(out var outLayer));
            Assert.AreEqual(layer, outLayer);
        }

        [Test]
        public void NodeLink_IsVoxel_ReturnsFalse(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var link = SVOLink.NodeLink(layer, offset);

            Assert.IsFalse(link.IsVoxel(out _));
        }

        [Test]
        public void NodeLink_Offset_ReturnsNodeOffset(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var link = SVOLink.NodeLink(layer, offset);

            Assert.AreEqual(offset, link.Offset);
        }

        [Test]
        public void NodeLink_IsValid_ReturnsTrue(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            Assert.IsTrue(SVOLink.NodeLink(layer, offset).IsValid);
        }

        #endregion

        #region Voxel link

        [Test]
        public void VoxelLink_IsVoxel_ReturnsTrueAndOutputsSubnodeIndex(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            var link = SVOLink.VoxelLink(leafOffset, subnodeIndex);

            Assert.IsTrue(link.IsVoxel(out int outSubnode));
            Assert.AreEqual(subnodeIndex, outSubnode);
        }

        [Test]
        public void VoxelLink_IsNode_ReturnsFalse(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            var link = SVOLink.VoxelLink(leafOffset, subnodeIndex);

            Assert.IsFalse(link.IsNode(out _));
        }

        [Test]
        public void VoxelLink_Offset_ReturnsLeafOffset(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            var link = SVOLink.VoxelLink(leafOffset, subnodeIndex);

            Assert.AreEqual(leafOffset, link.Offset);
        }

        [Test]
        public void VoxelLink_IsValid_ReturnsTrue(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            Assert.IsTrue(SVOLink.VoxelLink(leafOffset, subnodeIndex).IsValid);
        }

        [Test]
        public void VoxelLink_AtMaxValues_IsValid()
        {
            var link = SVOLink.VoxelLink(SVOLink.MAX_OFFSET_ALLOWED, SVOLink.MAX_SUBNODE_ALLOWED);

            Assert.IsTrue(link.IsValid);
            Assert.IsTrue(link.IsVoxel(out int outSubnode));
            Assert.AreEqual(SVOLink.MAX_SUBNODE_ALLOWED, outSubnode);
            Assert.AreEqual(SVOLink.MAX_OFFSET_ALLOWED, link.Offset);
        }

        #endregion

        [Test]
        public void NodeLink_AndVoxelLink_WithSameOffset_AreNotEqual(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var nodeLink = SVOLink.NodeLink(0, offset);
            var voxelLink = SVOLink.VoxelLink(offset, 0);

            Assert.AreNotEqual(nodeLink, voxelLink);
        }

        #region Invalid links

        [Test]
        public void Invalid_IsValid_ReturnsFalse()
        {
            Assert.IsFalse(SVOLink.Invalid.IsValid);
        }

        [Test]
        public void Invalid_IsNotEqualToValidNodeLink(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            Assert.AreNotEqual(SVOLink.Invalid, SVOLink.NodeLink(layer, offset));
        }

        [Test]
        public void Invalid_IsNotEqualToValidVoxelLink(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            Assert.AreNotEqual(SVOLink.Invalid, SVOLink.VoxelLink(leafOffset, subnodeIndex));
        }

        [Test]
        public void TwoInvalidLinks_AreEqual()
        {
            Assert.AreEqual(SVOLink.Invalid, SVOLink.Invalid);
        }

        #endregion

        #region Operators and overrides

        [Test]
        public void EqualityOperator_SameNodeLinks_AreEqual(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var a = SVOLink.NodeLink(layer, offset);
            var b = SVOLink.NodeLink(layer, offset);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void EqualityOperator_SameVoxelLinks_AreEqual(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            var a = SVOLink.VoxelLink(leafOffset, subnodeIndex);
            var b = SVOLink.VoxelLink(leafOffset, subnodeIndex);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void EqualityOperator_AndEqualsOverride_AreConsistent(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var a = SVOLink.NodeLink(layer, offset);
            var b = SVOLink.NodeLink(layer, offset);

            Assert.AreEqual(a == b, a.Equals(b));
        }

        [Test]
        public void GetHashCode_EqualLinks_ReturnSameValue(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var a = SVOLink.NodeLink(layer, offset);
            var b = SVOLink.NodeLink(layer, offset);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void ImplicitConversion_NodeLink_RoundTripsToUintAndBack(
            [Random(0, SVOLink.MAX_LAYER_ALLOWED, 3)] int layer,
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int offset
        )
        {
            var original = SVOLink.NodeLink(layer, offset);
            uint raw = original;
            SVOLink restored = raw;

            Assert.AreEqual(original, restored);
        }

        [Test]
        public void ImplicitConversion_VoxelLink_RoundTripsToUintAndBack(
            [Random(0, SVOLink.MAX_OFFSET_ALLOWED, 3)] int leafOffset,
            [Random(0, SVOLink.MAX_SUBNODE_ALLOWED, 3)] int subnodeIndex
        )
        {
            var original = SVOLink.VoxelLink(leafOffset, subnodeIndex);
            uint raw = original;
            SVOLink restored = raw;

            Assert.AreEqual(original, restored);
        }

        [Test]
        public void ImplicitConversion_Invalid_RoundTripsToUintAndBack()
        {
            uint raw = SVOLink.Invalid;
            SVOLink restored = raw;

            Assert.AreEqual(SVOLink.Invalid, restored);
        }

        #endregion
    }
}
