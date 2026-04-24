using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Input parameters for a single A* query.
    /// </summary>
    internal readonly struct PathRequest
    {
        public readonly Vector3 Start;

        public readonly Vector3 Goal;

        /// <summary>
        /// Greedy heuristic weight W.
        /// </summary>
        /// <remarks>
        /// See <see cref="SVOHeuristic"/> for details.
        /// </remarks>
        public readonly float HeuristicWeight;

        /// <summary>
        /// Maximum number of nodes the search is allowed to pop off the open list before giving up.
        /// Prevents runaway searches in degenerate cases.
        /// 0 = unlimited.
        /// </summary>
        public readonly int MaxNodesBudget;

        public PathRequest(
            Vector3 start,
            Vector3 goal,
            float heuristicWeight,
            int maxNodesBudget = 0
        )
        {
            Start = start;
            Goal = goal;
            HeuristicWeight = heuristicWeight;
            MaxNodesBudget = maxNodesBudget;
        }
    }
}
