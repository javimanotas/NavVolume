namespace NavVolume.Core
{
    /// <summary>
    /// The six axis-aligned face directions, used for neighbor lookups.
    /// </summary>
    /// <remarks>
    /// Even values match positive directions and odd values match negative directions.
    /// Axis order is X, Y, Z.
    /// </remarks>
    internal enum NeighborDirection
    {
        // DO NOT CHANGE THE ORDER OF THESE ENUM VALUES
        PosX = 0,
        NegX = 1,
        PosY = 2,
        NegY = 3,
        PosZ = 4,
        NegZ = 5,
    }
}
