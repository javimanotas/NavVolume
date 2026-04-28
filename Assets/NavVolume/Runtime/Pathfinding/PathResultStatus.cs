namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Status code for a completed pathfinding query.
    /// </summary>
    internal enum PathResultStatus
    {
        Sucess,
        NoPathFound,
        InvalidEndpoint,
        NoTree,
        BudgetExceeded,
    }
}
