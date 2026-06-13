using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Shared cell hashing used to build and query the agent spatial hash.
    /// </summary>
    /// <remarks>
    /// Two distinct cells may collide into the same key; queries must distance-filter candidates,
    /// which the avoidance neighbor gathering does anyway.
    /// </remarks>
    internal static class SpatialHash
    {
        public static int CellKey(int3 cell) => (int)math.hash(cell);
    }
}
