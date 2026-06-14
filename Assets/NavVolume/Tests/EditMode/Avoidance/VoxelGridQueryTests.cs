using System;
using System.Collections.Generic;
using NavVolume.Runtime.Avoidance;
using NavVolume.Runtime.Core;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace NavVolume.Tests.EditMode.Avoidance
{
    public class VoxelGridQueryTests
    {
        #region Auxiliary

        const float _TOLERANCE = 1e-4f;

        NativeArray<uint> _mortons;
        NativeArray<ulong> _masks;

        [TearDown]
        public void TearDown()
        {
            if (_mortons.IsCreated)
            {
                _mortons.Dispose();
            }

            if (_masks.IsCreated)
            {
                _masks.Dispose();
            }
        }

        /// <summary>
        /// Builds a 2x2x2-node grid (8 voxels per axis, voxel size 1, origin at zero) with the given occupied voxels, using the real SVO leaf layout.
        /// </summary>
        VoxelGrid BuildGrid(params int3[] occupiedVoxels)
        {
            var nodes = new SortedList<uint, SVOLeaf>();

            foreach (var voxel in occupiedVoxels)
            {
                var node = voxel >> 2;
                var code = (uint)new MortonCode(node.x, node.y, node.z);

                if (!nodes.TryGetValue(code, out var leaf))
                {
                    leaf = SVOLeaf.Empty;
                }

                leaf.SetOccupied(voxel.x & 3, voxel.y & 3, voxel.z & 3);
                nodes[code] = leaf;
            }

            _mortons = new NativeArray<uint>(nodes.Count, Allocator.Temp);
            _masks = new NativeArray<ulong>(nodes.Count, Allocator.Temp);

            var i = 0;

            foreach (var pair in nodes)
            {
                _mortons[i] = pair.Key;
                _masks[i] = pair.Value.RawBits;
                i++;
            }

            return new()
            {
                NodeStart = 0,
                NodeCount = nodes.Count,
                NodeGridDim = 2,
                Origin = float3.zero,
                VoxelSize = 1f,
                NodeSize = 4f,
            };
        }

        #endregion

        [Test]
        public void IsVoxelOccupied_WithOccupiedVoxel_ShouldReturnTrue()
        {
            var grid = BuildGrid(new int3(2, 0, 0), new int3(4, 5, 6));

            Assert.IsTrue(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(2, 0, 0))
            );
            Assert.IsTrue(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(4, 5, 6))
            );
        }

        [Test]
        public void IsVoxelOccupied_WithFreeVoxel_ShouldReturnFalse()
        {
            var grid = BuildGrid(new int3(2, 0, 0));

            Assert.IsFalse(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(0, 0, 0))
            );
            Assert.IsFalse(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(7, 7, 7))
            );
        }

        [Test]
        public void IsVoxelOccupied_OutsideGrid_ShouldReturnFalse()
        {
            var grid = BuildGrid(new int3(2, 0, 0));

            Assert.IsFalse(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(-1, 0, 0))
            );
            Assert.IsFalse(
                VoxelGridQuery.IsVoxelOccupied(in grid, _mortons, _masks, new int3(8, 0, 0))
            );
        }

        [Test]
        public void GatherNearestOccupiedVoxels_ShouldReturnClosestPointAndDistance()
        {
            // Voxel (2,0,0) spans [2..3]x[0..1]x[0..1]; from (3.5, 0.5, 0.5) the closest point is
            // the face at x = 3.
            var grid = BuildGrid(new int3(2, 0, 0));
            var position = new float3(3.5f, 0.5f, 0.5f);

            Span<float3> points = stackalloc float3[4];
            Span<float> distancesSq = stackalloc float[4];
            var found = VoxelGridQuery.GatherNearestOccupiedVoxels(
                in grid,
                _mortons,
                _masks,
                position,
                4f,
                points,
                distancesSq,
                4
            );

            Assert.AreEqual(1, found);
            Assert.Less(math.distance(points[0], new float3(3f, 0.5f, 0.5f)), _TOLERANCE);
            Assert.AreEqual(0.25f, distancesSq[0], _TOLERANCE);
        }

        [Test]
        public void GatherNearestOccupiedVoxels_ShouldSortByDistanceAndCap()
        {
            var grid = BuildGrid(new int3(0, 0, 0), new int3(2, 0, 0), new int3(0, 2, 0));
            var position = new float3(3.5f, 0.5f, 0.5f);

            Span<float3> points = stackalloc float3[2];
            Span<float> distancesSq = stackalloc float[2];
            var found = VoxelGridQuery.GatherNearestOccupiedVoxels(
                in grid,
                _mortons,
                _masks,
                position,
                8f,
                points,
                distancesSq,
                2
            );

            // Distances: voxel (2,0,0) -> 0.25, voxel (0,0,0) -> 6.25, voxel (0,2,0) -> 8.5.
            Assert.AreEqual(2, found);
            Assert.AreEqual(0.25f, distancesSq[0], _TOLERANCE);
            Assert.AreEqual(6.25f, distancesSq[1], _TOLERANCE);
            Assert.LessOrEqual(distancesSq[0], distancesSq[1]);
        }

        [Test]
        public void GatherNearestOccupiedVoxels_WhenInsideVoxel_ShouldReturnCenterAtZeroDistance()
        {
            var grid = BuildGrid(new int3(1, 1, 1));
            var position = new float3(1.2f, 1.7f, 1.4f);

            Span<float3> points = stackalloc float3[4];
            Span<float> distancesSq = stackalloc float[4];
            var found = VoxelGridQuery.GatherNearestOccupiedVoxels(
                in grid,
                _mortons,
                _masks,
                position,
                2f,
                points,
                distancesSq,
                4
            );

            Assert.AreEqual(1, found);
            Assert.AreEqual(0f, distancesSq[0], _TOLERANCE);
            Assert.Less(math.distance(points[0], new float3(1.5f, 1.5f, 1.5f)), _TOLERANCE);
        }
    }
}
