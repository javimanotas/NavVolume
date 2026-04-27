using System;
using NUnit.Framework;

namespace NavVolume.Tests.Unity
{
    public class HashHelperTests
    {
        [Test]
        public void FloatBitsAsInt_ReturnsCorrectBitRepresentation()
        {
            var testValue = 3.14159f;

            var expected = BitConverter.ToInt32(BitConverter.GetBytes(testValue), 0);
            var actual = HashHelper.FloatBitsAsInt(testValue);

            Assert.AreEqual(expected, actual);
        }
    }
}
