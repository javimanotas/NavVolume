using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Burst-readable view over one <see cref="NavVolumeSpace"/>'s finest-resolution occupancy.
    /// References a slice of the shared pools of sorted layer-0 morton codes and leaf bitmasks owned by <see cref="AvoidanceSimulation"/>.
    /// </summary>
    internal struct VoxelGrid
    {
        /// <summary>
        /// First node of this grid inside the shared morton/mask pools.
        /// </summary>
        public int NodeStart;

        /// <summary>
        /// Number of layer-0 nodes.
        /// </summary>
        public int NodeCount;

        /// <summary>
        /// Number of layer-0 nodes per axis.
        /// </summary>
        public int NodeGridDim;

        /// <summary>
        /// World-space minimum corner of the volume.
        /// </summary>
        public float3 Origin;

        public float VoxelSize;

        /// <summary>
        /// World size of a layer-0 node (4 voxels).
        /// </summary>
        public float NodeSize;
    }
}
