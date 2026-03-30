using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Data container for parameters involved in the build process of the SVO.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeBuildSettings", menuName = "NavVolume/Build settings")]
    public class BuildSettings : ScriptableObject
    {
        [Tooltip("The detail of the navigable space.")]
        [Min(1)]
        public int NumLayers;

        [Tooltip("World-space minimum corner of the volume to cover.")]
        public Vector3 Origin;

        [Tooltip("Side length of the cubic world volume (meters).")]
        [Min(0)]
        public float RootSize;

        [Tooltip("Physics layers that count as solid obstacles.")]
        public LayerMask CollisionMask;

        public float VoxelSize;

        public float NodeSizeForLayer(int layer) => VoxelSize * SVOLeaf.GRID_SIZE * (1 << layer);

        void OnValidate()
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
