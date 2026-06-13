using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Half-space constraint in velocity space produced by ORCA.
    /// A velocity v is permitted when <c>dot(Normal, v - Point) &gt;= 0</c>.
    /// </summary>
    internal struct OrcaPlane
    {
        /// <summary>
        /// A point on the boundary plane.
        /// </summary>
        public float3 Point;

        /// <summary>
        /// Unit normal pointing into the permitted half-space.
        /// </summary>
        public float3 Normal;
    }
}
