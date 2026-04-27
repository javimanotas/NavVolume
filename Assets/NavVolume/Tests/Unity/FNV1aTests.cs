using NUnit.Framework;
using UnityEngine;

namespace NavVolume.Tests.Unity
{
    public class FNV1aTests
    {
        [Test]
        public void FeedBool_ChangesHashConsistently()
        {
            var hasherTrue = new FNV1a();
            hasherTrue.Feed(true);

            var hasherFalse = new FNV1a();
            hasherFalse.Feed(false);

            var hasherTrue2 = new FNV1a();
            hasherTrue2.Feed(true);

            Assert.AreNotEqual(hasherTrue, hasherFalse);
            Assert.AreEqual(hasherTrue, hasherTrue2);
        }

        [Test]
        public void FeedInt_UpdatesHashConsistently()
        {
            var hasherA = new FNV1a();
            hasherA.Feed(42);

            var hasherB = new FNV1a();
            hasherB.Feed(42);

            var hasherC = new FNV1a();
            hasherC.Feed(43);

            Assert.AreEqual(hasherA, hasherB);
            Assert.AreNotEqual(hasherA, hasherC);
        }

        [Test]
        public void FeedFloat_UpdatesHashUsingFloatBits()
        {
            var hasherA = new FNV1a();
            hasherA.Feed(3.14f);

            var hasherB = new FNV1a();
            hasherB.Feed(3.14f);

            var hasherC = new FNV1a();
            hasherC.Feed(3.14159f);

            Assert.AreEqual(hasherA, hasherB);
            Assert.AreNotEqual(hasherA, hasherC);
        }

        [Test]
        public void FeedVector3_UpdatesHashConsistently()
        {
            var hasherA = new FNV1a();
            hasherA.Feed(new Vector3(1, 2, 3));

            var hasherB = new FNV1a();
            hasherB.Feed(new Vector3(1, 2, 3));

            var hasherC = new FNV1a();
            hasherC.Feed(new Vector3(3, 2, 1));

            Assert.AreEqual(hasherA, hasherB);
            Assert.AreNotEqual(hasherA, hasherC);
        }
    }
}
