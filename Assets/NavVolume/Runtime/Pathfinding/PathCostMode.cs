namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Selects how the A* g-cost (and its matching heuristic) is measured.
    /// </summary>
    public enum PathCostMode
    {
        /// <summary>
        /// Every node step costs 1 regardless of its size, biasing the path toward large nodes
        /// (open space) so waypoints stay clear of surfaces. The heuristic is measured in node hops.
        /// </summary>
        NodeCount,

        /// <summary>
        /// Each step costs the Euclidean distance between node centers, producing the geometrically
        /// shortest path, which hugs surfaces and corners. The heuristic is measured in meters.
        /// </summary>
        EuclideanDistance,
    }
}
