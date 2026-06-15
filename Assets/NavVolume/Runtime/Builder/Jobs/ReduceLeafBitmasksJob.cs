using NavVolume.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    [BurstCompile]
    internal struct ReduceLeafBitmasksJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<ColliderHit> Results;

        [WriteOnly]
        public NativeArray<ulong> Bitmasks;

        public void Execute(int leafIdx)
        {
            ulong bits = 0;
            var baseIdx = leafIdx * SVOLeaf.NUM_VOXELS;

            for (var sub = 0; sub < SVOLeaf.NUM_VOXELS; sub++)
            {
                if (Results[baseIdx + sub].instanceID == 0)
                {
                    continue;
                }

                var k = sub % SVOLeaf.GRID_SIZE;
                var j = (sub / SVOLeaf.GRID_SIZE) % SVOLeaf.GRID_SIZE;
                var i = sub / (SVOLeaf.GRID_SIZE * SVOLeaf.GRID_SIZE);

                var bitIdx = SVOLeaf.SubnodeCoordsToIndex(i, j, k);
                bits |= 1UL << bitIdx;
            }

            Bitmasks[leafIdx] = bits;
        }
    }
}
