namespace NavVolume.Core
{
    /// <summary>
    /// The six neighbor links of a node.
    /// </summary>
    internal struct NeighborSet
    {
        // An array implementation would be cleaner but it would imply more runtime overhead.
        SVOLink _posX,
            _negX,
            _posY,
            _negY,
            _posZ,
            _negZ;

        public static NeighborSet AllInvalid =>
            new()
            {
                _posX = SVOLink.Invalid,
                _negX = SVOLink.Invalid,
                _posY = SVOLink.Invalid,
                _negY = SVOLink.Invalid,
                _posZ = SVOLink.Invalid,
                _negZ = SVOLink.Invalid,
            };

        public SVOLink this[NeighborDirection index]
        {
            readonly get =>
                index switch
                {
                    NeighborDirection.PosX => _posX,
                    NeighborDirection.NegX => _negX,
                    NeighborDirection.PosY => _posY,
                    NeighborDirection.NegY => _negY,
                    NeighborDirection.PosZ => _posZ,
                    NeighborDirection.NegZ => _negZ,
                    _ => SVOLink.Invalid,
                };
            set
            {
                switch (index)
                {
                    case NeighborDirection.PosX:
                        _posX = value;
                        break;
                    case NeighborDirection.NegX:
                        _negX = value;
                        break;
                    case NeighborDirection.PosY:
                        _posY = value;
                        break;
                    case NeighborDirection.NegY:
                        _negY = value;
                        break;
                    case NeighborDirection.PosZ:
                        _posZ = value;
                        break;
                    case NeighborDirection.NegZ:
                        _negZ = value;
                        break;
                }
            }
        }
    }
}
