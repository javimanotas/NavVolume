using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Shared cell hashing used to build and query the agent spatial hash.
    /// </summary>
    internal static class SpatialHash
    {
        public static int CellKey(int3 cell) => (int)math.hash(cell);
    }
}
