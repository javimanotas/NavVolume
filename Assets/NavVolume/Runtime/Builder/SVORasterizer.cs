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
        /// <remarks>
        /// Uses a coarse-to-fine hierarchical traversal: starts at a layer just below the root
        /// (typically 8 candidate cells) and only subdivides candidates that contain geometry,
        /// skipping entire empty subtrees. For sparse scenes (the common case for nav volumes)
        /// this is orders of magnitude faster than a dense L1 sweep.
        /// </remarks>
        /// <returns>
        /// A sorted list of morton codes for every L1 cell that contains geometry.
        /// </returns>
        public static List<MortonCode> RasterizeL1(BuildSettings settings)
        {
            var queryParams = new QueryParameters(
                settings.CollisionMask,
                hitMultipleFaces: false,
                hitTriggers: QueryTriggerInteraction.Ignore,
                hitBackfaces: false
            );

            // Start at the layer just below root (8 seed cells = a 2x2x2 grid).
            // For very shallow trees (NumLayers < 3) we fall back to seeding at layer 1
            // directly, which is equivalent to the old dense sweep.
            var topLayer = Mathf.Max(1, settings.NumLayers - 2);

            var candidates = BuildSeedCandidates(settings, topLayer);

            for (var currentLayer = topLayer; currentLayer >= 1; currentLayer--)
            {
                candidates = OverlapAtLayer(settings, currentLayer, candidates, queryParams);

                if (currentLayer == 1)
                {
                    candidates.Sort();
                    return candidates;
                }

                // Subdivide every occupied cell into its 8 children at the next finer layer.
                var subdivided = new List<MortonCode>(candidates.Count * 8);
                foreach (var occ in candidates)
                {
                    for (var c = 0u; c < 8; c++)
                    {
                        subdivided.Add(occ.ChildCode(c));
                    }
                }
                candidates = subdivided;
            }

            // Unreachable because the loop returns when currentLayer == 1, but keeps the
            // compiler happy and guards against future refactors of the loop bounds.
            candidates.Sort();
            return candidates;
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

        /// <summary>
        /// Builds the seed set for the hierarchical sweep: every cell of the regular grid
        /// at <paramref name="topLayer"/>.
        /// </summary>
        static List<MortonCode> BuildSeedCandidates(BuildSettings settings, int topLayer)
        {
            // gridSize at layer L is 2^(NumLayers - 1 - L). At topLayer = NumLayers - 2 this is 2.
            var gridSize = 1 << (settings.NumLayers - 1 - topLayer);
            var total = gridSize * gridSize * gridSize;

            var seeds = new List<MortonCode>(total);
            for (var i = 0u; i < (uint)gridSize; i++)
            {
                for (var j = 0u; j < (uint)gridSize; j++)
                {
                    for (var k = 0u; k < (uint)gridSize; k++)
                    {
                        seeds.Add(new MortonCode(i, j, k));
                    }
                }
            }
            return seeds;
        }

        /// <summary>
        /// Runs a batched OverlapBox over <paramref name="candidates"/> at the given layer's
        /// cell size and returns the subset that hit geometry.
        /// </summary>
        static List<MortonCode> OverlapAtLayer(
            BuildSettings settings,
            int layer,
            List<MortonCode> candidates,
            QueryParameters queryParams
        )
        {
            if (candidates.Count == 0)
            {
                return candidates;
            }

            var cellSize = settings.NodeSizeForLayer(layer);
            var halfExtent = cellSize * 0.5f - _OVERLAP_BOX_SHRINK + settings.AgentRadius;

            var candidateArr = new NativeArray<MortonCode>(
                candidates.Count,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );
            for (var i = 0; i < candidates.Count; i++)
            {
                candidateArr[i] = candidates[i];
            }

            var commands = new NativeArray<OverlapBoxCommand>(
                candidates.Count,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );
            var results = new NativeArray<ColliderHit>(
                candidates.Count,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory
            );

            var fillHandle = new BuildLayerCommandsJob
            {
                Commands = commands,
                Candidates = candidateArr,
                Origin = settings.Origin,
                CellSize = cellSize,
                HalfExtents = new Vector3(halfExtent, halfExtent, halfExtent),
                QueryParams = queryParams,
            }.Schedule(candidates.Count, _PARALLEL_FOR_BATCH);

            var overlapHandle = OverlapBoxCommand.ScheduleBatch(
                commands,
                results,
                _OVERLAP_BATCH_MIN,
                1,
                fillHandle
            );
            overlapHandle.Complete();

            var occupied = new List<MortonCode>();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (results[i].instanceID != 0)
                {
                    occupied.Add(candidates[i]);
                }
            }

            candidateArr.Dispose();
            commands.Dispose();
            results.Dispose();

            return occupied;
        }

        [BurstCompile]
        struct BuildLayerCommandsJob : IJobParallelFor
        {
            [WriteOnly]
            public NativeArray<OverlapBoxCommand> Commands;

            [ReadOnly]
            public NativeArray<MortonCode> Candidates;

            public Vector3 Origin;
            public float CellSize;
            public Vector3 HalfExtents;
            public QueryParameters QueryParams;

            public void Execute(int index)
            {
                var (x, y, z) = Candidates[index].Decoded;

                var center =
                    Origin
                    + new Vector3(
                        (x + 0.5f) * CellSize,
                        (y + 0.5f) * CellSize,
                        (z + 0.5f) * CellSize
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
