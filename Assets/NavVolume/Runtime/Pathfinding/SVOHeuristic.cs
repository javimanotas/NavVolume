using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// All heuristic and traversal cost functions for A* search.
    /// </summary>
    /// <remarks>
    /// The g-cost and the heuristic must share a unit, otherwise <c>f = g + h</c> is meaningless.
    /// Two consistent pairs are provided, selected by <see cref="PathCostMode"/>: <see cref="UnitCost"/>
    /// pairs with <see cref="NodeCountHeuristic"/> (node hops), and <see cref="EuclideanCost"/> pairs
    /// with <see cref="EuclideanHeuristic"/> (meters).
    /// </remarks>
    internal static class SVOHeuristic
    {
        /// <summary>
        /// Unit traversal cost: every node step costs exactly 1.0 regardless of the node's physical size.
        /// </summary>
        /// <remarks>
        /// Counting nodes instead of accumulating distance biases the search toward large nodes, which
        /// only exist in open space, so the resulting path keeps clear of surfaces. Pair with
        /// <see cref="NodeCountHeuristic"/>.
        /// </remarks>
        public static float UnitCost() => 1f;

        /// <summary>
        /// Distance traversal cost: a step costs the Euclidean distance between the two node centers.
        /// </summary>
        /// <remarks>
        /// Accumulating distance yields the geometrically shortest path, which hugs surfaces and corners.
        /// Pair with <see cref="EuclideanHeuristic"/>.
        /// </remarks>
        public static float EuclideanCost(Vector3 fromCenter, Vector3 toCenter) =>
            Vector3.Distance(fromCenter, toCenter);

        /// <summary>
        /// Weighted heuristic in node-hop units: an estimate of the number of node hops still needed to
        /// reach the goal. Pairs with <see cref="UnitCost"/>.
        /// </summary>
        /// <remarks>
        /// The straight-line distance is divided by <paramref name="largestNodeSize"/> because a single
        /// hop can never span more than the largest node, so the quotient is an admissible lower bound on
        /// the remaining hop count. The greater the <paramref name="weight"/>, the more greedily the
        /// search behaves (faster but less optimal); a weight of 1 stays admissible.
        /// </remarks>
        public static float NodeCountHeuristic(
            Vector3 current,
            Vector3 goal,
            float largestNodeSize,
            float weight
        ) => Vector3.Distance(current, goal) / largestNodeSize * weight;

        /// <summary>
        /// Standard weighted Euclidean heuristic in meters. Pairs with <see cref="EuclideanCost"/>.
        /// </summary>
        /// <remarks>
        /// The greater the <paramref name="weight"/>, the more greedily the search behaves (faster but
        /// less optimal); a weight of 1 stays admissible.
        /// </remarks>
        public static float EuclideanHeuristic(Vector3 current, Vector3 goal, float weight) =>
            Vector3.Distance(current, goal) * weight;
    }
}
