using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NavVolume.Core
{
    internal readonly struct SVOStats
    {
        // TODO: consider if the best approach is to be defined inside the SVO to access private data.
        // consider if this struct should be tottaly private or if should be exposed in some way for displaying data in the editor or for debugging purposes (not using .ToString()).

        public readonly int NumLayers;

        public readonly int TheoreticalVoxelsCount;

        public readonly int VoxelsCount;

        public readonly int MemoryUsedBytes;

        public SVOStats(SVO svo)
        {
            NumLayers = svo.Layers.Length;

            TheoreticalVoxelsCount = (int)(
                Mathf.Pow(8, svo.Layers.Length - 1) * SVOLeaf.NUM_VOXELS
            );

            VoxelsCount = svo.LeafNodes.Length * SVOLeaf.NUM_VOXELS;

            // TODO: improve memory calculation
            MemoryUsedBytes =
                Unsafe.SizeOf<SVOLeaf>() * svo.LeafNodes.Length
                + Unsafe.SizeOf<SVONode>() * svo.Layers.Sum(l => l.Count);
        }

        public override readonly string ToString() =>
            $"Theoretical voxels for {NumLayers} layer octree: {TheoreticalVoxelsCount}.\n"
            + $"Allocated voxels: {VoxelsCount}.\n"
            + $"Voxels saved with sparse implementation: {(1 - (float)VoxelsCount / TheoreticalVoxelsCount) * 100:F2}%.\n"
            + $"Approximate memory usage (bytes): {MemoryUsedBytes}.\n";
    }
}
