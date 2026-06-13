using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// World-space snapshot of one <see cref="NavVolumeObstacle"/>, refreshed by the simulation
    /// every step.
    /// </summary>
    internal struct AvoidanceObstacleState
    {
        public float3 Position;

        /// <summary>
        /// World rotation; only meaningful for boxes.
        /// </summary>
        public quaternion Rotation;

        /// <summary>
        /// Box: world half extents. Sphere: radius stored in X.
        /// </summary>
        public float3 HalfExtents;

        /// <summary>
        /// Velocity derived from successive positions, so agents dodge moving obstacles predictively.
        /// </summary>
        public float3 Velocity;

        /// <summary>
        /// Radius of the bounding sphere, used to reject far obstacles cheaply.
        /// </summary>
        public float BoundingRadius;

        public ObstacleShape Shape;
    }
}
