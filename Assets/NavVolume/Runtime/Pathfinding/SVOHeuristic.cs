using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// All heuristic and traversal cost functions for A* search.
    /// </summary>
    internal static class SVOHeuristic
    {
        /// <summary>
        /// Unit traversal cost: every node step costs exactly 1.0 regardless of the node's physical size.
        /// </summary>
        /// <remarks>
        /// Counting nodes instead of accumulating distance biases the search toward large nodes, which
        /// only exist in open space, so the resulting path keeps clear of surfaces.
        /// </remarks>
        public static float UnitCost() => 1f;

        /// <summary>
        /// Weighted heuristic expressed in the same unit as <see cref="UnitCost"/>: an estimate of the
        /// number of node hops still needed to reach the goal.
        /// </summary>
        /// <remarks>
        /// The straight-line distance is divided by <paramref name="largestNodeSize"/> because a single
        /// hop can never span more than the largest node, so the quotient is an admissible lower bound on
        /// the remaining hop count. Keeping the heuristic in hop units (instead of meters) is what makes it
        /// commensurable with the g-cost. The greater the <paramref name="weight"/>, the more greedily the
        /// search behaves (faster but less optimal); a weight of 1 stays admissible.
        /// </remarks>
        public static float NodeCountHeuristic(
            Vector3 current,
            Vector3 goal,
            float largestNodeSize,
            float weight
        ) => Vector3.Distance(current, goal) / largestNodeSize * weight;
    }
}
