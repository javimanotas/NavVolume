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
        [field: SerializeField]
        public int NumLayers { get; private set; }

        [Tooltip("World-space minimum corner of the volume to cover.")]
        [field: SerializeField]
        public Vector3 Origin { get; private set; }

        [Tooltip("Side length of the cubic world volume (meters).")]
        [Min(0)]
        [field: SerializeField]
        public float RootSize { get; private set; }

        [Tooltip("Physics layers that count as solid obstacles.")]
        [field: SerializeField]
        public LayerMask CollisionMask { get; private set; }

        [field: SerializeField]
        public float VoxelSize { get; private set; }

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
