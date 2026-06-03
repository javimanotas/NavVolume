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
        const int _LIST_FIELDS_BYTES = 16; // Items ref + Count + Version (mutation counter for enumeration)
        const int _DICT_FIELDS_BYTES = 56; // Buckets ref + Entries ref + Count + freeList + freeCount + version + comparer ref + fastModMultiplier
        const int _DICT_BUCKET_BYTES = 4;
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

            NodesPerLayer = svo.Layers.Select(l => l.Count).Reverse().ToArray();

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

            total += _ARRAY_HEADER_BYTES + _REFERENCE_BYTES * svo.Layers.Length;
            foreach (var layer in svo.Layers)
            {
                total += _OBJECT_HEADER_BYTES + _LIST_FIELDS_BYTES;
                total += _ARRAY_HEADER_BYTES + Unsafe.SizeOf<SVONode>() * layer.Capacity;
            }

            var dictEntrySize = sizeof(int) * 2 + Unsafe.SizeOf<MortonCode>() + sizeof(int);
            total += _ARRAY_HEADER_BYTES + _REFERENCE_BYTES * svo.MortonToIndex.Length;
            foreach (var dict in svo.MortonToIndex)
            {
                total += _OBJECT_HEADER_BYTES + _DICT_FIELDS_BYTES;
                total += _ARRAY_HEADER_BYTES + (dictEntrySize + _DICT_BUCKET_BYTES) * dict.Count;
            }

            return total;
        }
    }
}
