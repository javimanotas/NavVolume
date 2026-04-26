using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// Provides an implementation of the FNV-1a hash algorithm.
    /// </summary>
    /// <remarks>
    /// The hash value can be retrieved as a 64-bit unsigned integer via the implicit conversion operator.
    /// </remarks>
    internal class FNV1a
    {
        const ulong _OFFSET_BASIS = 0xcbf29ce484222325;
        const ulong _PRIME = 0x00000100000001b3;

        ulong _hash = _OFFSET_BASIS;

        #region Datatypes hashing

        public void Feed(byte b)
        {
            unchecked
            {
                _hash ^= b;
                _hash *= _PRIME;
            }
        }

        public void Feed(bool b)
        {
            Feed(b ? (byte)1 : (byte)0);
        }

        public void Feed(int i)
        {
            Feed((byte)(i & 0xff));
            Feed((byte)((i >> 8) & 0xff));
            Feed((byte)((i >> 16) & 0xff));
            Feed((byte)((i >> 24) & 0xff));
        }

        public void Feed(float f)
        {
            Feed(HashHelper.FloatBitsAsInt(f));
        }

        public void Feed(Vector3 v)
        {
            Feed(v.x);
            Feed(v.y);
            Feed(v.z);
        }

        #endregion

        #region Operators and overrides

        public static bool operator ==(FNV1a lhs, FNV1a rhs) => lhs._hash == rhs._hash;

        public static bool operator !=(FNV1a lhs, FNV1a rhs) => lhs._hash != rhs._hash;

        public override bool Equals(object obj) => this == (FNV1a)obj;

        public override int GetHashCode() => _hash.GetHashCode();

        public static implicit operator ulong(FNV1a hasher) => hasher._hash;

        #endregion
    }
}
