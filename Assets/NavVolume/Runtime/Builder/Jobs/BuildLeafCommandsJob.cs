using NavVolume.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    [BurstCompile]
    internal struct BuildLeafCommandsJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<OverlapBoxCommand> Commands;

        [ReadOnly]
        public NativeArray<float3> Corners;

        public float VoxelSize;
        public float3 HalfExtents;
        public QueryParameters QueryParams;

        public void Execute(int index)
        {
            var leafIdx = index / SVOLeaf.NUM_VOXELS;
            var sub = index - leafIdx * SVOLeaf.NUM_VOXELS;

            var k = sub % SVOLeaf.GRID_SIZE;
            var j = (sub / SVOLeaf.GRID_SIZE) % SVOLeaf.GRID_SIZE;
            var i = sub / (SVOLeaf.GRID_SIZE * SVOLeaf.GRID_SIZE);

            var ijk = new float3(i, j, k);
            var center = Corners[leafIdx] + (ijk + 0.5f) * VoxelSize;

            Commands[index] = new OverlapBoxCommand(
                center,
                HalfExtents,
                quaternion.identity,
                QueryParams
            );
        }
    }
}
