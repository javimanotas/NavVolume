using NavVolume.Runtime.Builder;
using NUnit.Framework;
using UnityEngine;

namespace NavVolume.Tests.EditMode.Builder
{
    public class BuildSettingsTests
    {
        const float _EPSILON = 1e-5f; // Used for floating-point comparisons.

        [Test]
        public void NodeSizeForLastLayer_ReturnsRootSize(
            [Random(1, 10, 3)] int numLayers,
            [Random(1f, 100f, 3)] float rootSize
        )
        {
            var settings = new BuildSettings(Vector3.zero, rootSize, numLayers, 0, 0);

            Assert.AreEqual(settings.RootSize, settings.NodeSizeForLayer(numLayers - 1), _EPSILON);
        }

        [Test]
        public void NodeSizeForLayer_EachSuccessiveLayer_DoublesThePrevious(
            [Random(1, 10, 3)] int numLayers,
            [Random(1f, 100f, 3)] float rootSize
        )
        {
            var settings = new BuildSettings(Vector3.zero, rootSize, numLayers, 0, 0);

            for (var layer = 0; layer < numLayers - 1; layer++)
            {
                var current = settings.NodeSizeForLayer(layer);
                var next = settings.NodeSizeForLayer(layer + 1);

                Assert.AreEqual(current * 2f, next, _EPSILON);
            }
        }
    }
}
