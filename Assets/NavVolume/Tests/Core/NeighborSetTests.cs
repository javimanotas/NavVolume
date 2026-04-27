using System;
using System.Collections.Generic;
using NavVolume.Runtime.Core;
using NUnit.Framework;

namespace NavVolume.Tests.Core
{
    public class NeighborSetTests
    {
        static IEnumerable<TestCaseData> NeighborDirectionCases()
        {
            yield return new TestCaseData((int)NeighborDirection.PosX).SetName("PosX");
            yield return new TestCaseData((int)NeighborDirection.NegX).SetName("NegX");
            yield return new TestCaseData((int)NeighborDirection.PosY).SetName("PosY");
            yield return new TestCaseData((int)NeighborDirection.NegY).SetName("NegY");
            yield return new TestCaseData((int)NeighborDirection.PosZ).SetName("PosZ");
            yield return new TestCaseData((int)NeighborDirection.NegZ).SetName("NegZ");
        }

        [Test]
        [TestCaseSource(nameof(NeighborDirectionCases))]
        public void AllInvalid_ReturnsInvalidLink_ForAllDirections(int dir)
        {
            var neighbors = NeighborSet.AllInvalid;

            Assert.AreEqual(SVOLink.Invalid, neighbors[(NeighborDirection)dir]);
        }

        [Test]
        [TestCaseSource(nameof(NeighborDirectionCases))]
        public void Indexer_SetAndGet_WorksCorrectly(int dir)
        {
            var neighbors = NeighborSet.AllInvalid;
            var arbitraryValidLink = new SVOLink(1, (uint)dir);

            neighbors[(NeighborDirection)dir] = arbitraryValidLink;

            Assert.AreEqual(arbitraryValidLink, neighbors[(NeighborDirection)dir]);

            foreach (NeighborDirection otherDir in Enum.GetValues(typeof(NeighborDirection)))
            {
                if (otherDir != (NeighborDirection)dir)
                {
                    Assert.AreEqual(SVOLink.Invalid, neighbors[otherDir]);
                }
            }
        }
    }
}
