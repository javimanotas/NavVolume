namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// The six axis-aligned face directions, used for neighbor lookups.
    /// </summary>
    internal enum NeighborDirection : int
    {
        PosX = 0,
        NegX = 1,
        PosY = 2,
        NegY = 3,
        PosZ = 4,
        NegZ = 5,
    }
}
