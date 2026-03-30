namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Then six neighbor links of a node.
    /// </summary>
    internal struct NeighborSet
    {
        SVOLink PosX,
            NegX,
            PosY,
            NegY,
            PosZ,
            NegZ;

        public static NeighborSet AllInvalid =>
            new()
            {
                PosX = SVOLink.Invalid,
                NegX = SVOLink.Invalid,
                PosY = SVOLink.Invalid,
                NegY = SVOLink.Invalid,
                PosZ = SVOLink.Invalid,
                NegZ = SVOLink.Invalid,
            };

        public SVOLink this[NeighborDirection index]
        {
            readonly get =>
                index switch
                {
                    NeighborDirection.PosX => PosX,
                    NeighborDirection.NegX => NegX,
                    NeighborDirection.PosY => PosY,
                    NeighborDirection.NegY => NegY,
                    NeighborDirection.PosZ => PosZ,
                    NeighborDirection.NegZ => NegZ,
                    _ => SVOLink.Invalid,
                };
            set
            {
                switch (index)
                {
                    case NeighborDirection.PosX:
                        PosX = value;
                        break;
                    case NeighborDirection.NegX:
                        NegX = value;
                        break;
                    case NeighborDirection.PosY:
                        PosY = value;
                        break;
                    case NeighborDirection.NegY:
                        NegY = value;
                        break;
                    case NeighborDirection.PosZ:
                        PosZ = value;
                        break;
                    case NeighborDirection.NegZ:
                        NegZ = value;
                        break;
                }
            }
        }
    }
}
