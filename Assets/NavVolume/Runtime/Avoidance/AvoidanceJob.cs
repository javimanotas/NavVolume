using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Computes one ORCA step for every agent in parallel.
    /// </summary>
    /// <remarks>
    /// For each agent the job builds the constraint planes in a fixed order (nearest baked voxels, then nearest obstacles, then nearest agents).
    /// Then solves the linear program for the velocity closest to the preferred one.
    /// </remarks>
    [BurstCompile]
    internal struct AvoidanceJob : IJobParallelFor
    {
        public const int MAX_NEIGHBORS = 16;

        public const int MAX_OBSTACLE_PLANES = 8;

        public const int MAX_VOXEL_PLANES = 8;

        public const int MAX_PLANES = MAX_NEIGHBORS + MAX_OBSTACLE_PLANES + MAX_VOXEL_PLANES;

        /// <summary>
        /// Upper bound of the voxel search radius in layer-0 nodes.
        /// </summary>
        const float _MAX_VOXEL_SEARCH_NODES = 2.5f;

        [ReadOnly]
        public NativeArray<AvoidanceAgentState> Agents;

        [ReadOnly]
        public NativeArray<AvoidanceObstacleState> Obstacles;

        [ReadOnly]
        public NativeParallelMultiHashMap<int, int> Hash;

        [ReadOnly]
        public NativeArray<VoxelGrid> VoxelGrids;

        [ReadOnly]
        public NativeArray<uint> VoxelMortons;

        [ReadOnly]
        public NativeArray<ulong> VoxelMasks;

        public float CellSize;

        public float TimeStep;

        [WriteOnly]
        public NativeArray<float3> NewVelocities;

        public void Execute(int index)
        {
            var agent = Agents[index];

            Span<OrcaPlane> planes = stackalloc OrcaPlane[MAX_PLANES];
            Span<OrcaPlane> scratch = stackalloc OrcaPlane[MAX_PLANES];
            var planeCount = 0;

            AddVoxelPlanes(in agent, planes, ref planeCount);
            AddObstaclePlanes(in agent, planes, ref planeCount);
            var staticPlaneCount = planeCount;
            AddAgentPlanes(index, in agent, planes, ref planeCount);

            NewVelocities[index] = OrcaSolver.Solve(
                planes,
                planeCount,
                staticPlaneCount,
                agent.MaxSpeed,
                agent.PrefVelocity,
                scratch
            );
        }

        #region Baked volume constraints

        void AddVoxelPlanes(
            in AvoidanceAgentState agent,
            Span<OrcaPlane> planes,
            ref int planeCount
        )
        {
            if (agent.SpaceIndex < 0 || agent.SpaceIndex >= VoxelGrids.Length)
            {
                return;
            }

            var grid = VoxelGrids[agent.SpaceIndex];

            if (grid.NodeCount == 0)
            {
                return;
            }

            var searchRange = math.min(
                agent.Radius + agent.MaxSpeed * agent.TimeHorizonObstacles,
                _MAX_VOXEL_SEARCH_NODES * grid.NodeSize
            );

            Span<float3> closestPoints = stackalloc float3[MAX_VOXEL_PLANES];
            Span<float> distancesSq = stackalloc float[MAX_VOXEL_PLANES];
            var found = VoxelGridQuery.GatherNearestOccupiedVoxels(
                in grid,
                VoxelMortons,
                VoxelMasks,
                agent.Position,
                searchRange,
                closestPoints,
                distancesSq,
                MAX_VOXEL_PLANES
            );

            for (var i = 0; i < found; i++)
            {
                var relativePosition = closestPoints[i] - agent.Position;

                if (math.lengthsq(relativePosition) < 1e-12f)
                {
                    // Exactly at the voxel center: push back the way the agent came.
                    relativePosition =
                        math.normalizesafe(agent.Velocity, new float3(0f, 1f, 0f)) * 1e-3f;
                }

                planes[planeCount++] = OrcaMath.StaticPlane(
                    relativePosition,
                    agent.Radius,
                    agent.Velocity,
                    float3.zero,
                    agent.TimeHorizonObstacles,
                    TimeStep
                );
            }
        }

        #endregion

        #region Obstacle constraints

        void AddObstaclePlanes(
            in AvoidanceAgentState agent,
            Span<OrcaPlane> planes,
            ref int planeCount
        )
        {
            if (Obstacles.Length == 0)
            {
                return;
            }

            var searchRange = agent.Radius + agent.MaxSpeed * agent.TimeHorizonObstacles;

            // Keep only the nearest obstacles when there are more than fit in the plane budget.
            Span<int> nearest = stackalloc int[MAX_OBSTACLE_PLANES];
            Span<float> nearestDistSq = stackalloc float[MAX_OBSTACLE_PLANES];
            var count = 0;

            for (var i = 0; i < Obstacles.Length; i++)
            {
                var reach = searchRange + Obstacles[i].BoundingRadius;
                var distSq = math.distancesq(Obstacles[i].Position, agent.Position);

                if (distSq > reach * reach)
                {
                    continue;
                }

                InsertNearest(nearest, nearestDistSq, ref count, MAX_OBSTACLE_PLANES, i, distSq);
            }

            for (var n = 0; n < count; n++)
            {
                var obstacle = Obstacles[nearest[n]];
                float3 relativePosition;
                float combinedRadius;

                if (obstacle.Shape == ObstacleShape.Sphere)
                {
                    relativePosition = obstacle.Position - agent.Position;
                    combinedRadius = obstacle.HalfExtents.x + agent.Radius;
                }
                else
                {
                    relativePosition = BoxRelativePosition(in agent, in obstacle);
                    combinedRadius = agent.Radius;
                }

                planes[planeCount++] = OrcaMath.StaticPlane(
                    relativePosition,
                    combinedRadius,
                    agent.Velocity,
                    obstacle.Velocity,
                    agent.TimeHorizonObstacles,
                    TimeStep
                );
            }
        }

        /// <summary>
        /// Vector from the agent to the point of the box it could collide with: the closest surface point when outside, or a virtual point placed so the ORCA collision branch pushes the agent out through the nearest face when inside.
        /// </summary>
        static float3 BoxRelativePosition(
            in AvoidanceAgentState agent,
            in AvoidanceObstacleState obstacle
        )
        {
            var local = math.rotate(
                math.conjugate(obstacle.Rotation),
                agent.Position - obstacle.Position
            );
            var clamped = math.clamp(local, -obstacle.HalfExtents, obstacle.HalfExtents);

            if (!math.all(local == clamped))
            {
                var closest = obstacle.Position + math.rotate(obstacle.Rotation, clamped);
                return closest - agent.Position;
            }

            // Inside the box: exit through the nearest face.
            var distToFace = obstacle.HalfExtents - math.abs(local);

            if (distToFace.x <= distToFace.y && distToFace.x <= distToFace.z)
            {
                clamped.x = local.x >= 0f ? obstacle.HalfExtents.x : -obstacle.HalfExtents.x;
            }
            else if (distToFace.y <= distToFace.z)
            {
                clamped.y = local.y >= 0f ? obstacle.HalfExtents.y : -obstacle.HalfExtents.y;
            }
            else
            {
                clamped.z = local.z >= 0f ? obstacle.HalfExtents.z : -obstacle.HalfExtents.z;
            }

            var surface = obstacle.Position + math.rotate(obstacle.Rotation, clamped);
            var exitDirection = math.normalizesafe(
                surface - agent.Position,
                new float3(0f, 1f, 0f)
            );

            // Pointing away from the exit keeps the constraint in the collision branch, which
            // pushes the velocity toward the surface at escape speed.
            return -exitDirection * (agent.Radius * 0.5f);
        }

        #endregion

        #region Agent constraints

        void AddAgentPlanes(
            int index,
            in AvoidanceAgentState agent,
            Span<OrcaPlane> planes,
            ref int planeCount
        )
        {
            var maxNeighbors = math.min(agent.MaxNeighbors, MAX_NEIGHBORS);

            if (maxNeighbors <= 0 || agent.NeighborRange <= 0f)
            {
                return;
            }

            Span<int> neighbors = stackalloc int[MAX_NEIGHBORS];
            Span<float> neighborDistSq = stackalloc float[MAX_NEIGHBORS];
            var count = 0;
            var rangeSq = agent.NeighborRange * agent.NeighborRange;

            var invCellSize = 1f / CellSize;
            var minCell = (int3)math.floor((agent.Position - agent.NeighborRange) * invCellSize);
            var maxCell = (int3)math.floor((agent.Position + agent.NeighborRange) * invCellSize);

            for (var x = minCell.x; x <= maxCell.x; x++)
            {
                for (var y = minCell.y; y <= maxCell.y; y++)
                {
                    for (var z = minCell.z; z <= maxCell.z; z++)
                    {
                        var key = SpatialHash.CellKey(new int3(x, y, z));

                        if (!Hash.TryGetFirstValue(key, out var other, out var iterator))
                        {
                            continue;
                        }

                        do
                        {
                            if (other == index)
                            {
                                continue;
                            }

                            var distSq = math.distancesq(Agents[other].Position, agent.Position);

                            if (distSq >= rangeSq)
                            {
                                continue;
                            }

                            InsertNearest(
                                neighbors,
                                neighborDistSq,
                                ref count,
                                maxNeighbors,
                                other,
                                distSq
                            );

                            if (count == maxNeighbors)
                            {
                                // Shrink the search to the farthest kept neighbor.
                                rangeSq = neighborDistSq[count - 1];
                            }
                        } while (Hash.TryGetNextValue(out other, ref iterator));
                    }
                }
            }

            for (var n = 0; n < count; n++)
            {
                var other = Agents[neighbors[n]];

                planes[planeCount++] = OrcaMath.AgentPlane(
                    agent.Position,
                    agent.Velocity,
                    agent.Radius,
                    other.Position,
                    other.Velocity,
                    other.Radius,
                    agent.TimeHorizonAgents,
                    TimeStep
                );
            }
        }

        #endregion

        /// <summary>
        /// Inserts a candidate into the parallel index/distance buffers, kept sorted by ascending distance and capped at <paramref name="maxCount"/> entries.
        /// </summary>
        static void InsertNearest(
            Span<int> indices,
            Span<float> distancesSq,
            ref int count,
            int maxCount,
            int candidate,
            float distSq
        )
        {
            if (count == maxCount && distSq >= distancesSq[count - 1])
            {
                return;
            }

            if (count < maxCount)
            {
                count++;
            }

            var i = count - 1;

            while (i > 0 && distancesSq[i - 1] > distSq)
            {
                distancesSq[i] = distancesSq[i - 1];
                indices[i] = indices[i - 1];
                i--;
            }

            distancesSq[i] = distSq;
            indices[i] = candidate;
        }
    }
}
