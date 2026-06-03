namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Per-query metrics captured during a path search.
    /// </summary>
    internal readonly struct PathStats
    {
        /// <summary>
        /// Number of A* search nodes popped from the open list before the search terminated.
        /// </summary>
        public readonly int NodesExpanded;

        /// <summary>
        /// Wall-clock time taken by <see cref="SVOPathfinder.FindPath"/> plus the smoothing passes.
        /// </summary>
        public readonly double ElapsedMs;

        /// <summary>
        /// Number of intermediate waypoints removed by the line-of-sight greedy shortcut step.
        /// </summary>
        public readonly int WaypointsRemovedByLOS;

        public PathStats(int nodesExpanded, double elapsedMs, int waypointsRemovedByLOS)
        {
            NodesExpanded = nodesExpanded;
            ElapsedMs = elapsedMs;
            WaypointsRemovedByLOS = waypointsRemovedByLOS;
        }
    }
}
