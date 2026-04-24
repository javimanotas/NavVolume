using System;
using NavVolume.Core;
using UnityEngine;

namespace NavVolume.Builder
{
    /// <summary>
    /// Data container for parameters involved in the build process of the SVO.
    /// </summary>
    /// <remarks>
    /// This struct is not implemented as read-only to allow serialization.
    /// </remarks>
    [Serializable]
    internal struct BuildSettings
    {
        // TODO: add settings for agent radius

        /// <summary>
        /// World-space minimum corner of the volume to cover.
        /// </summary>
        [field: SerializeField]
        public Vector3 Origin { get; private set; }

        /// <summary>
        /// Side length of the cubic world volume (meters).
        /// </summary>
        [field: SerializeField]
        public float RootSize { get; private set; }

        /// <summary>
        /// Number of layers of the SVO.
        /// </summary>
        [field: SerializeField]
        public int NumLayers { get; private set; }

        /// <summary>
        /// Physics layers that count as solid obstacles.
        /// </summary>
        [field: SerializeField]
        public LayerMask CollisionMask { get; private set; }

        /// <summary>
        /// Size of a single voxel (meters). This is derived from the root size and number of layers.
        /// </summary>
        [field: SerializeField]
        public float VoxelSize { get; private set; }

        public BuildSettings(Vector3 center, float rootSize, int numLayers, LayerMask collisionMask)
        {
            Origin = center - Vector3.one * (rootSize / 2);
            RootSize = rootSize;
            NumLayers = numLayers;
            VoxelSize = RootSize;
            CollisionMask = collisionMask;

            VoxelSize = RootSize;

            for (var i = 0; i < NumLayers - 1; i++)
            {
                VoxelSize /= 2;
            }

            VoxelSize /= 4;
        }

        /// <summary>
        /// Determines the size of a node at a given layer.
        /// </summary>
        /// <param name="layer">
        /// A lower layer index corresponds to a smaller node size.
        /// </param>
        public readonly float NodeSizeForLayer(int layer) =>
            VoxelSize * SVOLeaf.GRID_SIZE * (1 << layer);
    }
}
