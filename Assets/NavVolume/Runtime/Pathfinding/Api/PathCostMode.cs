namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Selects how the A* g-cost (and its matching heuristic) is measured.
    /// </summary>
    internal enum PathCostMode
    {
        /// <summary>
        /// Every node step costs 1 regardless of its size, biasing the path toward large nodes (open space).
        /// </summary>
        NodeCount,

        /// <summary>
        /// Each step costs the Euclidean distance between node centers, producing the geometrically shortest path.
        /// </summary>
        EuclideanDistance,
    }
}
