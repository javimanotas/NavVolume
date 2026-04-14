using NavVolume.Core;
using UnityEngine;

namespace NavVolume.Builder
{
    /// <summary>
    /// Data container for parameters involved in the build process of the SVO.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeBuildSettings", menuName = "NavVolume/Build settings")]
    internal class BuildSettings : ScriptableObject
    {
        [Tooltip("The detail of the navigable space.")]
        [Range(1, 8)]
        [field: SerializeField]
        public int NumLayers { get; private set; } = 5;

        [Tooltip("World-space minimum corner of the volume to cover.")]
        [field: SerializeField]
        public Vector3 Origin { get; private set; }

        [Tooltip("Side length of the cubic world volume (meters).")]
        [Min(0)]
        [field: SerializeField]
        public float RootSize { get; private set; }

        [Tooltip("Physics layers that count as solid obstacles.")]
        [field: SerializeField]
        public LayerMask CollisionMask { get; private set; } = ~0;

        [Tooltip(
            "Size of a single voxel (meters). This is derived from the root size and number of layers."
        )]
        [field: SerializeField]
        public float VoxelSize { get; private set; }

        // TODO: add settings for agent radius

        /// <summary>
        /// Determines the size of a node at a given layer.
        /// </summary>
        /// <param name="layer">
        /// A lower layer index corresponds to a smaller node size.
        /// </param>
        public float NodeSizeForLayer(int layer) => VoxelSize * SVOLeaf.GRID_SIZE * (1 << layer);

        internal void OnValidate()
        {
            VoxelSize = RootSize;

            for (var i = 0; i < NumLayers - 1; i++)
            {
                VoxelSize /= 2;
            }

            VoxelSize /= 4;
        }
    }
}
