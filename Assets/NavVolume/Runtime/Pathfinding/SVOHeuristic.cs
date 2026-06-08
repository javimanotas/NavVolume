using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Traversal-cost and heuristic helpers for the A* search.
    /// </summary>
    /// <remarks>
    /// The g-cost and the heuristic must share a unit, otherwise <c>f = g + h</c> is meaningless. Two
    /// consistent pairings are used, selected by <see cref="PathCostMode"/>:
    /// <list type="bullet">
    /// <item><see cref="UnitCost"/> (one per hop) pairs with a heuristic scaled by
    /// <c>weight / largestNodeSize</c>, which re-expresses the straight-line distance as a hop count
    /// (a single hop can never span more than the largest node, so it stays admissible).</item>
    /// <item><see cref="EuclideanCost"/> (meters per hop) pairs with a heuristic scaled by
    /// <c>weight</c> (meters).</item>
    /// </list>
    /// The pathfinder folds that scale into a single factor once per query and feeds it to
    /// <see cref="ScaledDistance"/>, keeping the inner loop down to one distance and one multiply. A
    /// weight of 1 stays admissible; larger weights search more greedily (faster, less optimal).
    /// </remarks>
    internal static class SVOHeuristic
    {
        /// <summary>
        /// Unit traversal cost: every hop costs 1 regardless of node size, which biases the path
        /// toward large nodes (open space) and keeps waypoints clear of surfaces.
        /// </summary>
        public static float UnitCost() => 1f;

        /// <summary>
        /// Distance traversal cost: a hop costs the Euclidean distance between the two node centers.
        /// </summary>
        public static float EuclideanCost(Vector3 fromCenter, Vector3 toCenter) =>
            Vector3.Distance(fromCenter, toCenter);

        /// <summary>
        /// Weighted heuristic: the straight-line distance to the goal times a precomputed
        /// <paramref name="scale"/> (see the type remarks for how the scale is derived per cost mode).
        /// </summary>
        public static float ScaledDistance(Vector3 current, Vector3 goal, float scale) =>
            Vector3.Distance(current, goal) * scale;
    }
}
