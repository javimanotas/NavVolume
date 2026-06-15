using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Traversal-cost and heuristic helpers for the A* search.
    /// </summary>
    internal static class SVOHeuristic
    {
        /// <summary>
        /// Unit traversal cost: every hop costs 1 regardless of node size, which biases the path toward large nodes (open space).
        /// </summary>
        public static float UnitCost() => 1f;

        /// <summary>
        /// Distance traversal cost: a hop costs the Euclidean distance between the two node centers.
        /// </summary>
        public static float EuclideanCost(Vector3 fromCenter, Vector3 toCenter) =>
            Vector3.Distance(fromCenter, toCenter);

        /// <summary>
        /// Weighted heuristic: the straight-line distance to the goal times a precomputed scale.
        /// </summary>
        public static float ScaledDistance(Vector3 current, Vector3 goal, float scale) =>
            Vector3.Distance(current, goal) * scale;
    }
}
