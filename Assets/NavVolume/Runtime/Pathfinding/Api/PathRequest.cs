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
        public readonly float HeuristicWeight;

        /// <summary>
        /// Maximum number of nodes the search is allowed to pop off the open list before giving up.
        /// </summary>
        /// <remarks>
        /// 0 = unlimited.
        /// </remarks>
        public readonly int MaxNodesBudget;

        /// <summary>
        /// How the g-cost and heuristic are measured.
        /// </summary>
        public readonly PathCostMode CostMode;

        public PathRequest(
            Vector3 start,
            Vector3 goal,
            float heuristicWeight,
            int maxNodesBudget = 0,
            PathCostMode costMode = PathCostMode.NodeCount
        )
        {
            Start = start;
            Goal = goal;
            HeuristicWeight = heuristicWeight;
            MaxNodesBudget = maxNodesBudget;
            CostMode = costMode;
        }
    }
}
