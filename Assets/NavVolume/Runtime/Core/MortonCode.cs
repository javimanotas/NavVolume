namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Encoding of a 3D grid position into a Z-order space filling curve.
    /// </summary>
    internal readonly struct MortonCode
    {
        readonly uint _code;

        #region Helper functions

        /// <summary>
        /// Interleaves 2 zeros between each bit.
        /// </summary>
        static uint Interleave00(uint x)
        {
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        /// <summary>
        /// Inverse function to <see cref="Interleave00"/>
        /// </summary>
        static uint CompactBits(uint x)
        {
            x = (x | (x >> 2)) & 0x030C30C3;
            x = (x | (x >> 4)) & 0x0300F00F;
            x = (x | (x >> 8)) & 0x030000FF;
            x = (x | (x >> 16)) & 0x000003FF;
            return x;
        }

        #endregion

        /// <summary>
        /// Creates the encoding of the given grid position.
        /// </summary>
        public MortonCode(uint x, uint y, uint z)
        {
            _code = Interleave00(x) | (Interleave00(y) << 1) | (Interleave00(z) << 2);
        }

        public (uint, uint, uint) Decoded =>
            (CompactBits(_code), CompactBits(_code >> 1), CompactBits(_code >> 2));

        public MortonCode ParentCode
        {
            get
            {
                var (x, y, z) = Decoded;
                return new(x >> 1, y >> 1, z >> 1);
            }
        }

        public MortonCode ChildCode(uint childIdx)
        {
            var (x, y, z) = Decoded;
            return new(
                (x << 1) | (childIdx & 1),
                (y << 1) | ((childIdx >> 1) & 1),
                (z << 1) | ((childIdx >> 2) & 1)
            );
        }
    }
}
