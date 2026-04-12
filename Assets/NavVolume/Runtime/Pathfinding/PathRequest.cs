using UnityEngine;

namespace NavVolume.Pathfinding
{
    /// <summary>
    /// Input parameters for a single A* query.
    /// </summary>
    internal struct PathRequest
    {
        public Vector3 Start;

        public Vector3 Goal;

        /// <summary>
        /// Greedy heuristic weight W.
        /// </summary>
        /// <remarks>
        /// See <see cref="SVOHeuristic"/> for details.
        /// </remarks>
        public float HeuristicWeight;

        /// <summary>
        /// Maximum number of nodes the search is allowed to pop off the open list before giving up.
        /// Prevents runaway searches in degenerate cases.
        /// 0 = unlimited.
        /// </summary>
        public int MaxNodesBudget;
    }
}
