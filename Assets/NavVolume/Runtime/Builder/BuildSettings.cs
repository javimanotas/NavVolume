using System;
using NavVolume.Core;
using UnityEngine;

namespace NavVolume.Builder
{
    /// <summary>
    /// Data container for parameters involved in the build process of the SVO.
    /// </summary>
    [Serializable]
    internal readonly struct BuildSettings
    {
        // TODO: add settings for agent radius

        /// <summary>
        /// World-space minimum corner of the volume to cover.
        /// </summary>
        public readonly Vector3 Origin;

        /// <summary>
        /// Side length of the cubic world volume (meters).
        /// </summary>
        public readonly float RootSize;

        /// <summary>
        /// Number of layers of the SVO.
        /// </summary>
        public readonly int NumLayers;

        /// <summary>
        /// Physics layers that count as solid obstacles.
        /// </summary>
        public readonly LayerMask CollisionMask;

        /// <summary>
        /// Size of a single voxel (meters). This is derived from the root size and number of layers.
        /// </summary>
        public readonly float VoxelSize;

        public BuildSettings(int numLayers, Vector3 center, float rootSize, LayerMask collisionMask)
        {
            NumLayers = numLayers;
            Origin = center - Vector3.one * (rootSize / 2);
            RootSize = rootSize;
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
        public float NodeSizeForLayer(int layer) => VoxelSize * SVOLeaf.GRID_SIZE * (1 << layer);
    }
}
