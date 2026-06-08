using System.Linq;
using System.Runtime.CompilerServices;
using NavVolume.Runtime.Builder;
using UnityEngine;

namespace NavVolume.Runtime.Core
{
    internal readonly struct SVOStats
    {
        #region Memory overhead constants

        // Rough managed-heap overhead constants on 64-bit CoreCLR.
        // Values are approximate.

        const int _OBJECT_HEADER_BYTES = 24; // Sync block / object header + method table pointer + padding to align to 8 bytes
        const int _ARRAY_HEADER_BYTES = 24; // Sync block / object header + method table pointer + length + padding to align to 8 bytes
        const int _REFERENCE_BYTES = 8;

        #endregion

        public readonly float VoxelSize;

        public readonly int TheoreticalVoxelsCount;

        public readonly int VoxelsCount;

        public readonly int MemoryUsedBytes;

        /// <summary>
        /// Allocated nodes per internal layer, ordered from root (index 0) down to layer 0.
        /// </summary>
        public readonly int[] NodesPerLayer;

        /// <summary>
        /// Theoretical node count per internal layer if the octree were fully dense, aligned with <see cref="NodesPerLayer"/>.
        /// </summary>
        public readonly long[] TheoreticalNodesPerLayer;

        public SVOStats(NavContext ctx)
        {
            var svo = ctx.Svo;

            VoxelSize = ctx.BuildSettings.VoxelSize;

            TheoreticalVoxelsCount = (int)(
                Mathf.Pow(8, svo.Layers.Length - 1) * SVOLeaf.NUM_VOXELS
            );

            VoxelsCount = svo.LeafNodes.Length * SVOLeaf.NUM_VOXELS;

            MemoryUsedBytes = ComputeMemoryUsedBytes(svo);

            NodesPerLayer = svo.Layers.Select(l => l.Length).Reverse().ToArray();

            TheoreticalNodesPerLayer = new long[NodesPerLayer.Length];
            for (var i = 0; i < NodesPerLayer.Length; i++)
            {
                TheoreticalNodesPerLayer[i] = 1L << (3 * i);
            }
        }

        static int ComputeMemoryUsedBytes(SVO svo)
        {
            var total = _OBJECT_HEADER_BYTES;

            total += _ARRAY_HEADER_BYTES + Unsafe.SizeOf<SVOLeaf>() * svo.LeafNodes.Length;

            // Outer jagged array: one reference slot per layer.
            total += _ARRAY_HEADER_BYTES + _REFERENCE_BYTES * svo.Layers.Length;

            // Each layer is a plain SVONode[] now (no List wrapper, no spare capacity): one array
            // header plus exactly its element count.
            foreach (var layer in svo.Layers)
            {
                total += _ARRAY_HEADER_BYTES + Unsafe.SizeOf<SVONode>() * layer.Length;
            }

            return total;
        }
    }
}
