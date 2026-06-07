using System;
using UnityEngine.Assertions;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Encoding of a 3D grid position into a Z-order space filling curve.
    /// </summary>
    internal readonly struct MortonCode : IComparable<MortonCode>, IEquatable<MortonCode>
    {
        public static MortonCode Invalid => new(uint.MaxValue);

        /// <summary>
        /// Since the encoding uses 32-bit integers, we can only encode a grid of up to 1024 (2^10) resolution per axis.
        /// That implies that the maximum number of layers of the octree is 11 (since the root node is at depth 0).
        /// </summary>
        readonly uint _code;

        #region Helper functions

        /// <summary>
        /// Interleaves 2 zeros between each bit.
        /// </summary>
        static uint Interleave00(uint x)
        {
            x = (x ^ (x << 16)) & 0xFF0000FF;
            x = (x ^ (x << 8)) & 0x0300F00F;
            x = (x ^ (x << 4)) & 0x030C30C3;
            x = (x ^ (x << 2)) & 0x09249249;
            return x;
        }

        /// <summary>
        /// Inverse function to <see cref="Interleave00"/>
        /// </summary>
        static uint CompactBits(uint x)
        {
            x &= 0x09249249;
            x = (x ^ (x >> 2)) & 0x030C30C3;
            x = (x ^ (x >> 4)) & 0x0300F00F;
            x = (x ^ (x >> 8)) & 0xFF0000FF;
            x = (x ^ (x >> 16)) & 0x000003FF;
            return x;
        }

        #endregion

        MortonCode(uint rawCode)
        {
            _code = rawCode;
        }

        public MortonCode(int x, int y, int z)
        {
            _code =
                Interleave00((uint)x) | (Interleave00((uint)y) << 1) | (Interleave00((uint)z) << 2);
        }

        public (int, int, int) Decoded =>
            ((int)CompactBits(_code), (int)CompactBits(_code >> 1), (int)CompactBits(_code >> 2));

        public MortonCode ParentCode
        {
            get
            {
                var (x, y, z) = Decoded;
                return new(x >> 1, y >> 1, z >> 1);
            }
        }

        public MortonCode ChildCode(int childIdx)
        {
            var (x, y, z) = Decoded;
            return new(
                (x << 1) | (childIdx & 1),
                (y << 1) | ((childIdx >> 1) & 1),
                (z << 1) | ((childIdx >> 2) & 1)
            );
        }

        /// <summary>
        /// Compute the Morton code of the node adjacent in the given direction.
        /// </summary>
        /// <returns>
        /// false when the neighbor would be outside the grid boundary
        /// </returns>
        public bool TryGetNeighborCode(
            NeighborDirection dir,
            int gridResolution,
            out MortonCode neighborCode
        )
        {
            var (x, y, z) = Decoded;

            switch (dir)
            {
                case NeighborDirection.PosX:
                    x++;
                    break;
                case NeighborDirection.NegX:
                    if (x == 0)
                    {
                        goto Invalid;
                    }
                    x--;
                    break;
                case NeighborDirection.PosY:
                    y++;
                    break;
                case NeighborDirection.NegY:
                    if (y == 0)
                    {
                        goto Invalid;
                    }
                    y--;
                    break;
                case NeighborDirection.PosZ:
                    z++;
                    break;
                case NeighborDirection.NegZ:
                    if (z == 0)
                    {
                        goto Invalid;
                    }
                    z--;
                    break;
            }

            if (x >= gridResolution || y >= gridResolution || z >= gridResolution)
            {
                goto Invalid;
            }

            neighborCode = new(x, y, z);
            return true;

            Invalid:
            neighborCode = Invalid;
            return false;
        }

        #region Operators and overrides

        public static bool operator ==(MortonCode lhs, MortonCode rhs) => lhs._code == rhs._code;

        public static bool operator !=(MortonCode lhs, MortonCode rhs) => lhs._code != rhs._code;

        public bool Equals(MortonCode other) => _code == other._code;

        public override bool Equals(object obj) => obj is MortonCode other && Equals(other);

        public override int GetHashCode() => _code.GetHashCode();

        public int CompareTo(MortonCode other) => _code.CompareTo(other._code);

        public static implicit operator uint(MortonCode code) => code._code;

        public static implicit operator MortonCode(uint rawCode) => new(rawCode);

        #endregion
    }
}
