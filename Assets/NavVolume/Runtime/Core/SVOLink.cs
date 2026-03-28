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

        // Bit data representation:
        //   [31..28] -> layer index   |  4 bits | range: [0, 15]
        //   [27..6 ] -> node index    | 22 bits | range: [0, 4_194_303]
        //   [ 5..0 ] -> subnode index |  6 bits | range: [0, 63]        | only if its a leaf
        /// <summary>
        /// The whole data is packed into this 32 bit integer.
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
            _link =
                ((layer & _UNSHIFTED_LAYER_MASK) << _LAYER_SHIFT)
                | ((nodeIndex & _UNSHIFTED_NODE_MASK) << _NODE_SHIFT)
                | (subnode & _UNSHIFTED_SUBNODE_MASK);
        }

        public readonly bool IsValid => _link != uint.MaxValue;

        public readonly uint LayerIdx => (_link & _LAYER_MASK) >> _LAYER_SHIFT;

        public readonly uint NodeIdx => (_link & _NODE_MASK) >> _NODE_SHIFT;

        public readonly uint SubnodeIdx => _link & _UNSHIFTED_SUBNODE_MASK;
    }
}
