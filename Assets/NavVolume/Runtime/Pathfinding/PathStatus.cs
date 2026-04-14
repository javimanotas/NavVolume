namespace NavVolume.Pathfinding
{
    /// <summary>
    /// Status code for a completed pathfinding query.
    /// </summary>
    internal enum PathStatus
    {
        Success,
        NoPathFound,
        InvalidEndpoint,
        NoTree,
        BudgetExceeded,
    }
}
