using System.Runtime.InteropServices;

namespace NavVolume
{
    /// <summary>
    /// Helper methods for calculating the hash that are not dependant on a concrete hash algorithm.
    /// </summary>
    internal static class HashHelper
    {
        /// <summary>
        /// Union trick used to reinterpret a float's bits as an int.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FloatIntUnion
        {
            [FieldOffset(0)]
            public float Float;

            [FieldOffset(0)]
            public int Int;
        }

        /// <summary>
        /// Returns the int whose bit representation matches the input float's.
        /// </summary>
        public static int FloatBitsAsInt(float value)
        {
            var union = new FloatIntUnion() { Float = value };
            return union.Int;
        }
    }
}
