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
        /// This biases the search toward large nodes.
        /// </remarks>
        public static float UnitCost() => 1f;

        public static float EuclideanCost(Vector3 fromCenter, Vector3 toCenter) =>
            Vector3.Distance(fromCenter, toCenter);

        /// <summary>
        /// Standard weighted Euclidean heuristic.
        /// </summary>
        /// <remarks>
        /// The greater the weight, the more greedily the search behaves (faster but less optimal).
        /// </remarks>
        public static float EuclideanHeuristic(Vector3 current, Vector3 goal, float weight) =>
            Vector3.Distance(current, goal) * weight;
    }
}
