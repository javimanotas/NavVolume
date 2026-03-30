using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
{
    public class SVOLeafTests
    {
        [Test]
        public void IsOccupied_AfterSetOccupied_ReturnsTrue(
            [Random(0, SVOLeaf.GRID_SIZE - 1, 2)] int x,
            [Random(0, SVOLeaf.GRID_SIZE - 1, 2)] int y,
            [Random(0, SVOLeaf.GRID_SIZE - 1, 2)] int z
        )
        {
            var leaf = SVOLeaf.Empty;
            leaf.SetOccupied(x, y, z);
            Assert.IsTrue(leaf.IsOccupied(x, y, z));
        }
    }
}
