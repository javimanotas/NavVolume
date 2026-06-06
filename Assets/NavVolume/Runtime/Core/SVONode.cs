using System;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// A non-leaf node in the Sparse Voxel Octree.
    /// </summary>
    internal struct SVONode : IComparable<SVONode>
    {
        public readonly MortonCode MortonCode;

        public SVOLink FirstChild; // all 8 children are contiguous: NodeIdx + 0..7

        public SVOLink Parent;

        public NeighborSet Neighbors;

        /// <summary>
        /// Raw constructor.
        /// </summary>
        public SVONode(
            MortonCode mortonCode,
            SVOLink firstChild,
            SVOLink parent,
            SVOLink posXNeighbor,
            SVOLink negXNeighbor,
            SVOLink posYNeighbor,
            SVOLink negYNeighbor,
            SVOLink posZNeighbor,
            SVOLink negZNeighbor
        )
        {
            MortonCode = mortonCode;
            FirstChild = firstChild;
            Parent = parent;
            Neighbors = NeighborSet.AllInvalid;

            Neighbors[NeighborDirection.PosX] = posXNeighbor;
            Neighbors[NeighborDirection.NegX] = negXNeighbor;
            Neighbors[NeighborDirection.PosY] = posYNeighbor;
            Neighbors[NeighborDirection.NegY] = negYNeighbor;
            Neighbors[NeighborDirection.PosZ] = posZNeighbor;
            Neighbors[NeighborDirection.NegZ] = negZNeighbor;
        }

        public SVONode(MortonCode mortonCode)
        {
            MortonCode = mortonCode;
            Parent = SVOLink.Invalid;
            FirstChild = SVOLink.Invalid;
            Neighbors = NeighborSet.AllInvalid;
        }

        public readonly bool HasChildren => FirstChild.IsValid;

        public readonly int CompareTo(SVONode other) => MortonCode.CompareTo(other.MortonCode);

        #region Operators and overrides

        public readonly void Deconstruct(
            out MortonCode mortonCode,
            out SVOLink firstChild,
            out SVOLink parent,
            out SVOLink posXNeighbor,
            out SVOLink negXNeighbor,
            out SVOLink posYNeighbor,
            out SVOLink negYNeighbor,
            out SVOLink posZNeighbor,
            out SVOLink negZNeighbor
        )
        {
            mortonCode = MortonCode;
            firstChild = FirstChild;
            parent = Parent;
            posXNeighbor = Neighbors[NeighborDirection.PosX];
            negXNeighbor = Neighbors[NeighborDirection.NegX];
            posYNeighbor = Neighbors[NeighborDirection.PosY];
            negYNeighbor = Neighbors[NeighborDirection.NegY];
            posZNeighbor = Neighbors[NeighborDirection.PosZ];
            negZNeighbor = Neighbors[NeighborDirection.NegZ];
        }

        #endregion
    }
}
