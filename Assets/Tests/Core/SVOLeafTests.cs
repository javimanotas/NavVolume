using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace Assets.Tests.Core
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

                        Assert.IsTrue(leaf.IsOccupied(x, y, z));
                    }
                }
            }
        }
    }
}
