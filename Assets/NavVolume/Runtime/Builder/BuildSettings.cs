using System;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
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

        /// <summary>
        /// Physical radius of the agent (meters).
        /// <remarks>
        /// Practical upper bound: keep AgentRadius <= VoxelSize / 2 to avoid fully blocking single-voxel-wide corridors.
        /// </remarks>
        [field: SerializeField]
        public float AgentRadius { get; private set; }

        public BuildSettings(
            Vector3 center,
            float rootSize,
            int numLayers,
            LayerMask collisionMask,
            float agentRadius
        )
        {
            Origin = center - Vector3.one * (rootSize / 2);
            RootSize = rootSize;
            NumLayers = numLayers;
            CollisionMask = collisionMask;
            AgentRadius = agentRadius;

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

        #region Operators and overrides

        public static bool operator ==(BuildSettings lhs, BuildSettings rhs) =>
            (lhs.Origin, lhs.RootSize, lhs.NumLayers, lhs.CollisionMask, lhs.AgentRadius)
            == (rhs.Origin, rhs.RootSize, rhs.NumLayers, rhs.CollisionMask, rhs.AgentRadius);

        public static bool operator !=(BuildSettings lhs, BuildSettings rhs) => !(lhs == rhs);

        public override readonly bool Equals(object obj) =>
            obj is BuildSettings other && this == other;

        public override readonly int GetHashCode() =>
            (Origin, RootSize, NumLayers, CollisionMask, AgentRadius).GetHashCode();

        #endregion
    }
}
