namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Pointer to an arbitrary node or subnode in the SVO.
    /// </summary>
    internal readonly struct SVOLink
    {
        // Technically every node in a layer different to layer 0 with a subnode index set to a value will be invalid.
        // This is just the easiest representation.
        public static readonly SVOLink Invalid = new(uint.MaxValue);

        /// <summary>
        /// The whole data is packed into this 32 bit integer with the following data representation:
        /// <list type="bullet">
        ///     <item> [31..28]: layer index   |  4 bits | range: [0, 15] </item>
        ///     <item> [27..6 ]: node index    | 22 bits | range: [0, 4_194_303] </item>
        ///     <item> [ 5..0 ]: subnode index |  6 bits | range: [0, 63]        (only if its a leaf) </item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// See <see cref="SVOLeaf"/> for more information about subnodes.
        /// </remarks>
        readonly uint _link;

        #region Bit representation constants

        const int _LAYER_SHIFT = 28;
        const uint _UNSHIFTED_LAYER_MASK = 0xF;
        const uint _LAYER_MASK = _UNSHIFTED_LAYER_MASK << _LAYER_SHIFT;

        const int _NODE_SHIFT = 6;
        const uint _UNSHIFTED_NODE_MASK = 0x3FFFFF;
        const uint _NODE_MASK = _UNSHIFTED_NODE_MASK << _NODE_SHIFT;

        const uint _UNSHIFTED_SUBNODE_MASK = 0x3F;

        #endregion

        SVOLink(uint rawLink)
        {
            _link = rawLink;
        }

        public SVOLink(uint layer, uint nodeIndex, uint subnode = 0)
        {
            // TODO: add defensive assertions to check that values are in range

            _link =
                ((layer & _UNSHIFTED_LAYER_MASK) << _LAYER_SHIFT)
                | ((nodeIndex & _UNSHIFTED_NODE_MASK) << _NODE_SHIFT)
                | (subnode & _UNSHIFTED_SUBNODE_MASK);
        }

        public readonly bool IsValid => _link != uint.MaxValue;

        public readonly uint LayerIdx => (_link & _LAYER_MASK) >> _LAYER_SHIFT;

        public readonly uint NodeIdx => (_link & _NODE_MASK) >> _NODE_SHIFT;

        public readonly uint SubnodeIdx => _link & _UNSHIFTED_SUBNODE_MASK;

        public const uint MAX_LAYER_ALLOWED = _UNSHIFTED_LAYER_MASK;

        public const uint MAX_NODE_ALLOWED = _UNSHIFTED_NODE_MASK;

        public const uint MAX_SUBNODE_ALLOWED = _UNSHIFTED_SUBNODE_MASK;

        // TODO: consider if l0 nodes should have a different layer than leaf nodes
        // right now is impossible to distinguish between a leaf node and a layer 0 node, but maybe it would be useful to be able to do so.

        public SVOLink WithSubnode(uint subnode) =>
            new(_link & ~_UNSHIFTED_SUBNODE_MASK | subnode & _UNSHIFTED_SUBNODE_MASK);

        public SVOLink WithoutSubnode() => new(_link & ~_UNSHIFTED_SUBNODE_MASK);

        /// <summary>
        /// Returns true if both links point to the same leaf node, regardless of their subnode index.
        /// </summary>
        public bool SameLeafNode(SVOLink other) =>
            LayerIdx == 0 && other.LayerIdx == 0 && NodeIdx == other.NodeIdx;

        #region Operators and overrides

        public static bool operator ==(SVOLink lhs, SVOLink rhs) => lhs._link == rhs._link;

        public static bool operator !=(SVOLink lhs, SVOLink rhs) => lhs._link != rhs._link;

        public override bool Equals(object obj) => this == (SVOLink)obj;

        public override int GetHashCode() => _link.GetHashCode();

        public static implicit operator uint(SVOLink link) => link._link;

        public static implicit operator SVOLink(uint rawLink) => new(rawLink);

        #endregion
    }
}
