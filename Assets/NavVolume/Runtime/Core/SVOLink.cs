using System;
using UnityEngine.Assertions;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Pointer to an arbitrary node or voxel in the SVO.
    /// </summary>
    internal readonly struct SVOLink : IEquatable<SVOLink>
    {
        // Technically this is not an invalid pointer but in practice we will never have this data configuration.
        // This will imply that we are using 63 layers and it is completely impossible to rasterize at such a high resolution.
        public static readonly SVOLink Invalid = new(uint.MaxValue);

        /// <summary>
        /// The whole data is packed into this 32 bit integer with the following data representation:
        /// <list type="bullet">
        ///     <item> bit 31: link type | 1 bit | 0 for voxels and 1 for nodes </item>
        ///     <item> bits [30..6 ]: offset | 25 bits | values up to 33_554_431 </item>
        ///     <item> bits [ 5..0 ]: concrete data | 6 bits | values up to 63 </item>
        /// </list>
        /// The concrete data will represent:
        /// <list type="bullet">
        ///     <item> Layer of the node for nodes. </item>
        ///     <item> The specific subnode for voxels. </item>
        /// </list>
        /// See <see cref="SVOLeaf"/> for more details.
        /// </summary>
        readonly uint _link;

        #region Bit representation constants

        const uint _IS_NODE_MASK = 1u << 31;

        const int _OFFSET_MASK_SHIFT = 6;
        const uint _OFFSET_MASK = 0x1FFFFFF << _OFFSET_MASK_SHIFT;

        const uint _CONCRETE_DATA_MASK = 0x3F;

        public const int MAX_OFFSET_ALLOWED = (int)(_OFFSET_MASK >> _OFFSET_MASK_SHIFT);

        public const int MAX_LAYER_ALLOWED = (int)_CONCRETE_DATA_MASK;

        public const int MAX_SUBNODE_ALLOWED = (int)_CONCRETE_DATA_MASK;

        #endregion

        SVOLink(uint rawLink)
        {
            _link = rawLink;
        }

        /// <summary>
        /// Creates a link to a node in the SVO with the given layer and offset on the layer.
        /// </summary>
        public static SVOLink NodeLink(int layer, int nodeOffset) =>
            new(_IS_NODE_MASK | ((uint)nodeOffset << _OFFSET_MASK_SHIFT) | (uint)layer);

        /// <summary>
        /// Creates a link to a voxel in the SVO with the given offset of the leaf and subnode index.
        /// </summary>
        public static SVOLink VoxelLink(int leafOffset, int subnodeIndex) =>
            new(((uint)leafOffset << _OFFSET_MASK_SHIFT) | (uint)subnodeIndex);

        /// <summary>
        /// Returns the same as comparing equality with <see cref="SVOLink.Invalid"/>.
        /// </summary>
        public bool IsValid => _link != uint.MaxValue;

        public int Offset => (int)((_link & _OFFSET_MASK) >> _OFFSET_MASK_SHIFT);

        /// <summary>
        /// Returns true if this link points to a node and outputs its layer.
        /// If it returns false the value of layerIdx is undefined and SHOULD NOT be used.
        /// </summary>
        public bool IsNode(out int layerIdx)
        {
            layerIdx = (int)(_link & _CONCRETE_DATA_MASK);
            return (_link & _IS_NODE_MASK) != 0;
        }

        /// <summary>
        /// Returns true if this link points to a voxel and outputs its subnode index.
        /// If it returns false the value of subnodeIdx is undefined and SHOULD NOT be used.
        /// </summary>
        public bool IsVoxel(out int subnodeIdx)
        {
            subnodeIdx = (int)(_link & _CONCRETE_DATA_MASK);
            return (_link & _IS_NODE_MASK) == 0;
        }

        #region Operators and overrides

        public static bool operator ==(SVOLink lhs, SVOLink rhs) => lhs._link == rhs._link;

        public static bool operator !=(SVOLink lhs, SVOLink rhs) => lhs._link != rhs._link;

        // IEquatable<SVOLink> matters for performance: SVOLink is used as the key of the pathfinder's
        // hash collections, and without it Dictionary/HashSet fall back to the boxing
        // ObjectEqualityComparer in their hottest path.
        public bool Equals(SVOLink other) => _link == other._link;

        public override bool Equals(object obj) => obj is SVOLink other && this == other;

        public override int GetHashCode() => _link.GetHashCode();

        public static implicit operator uint(SVOLink link) => link._link;

        public static implicit operator SVOLink(uint rawLink) => new(rawLink);

        #endregion
    }
}
