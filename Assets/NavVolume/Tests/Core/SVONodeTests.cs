using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace NavVolume.Tests.Core
{
    public class SVONodeTests
    {
        [Test]
        public void Constructor_InitializesStateCorrectly()
        {
            var morton = new MortonCode(1, 2, 3);
            var node = new SVONode(morton);

            Assert.AreEqual(morton, node.MortonCode);
            Assert.AreEqual(SVOLink.Invalid, node.Parent);
            Assert.AreEqual(SVOLink.Invalid, node.FirstChild);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.PosX]);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.NegX]);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.PosY]);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.NegY]);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.PosZ]);
            Assert.AreEqual(SVOLink.Invalid, node.Neighbors[NeighborDirection.NegZ]);
        }

        [Test]
        public void HasChildren_WhenFirstChildIsInvalid_ReturnsFalse()
        {
            var node = new SVONode(new(0, 0, 0));

            Assert.IsFalse(node.HasChildren);
        }

        [Test]
        public void HasChildren_WhenFirstChildIsValid_ReturnsTrue()
        {
            var node = new SVONode(new(0, 0, 0))
            {
                FirstChild = new SVOLink(1, 0), // arbitrary valid link
            };

            Assert.IsTrue(node.HasChildren);
        }
    }
}
