using System;
using NavVolume.Runtime.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Burst-compatible queries against a <see cref="VoxelGrid"/>.
    /// </summary>
    internal static class VoxelGridQuery
    {
        /// <summary>
        /// Returns true when the voxel at the given finest-grid coordinates is occupied.
        /// Coordinates outside the grid count as free.
        /// </summary>
        public static bool IsVoxelOccupied(
            in VoxelGrid grid,
            NativeArray<uint> mortons,
            NativeArray<ulong> masks,
            int3 voxel
        )
        {
            var node = voxel >> 2;

            if (math.any(node < 0) || math.any(node >= grid.NodeGridDim))
            {
                return false;
            }

            var code = (uint)new MortonCode(node.x, node.y, node.z);

            if (!TryFindNode(mortons, grid.NodeStart, grid.NodeCount, code, out var nodeIndex))
            {
                return false;
            }

            var subnode = voxel & 3;

            // Bit layout mirrors SVOLeaf.SubnodeCoordsToIndex.
            var bit = (subnode.x << 4) | (subnode.y << 2) | subnode.z;
            return (masks[nodeIndex] & (1ul << bit)) != 0;
        }

        /// <summary>
        /// Gathers up to <paramref name="maxCount"/> occupied voxels nearest to <paramref name="position"/> within <paramref name="searchRange"/>.
        /// Writes the closest point on each voxel and its squared distance, sorted by ascending distance, and returns how many were found.
        /// </summary>
        public static int GatherNearestOccupiedVoxels(
            in VoxelGrid grid,
            NativeArray<uint> mortons,
            NativeArray<ulong> masks,
            float3 position,
            float searchRange,
            Span<float3> closestPoints,
            Span<float> distancesSq,
            int maxCount
        )
        {
            if (grid.NodeCount == 0 || maxCount <= 0 || searchRange <= 0f)
            {
                return 0;
            }

            var count = 0;
            var rangeSq = searchRange * searchRange;

            var minNode = math.clamp(
                (int3)math.floor((position - searchRange - grid.Origin) / grid.NodeSize),
                0,
                grid.NodeGridDim - 1
            );
            var maxNode = math.clamp(
                (int3)math.floor((position + searchRange - grid.Origin) / grid.NodeSize),
                0,
                grid.NodeGridDim - 1
            );

            for (var nx = minNode.x; nx <= maxNode.x; nx++)
            {
                for (var ny = minNode.y; ny <= maxNode.y; ny++)
                {
                    for (var nz = minNode.z; nz <= maxNode.z; nz++)
                    {
                        var nodeMin = grid.Origin + new float3(nx, ny, nz) * grid.NodeSize;
                        var nodeClosest = math.clamp(position, nodeMin, nodeMin + grid.NodeSize);
                        var nodeDistSq = math.distancesq(nodeClosest, position);

                        if (nodeDistSq > rangeSq)
                        {
                            continue;
                        }

                        if (count == maxCount && nodeDistSq >= distancesSq[count - 1])
                        {
                            continue;
                        }

                        var code = (uint)new MortonCode(nx, ny, nz);

                        if (
                            !TryFindNode(
                                mortons,
                                grid.NodeStart,
                                grid.NodeCount,
                                code,
                                out var nodeIndex
                            )
                        )
                        {
                            continue;
                        }

                        var mask = masks[nodeIndex];

                        while (mask != 0)
                        {
                            var bit = math.tzcnt(mask);
                            mask &= mask - 1;

                            var voxelMin =
                                nodeMin
                                + new float3((bit >> 4) & 3, (bit >> 2) & 3, bit & 3)
                                    * grid.VoxelSize;
                            var voxelMax = voxelMin + grid.VoxelSize;
                            var closest = math.clamp(position, voxelMin, voxelMax);
                            var distSq = math.distancesq(closest, position);

                            if (distSq > rangeSq)
                            {
                                continue;
                            }

                            if (distSq < 1e-12f)
                            {
                                // Inside the voxel
                                closest = (voxelMin + voxelMax) * 0.5f;
                                distSq = 0f;
                            }

                            Insert(
                                closestPoints,
                                distancesSq,
                                ref count,
                                maxCount,
                                closest,
                                distSq
                            );
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Binary search over the slice [start, start + count) of sorted morton codes.
        /// </summary>
        public static bool TryFindNode(
            NativeArray<uint> mortons,
            int start,
            int count,
            uint code,
            out int index
        )
        {
            var lo = start;
            var hi = start + count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var midCode = mortons[mid];

                if (midCode == code)
                {
                    index = mid;
                    return true;
                }

                if (midCode < code)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            index = -1;
            return false;
        }

        static void Insert(
            Span<float3> points,
            Span<float> distancesSq,
            ref int count,
            int maxCount,
            float3 point,
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
                points[i] = points[i - 1];
                i--;
            }

            distancesSq[i] = distSq;
            points[i] = point;
        }
    }
}
