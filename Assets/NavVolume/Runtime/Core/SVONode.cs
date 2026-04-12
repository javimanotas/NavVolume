namespace NavVolume.Core
{
    /// <summary>
    /// A non-leaf node in the Sparse Voxel Octree.
    /// </summary>
    internal struct SVONode
    {
        public readonly MortonCode MortonCode;

        public SVOLink FirstChild; // all 8 children are contiguous: NodeIdx + 0..7

        public SVOLink Parent;

        public NeighborSet Neighbors;

        public SVONode(MortonCode mortonCode)
        {
            MortonCode = mortonCode;
            Parent = SVOLink.Invalid;
            FirstChild = SVOLink.Invalid;
            Neighbors = NeighborSet.AllInvalid;
        }

        public readonly bool HasChildren => FirstChild.IsValid;
    }
}
