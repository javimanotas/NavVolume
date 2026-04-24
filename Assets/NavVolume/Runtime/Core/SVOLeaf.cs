using System;
using UnityEngine;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Defines a 4x4x4 subnode grid where each subnode corresponds to a voxel.
    /// </summary>
    [Serializable]
    internal struct SVOLeaf
    {
        public static SVOLeaf Empty => new() { _occupiedSubnodes = 0 };

        /// <summary>
        /// Bitset of the occupied subnodes.
        /// </summary>
        [SerializeField]
        ulong _occupiedSubnodes;

        public const int GRID_SIZE = 4;

        public const int NUM_VOXELS = GRID_SIZE * GRID_SIZE * GRID_SIZE;

        // TODO: consider doing some assertions
        public static int SubnodeCoordsToIndex(int x, int y, int z) => (x << 4) | (y << 2) | z;

        public static (int, int, int) IndexToSubnodeCoords(int n)
        {
            var x = (n >> 4) & 0b11;
            var y = (n >> 2) & 0b11;
            var z = n & 0b11;
            return (x, y, z);
        }

        public readonly bool IsEmpty => _occupiedSubnodes == 0;

        public readonly bool IsFull => _occupiedSubnodes == ulong.MaxValue;

        public readonly bool IsOccupied(int index) => (_occupiedSubnodes & (1ul << index)) != 0;

        public void SetOccupied(int x, int y, int z)
        {
            _occupiedSubnodes |= 1ul << SubnodeCoordsToIndex(x, y, z);
        }
    }
}
