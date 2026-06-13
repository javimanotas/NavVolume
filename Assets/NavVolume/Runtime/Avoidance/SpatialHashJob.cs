using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Hashes every agent into its uniform-grid cell so the avoidance job can gather neighbors
    /// without scanning the whole population.
    /// </summary>
    [BurstCompile]
    internal struct SpatialHashJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<AvoidanceAgentState> Agents;

        public float InvCellSize;

        public NativeParallelMultiHashMap<int, int>.ParallelWriter Writer;

        public void Execute(int index) =>
            Writer.Add(
                SpatialHash.CellKey((int3)math.floor(Agents[index].Position * InvCellSize)),
                index
            );
    }
}
