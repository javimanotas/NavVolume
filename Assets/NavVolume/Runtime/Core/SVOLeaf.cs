namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Defines a 4x4x4 subnode grid where each subnode corresponds to a voxel.
    /// </summary>
    internal struct SVOLeaf
    {
        /// <summary>
        /// Bitset of the occupied subnodes.
        /// </summary>
        ulong _occupiedSubnodes;

        #region Helper functions

        static int CoordsToIndex(int x, int y, int z) => (x << 4) | (y << 2) | z;

        #endregion

        public const int GRID_SIZE = 4;

        public static SVOLeaf Empty => new() { _occupiedSubnodes = 0 };

        public void SetOccupied(int x, int y, int z)
        {
            _occupiedSubnodes |= 1u << CoordsToIndex(x, y, z);
        }

        public readonly bool IsOccupied(int x, int y, int z) =>
            (_occupiedSubnodes & (1u << CoordsToIndex(x, y, z))) != 0;
    }
}
