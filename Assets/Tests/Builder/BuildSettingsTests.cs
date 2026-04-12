using System.Reflection;
using NavVolume.Builder;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Tests.Builder
{
    public class BuildSettingsTests
    {
        #region Auxiliary

        // Fields are readonly, so reflection is needed to set them for testing.
        const string _NUM_LAYERS_FIELD = "<NumLayers>k__BackingField";
        const string _ROOT_SIZE_FIELD = "<RootSize>k__BackingField";

        const float _EPSILON = 1e-5f; // Used for floating-point comparisons.

        /// <summary>
        /// Creates a BuildSettings instance with the given parameters.
        /// </summary>
        BuildSettings CreateSettings(int numLayers, float rootSize)
        {
            var settings = ScriptableObject.CreateInstance<BuildSettings>();

            var numLayersField = typeof(BuildSettings).GetField(
                _NUM_LAYERS_FIELD,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(numLayersField, $"Backing field '{_NUM_LAYERS_FIELD}' not found.");
            numLayersField.SetValue(settings, numLayers);

            var rootSizeField = typeof(BuildSettings).GetField(
                _ROOT_SIZE_FIELD,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(rootSizeField, $"Backing field '{_ROOT_SIZE_FIELD}' not found.");
            rootSizeField.SetValue(settings, rootSize);

            settings.OnValidate();

            return settings;
        }

        #endregion

        [Test]
        public void NodeSizeForLastLayer_ReturnsRootSize(
            [Random(1, 10, 3)] int numLayers,
            [Random(1f, 100f, 3)] float rootSize
        )
        {
            var settings = CreateSettings(numLayers, rootSize);
            Assert.AreEqual(settings.RootSize, settings.NodeSizeForLayer(numLayers - 1), _EPSILON);
        }

        [Test]
        public void NodeSizeForLayer_EachSuccessiveLayer_DoublesThePrevious(
            [Random(1, 10, 3)] int numLayers,
            [Random(1f, 100f, 3)] float rootSize
        )
        {
            var settings = CreateSettings(numLayers, rootSize);

            for (var layer = 0; layer < numLayers - 1; layer++)
            {
                var current = settings.NodeSizeForLayer(layer);
                var next = settings.NodeSizeForLayer(layer + 1);

                Assert.AreEqual(current * 2f, next, _EPSILON);
            }
        }
    }
}
