using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    [BurstCompile]
    internal struct BuildCoarseLeafCommandsJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<OverlapBoxCommand> Commands;

        [ReadOnly]
        public NativeArray<float3> Corners;

        public float NodeCenterOffset;
        public float3 HalfExtents;
        public QueryParameters QueryParams;

        public void Execute(int index)
        {
            var center = Corners[index] + NodeCenterOffset;

            Commands[index] = new OverlapBoxCommand(
                center,
                HalfExtents,
                quaternion.identity,
                QueryParams
            );
        }
    }
}
