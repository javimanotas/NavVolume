using System.Reflection;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Tests.Builder
{
    public class BuildSettingsTests
    {
        #region Auxiliary

        const string _VOXEL_SIZE_FIELD = "<VoxelSize>k__BackingField";

        const float _EPSILON = 1e-5f;

        /// <summary>
        /// Creates a BuildSettings instance and injects a VoxelSize value directly, bypassing OnValidate.
        /// </summary>
        BuildSettings CreateSettings(float voxelSize)
        {
            var settings = ScriptableObject.CreateInstance<BuildSettings>();
            var field = typeof(BuildSettings).GetField(
                _VOXEL_SIZE_FIELD,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(field, $"Backing field '{_VOXEL_SIZE_FIELD}' not found.");

            field.SetValue(settings, voxelSize);
            return settings;
        }

        #endregion

        [Test]
        public void NodeSizeForLayer_ReturnsVoxelSizeTimesGridSize_ForLayerZero(
            [Random(0.1f, 10f, 3)] float voxelSize
        )
        {
            var settings = CreateSettings(voxelSize);
            var expected = voxelSize * SVOLeaf.GRID_SIZE;

            Assert.AreEqual(expected, settings.NodeSizeForLayer(0), _EPSILON);
        }

        [Test]
        public void NodeSizeForLayer_EachSuccessiveLayer_DoublesThePrevious(
            [Random(0.1f, 10f, 3)] float voxelSize,
            [Random(1, 10, 3)] int layer
        )
        {
            var settings = CreateSettings(voxelSize);
            var current = settings.NodeSizeForLayer(layer);
            var next = settings.NodeSizeForLayer(layer + 1);

            Assert.AreEqual(current * 2f, next, _EPSILON);
        }
    }
}
