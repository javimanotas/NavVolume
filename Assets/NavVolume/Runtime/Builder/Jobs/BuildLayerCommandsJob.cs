using NavVolume.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    [BurstCompile]
    internal struct BuildLayerCommandsJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<OverlapBoxCommand> Commands;

        [ReadOnly]
        public NativeArray<MortonCode> Candidates;

        public float3 Origin;
        public float CellSize;
        public float3 HalfExtents;
        public QueryParameters QueryParams;

        public void Execute(int index)
        {
            var (x, y, z) = Candidates[index].Decoded;

            var ijk = new float3(x, y, z);
            var center = Origin + (ijk + 0.5f) * CellSize;

            Commands[index] = new OverlapBoxCommand(
                center,
                HalfExtents,
                quaternion.identity,
                QueryParams
            );
        }
    }
}
