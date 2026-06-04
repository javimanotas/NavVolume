using System.Collections.Generic;
using NavVolume.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Gathers SVO voxel data by sampling geometry of the scene.
    /// </summary>
    /// <remarks>
    /// All checks are done with <see cref="OverlapBoxCommand"/> batched across worker threads.
    /// No mesh data is read directly.
    /// </remarks>
    internal static class SVORasterizer
    {
        /// <summary>
        /// Epsilon value used to avoid detecting overlaps on edges.
        /// </summary>
        const float _OVERLAP_BOX_SHRINK = 1e-3f;

        /// <summary>
        /// Inner-batch size for the IJobParallelFor passes that build commands and reduce hits.
        /// </summary>
        const int _PARALLEL_FOR_BATCH = 64;

        /// <summary>
        /// Minimum commands per worker passed to OverlapBoxCommand.ScheduleBatch.
        /// </summary>
        const int _OVERLAP_BATCH_MIN = 32;

        /// <summary>
        /// Sweeps the navigation volume with layer 1 resolution while checking for geometry.
        /// </summary>
        /// <returns>
        /// A sorted list of morton codes for every cell that contains geometry.
        /// </returns>
        public static List<MortonCode> RasterizeL1(BuildSettings settings)
        {
            var cellSize = settings.NodeSizeForLayer(1);
            var gridSize = Mathf.RoundToInt(settings.RootSize / cellSize);
            var totalCells = gridSize * gridSize * gridSize;

            if (totalCells == 0)
            {
                return new List<MortonCode>();
            }

            var halfExtent = cellSize * 0.5f - _OVERLAP_BOX_SHRINK + settings.AgentRadius;
            var queryParams = new QueryParameters(
                settings.CollisionMask,
                hitMultipleFaces: false,
                hitTriggers: QueryTriggerInteraction.Ignore,
                hitBackfaces: false
            );

            var commands = new NativeArray<OverlapBoxCommand>(
                totalCells,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );
            var results = new NativeArray<ColliderHit>(
                totalCells,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory
            );

            var fillHandle = new BuildGridCommandsJob
            {
                Commands = commands,
                Origin = settings.Origin,
                CellSize = cellSize,
                GridSize = gridSize,
                HalfExtents = new Vector3(halfExtent, halfExtent, halfExtent),
                QueryParams = queryParams,
            }.Schedule(totalCells, _PARALLEL_FOR_BATCH);

            var overlapHandle = OverlapBoxCommand.ScheduleBatch(
                commands,
                results,
                _OVERLAP_BATCH_MIN,
                1,
                fillHandle
            );
            overlapHandle.Complete();

            var occupied = new List<MortonCode>();
            for (var i = 0u; i < (uint)gridSize; i++)
            {
                for (var j = 0u; j < (uint)gridSize; j++)
                {
                    for (var k = 0u; k < (uint)gridSize; k++)
                    {
                        var idx = (int)((i * (uint)gridSize + j) * (uint)gridSize + k);
                        if (results[idx].instanceID != 0)
                        {
                            occupied.Add(new MortonCode(i, j, k));
                        }
                    }
                }
            }

            commands.Dispose();
            results.Dispose();

            // (i, j, k) iteration is not Morton-sorted, so we still need to sort.
            occupied.Sort();
            return occupied;
        }

        /// <summary>
        /// Rasterizes the geometry for a batch of leaf nodes in parallel.
        /// </summary>
        public static SVOLeaf[] RasterizeLeaves(BuildSettings settings, Vector3[] leafCorners)
        {
            var leafCount = leafCorners.Length;
            if (leafCount == 0)
            {
                return System.Array.Empty<SVOLeaf>();
            }

            var totalCells = leafCount * SVOLeaf.NUM_VOXELS;
            var halfExtent = settings.VoxelSize * 0.5f - _OVERLAP_BOX_SHRINK + settings.AgentRadius;
            var queryParams = new QueryParameters(
                settings.CollisionMask,
                hitMultipleFaces: false,
                hitTriggers: QueryTriggerInteraction.Ignore,
                hitBackfaces: false
            );

            var corners = new NativeArray<Vector3>(leafCorners, Allocator.TempJob);
            var commands = new NativeArray<OverlapBoxCommand>(
                totalCells,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );
            var results = new NativeArray<ColliderHit>(
                totalCells,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory
            );
            var bitmasks = new NativeArray<ulong>(
                leafCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );

            var fillHandle = new BuildLeafCommandsJob
            {
                Commands = commands,
                Corners = corners,
                VoxelSize = settings.VoxelSize,
                HalfExtents = new Vector3(halfExtent, halfExtent, halfExtent),
                QueryParams = queryParams,
            }.Schedule(totalCells, _PARALLEL_FOR_BATCH);

            var overlapHandle = OverlapBoxCommand.ScheduleBatch(
                commands,
                results,
                _OVERLAP_BATCH_MIN,
                1,
                fillHandle
            );

            var reduceHandle = new ReduceLeafBitmasksJob
            {
                Results = results,
                Bitmasks = bitmasks,
            }.Schedule(leafCount, _PARALLEL_FOR_BATCH, overlapHandle);

            reduceHandle.Complete();

            var leaves = new SVOLeaf[leafCount];
            for (var i = 0; i < leafCount; i++)
            {
                leaves[i] = SVOLeaf.FromRawBits(bitmasks[i]);
            }

            corners.Dispose();
            commands.Dispose();
            results.Dispose();
            bitmasks.Dispose();

            return leaves;
        }

        [BurstCompile]
        struct BuildGridCommandsJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<OverlapBoxCommand> Commands;

            public Vector3 Origin;
            public float CellSize;
            public int GridSize;
            public Vector3 HalfExtents;
            public QueryParameters QueryParams;

            public void Execute(int index)
            {
                var k = index % GridSize;
                var j = (index / GridSize) % GridSize;
                var i = index / (GridSize * GridSize);

                var center =
                    Origin
                    + new Vector3(
                        (i + 0.5f) * CellSize,
                        (j + 0.5f) * CellSize,
                        (k + 0.5f) * CellSize
                    );

                Commands[index] = new OverlapBoxCommand(
                    center,
                    HalfExtents,
                    Quaternion.identity,
                    QueryParams
                );
            }
        }

        [BurstCompile]
        struct BuildLeafCommandsJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<OverlapBoxCommand> Commands;

            [ReadOnly]
            public NativeArray<Vector3> Corners;

            public float VoxelSize;
            public Vector3 HalfExtents;
            public QueryParameters QueryParams;

            public void Execute(int index)
            {
                var leafIdx = index / SVOLeaf.NUM_VOXELS;
                var sub = index - leafIdx * SVOLeaf.NUM_VOXELS;

                var k = sub % SVOLeaf.GRID_SIZE;
                var j = (sub / SVOLeaf.GRID_SIZE) % SVOLeaf.GRID_SIZE;
                var i = sub / (SVOLeaf.GRID_SIZE * SVOLeaf.GRID_SIZE);

                var center =
                    Corners[leafIdx]
                    + new Vector3(
                        (i + 0.5f) * VoxelSize,
                        (j + 0.5f) * VoxelSize,
                        (k + 0.5f) * VoxelSize
                    );

                Commands[index] = new OverlapBoxCommand(
                    center,
                    HalfExtents,
                    Quaternion.identity,
                    QueryParams
                );
            }
        }

        [BurstCompile]
        struct ReduceLeafBitmasksJob : IJobParallelFor
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
}
