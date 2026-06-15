using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Builds ORCA half-space constraints out of velocity obstacles.
    /// </summary>
    /// <remarks>
    /// Follows the 3D formulation of "Reciprocal n-Body Collision Avoidance" (the RVO2-3D library, Apache 2.0).
    /// </remarks>
    internal static class OrcaMath
    {
        /// <summary>
        /// Length below which a direction is considered degenerate and replaced by a deterministic fallback.
        /// </summary>
        const float _MIN_DIRECTION_LENGTH = 1e-6f;

        /// <summary>
        /// Builds the ORCA plane induced by a neighboring agent.
        /// </summary>
        public static OrcaPlane AgentPlane(
            float3 position,
            float3 velocity,
            float radius,
            float3 otherPosition,
            float3 otherVelocity,
            float otherRadius,
            float timeHorizon,
            float timeStep
        )
        {
            var u = ComputeU(
                otherPosition - position,
                velocity - otherVelocity,
                radius + otherRadius,
                timeHorizon,
                timeStep,
                out var normal
            );

            return new OrcaPlane { Normal = normal, Point = velocity + 0.5f * u };
        }

        /// <summary>
        /// Builds the ORCA plane induced by a static or scripted obstacle.
        /// </summary>
        public static OrcaPlane StaticPlane(
            float3 relativePosition,
            float combinedRadius,
            float3 velocity,
            float3 obstacleVelocity,
            float timeHorizon,
            float timeStep
        )
        {
            var u = ComputeU(
                relativePosition,
                velocity - obstacleVelocity,
                combinedRadius,
                timeHorizon,
                timeStep,
                out var normal
            );

            return new OrcaPlane { Normal = normal, Point = velocity + u };
        }

        /// <summary>
        /// Computes the smallest change u that takes the relative velocity out of the velocity obstacle induced by a sphere of <paramref name="combinedRadius"/> centered at <paramref name="relativePosition"/>, truncated at <paramref name="timeHorizon"/>.
        /// </summary>
        static float3 ComputeU(
            float3 relativePosition,
            float3 relativeVelocity,
            float combinedRadius,
            float timeHorizon,
            float timeStep,
            out float3 normal
        )
        {
            var distSq = math.lengthsq(relativePosition);
            var combinedRadiusSq = combinedRadius * combinedRadius;

            if (distSq > combinedRadiusSq)
            {
                var invTimeHorizon = 1f / timeHorizon;
                var w = relativeVelocity - invTimeHorizon * relativePosition;
                var wLengthSq = math.lengthsq(w);
                var dotProduct = math.dot(w, relativePosition);

                if (dotProduct < 0f && dotProduct * dotProduct > combinedRadiusSq * wLengthSq)
                {
                    var wLength = math.sqrt(wLengthSq);
                    var unitW = SafeDirection(w, wLength, relativePosition);

                    normal = unitW;
                    return (combinedRadius * invTimeHorizon - wLength) * unitW;
                }

                var a = distSq;
                var b = math.dot(relativePosition, relativeVelocity);
                var crossProduct = math.cross(relativePosition, relativeVelocity);
                var c =
                    math.lengthsq(relativeVelocity)
                    - math.lengthsq(crossProduct) / (distSq - combinedRadiusSq);
                var t = (b + math.sqrt(math.max(0f, b * b - a * c))) / a;
                var ww = relativeVelocity - t * relativePosition;
                var wwLength = math.length(ww);
                var unitWW = SafeDirection(ww, wwLength, relativePosition);

                normal = unitWW;
                return (combinedRadius * t - wwLength) * unitWW;
            }

            var invTimeStep = 1f / timeStep;
            var wc = relativeVelocity - invTimeStep * relativePosition;
            var wcLength = math.length(wc);
            var unitWc = SafeDirection(wc, wcLength, relativePosition);

            normal = unitWc;
            return (combinedRadius * invTimeStep - wcLength) * unitWc;
        }

        /// <summary>
        /// Normalizes <paramref name="direction"/>, falling back to a deterministic perpendicular of <paramref name="relativePosition"/> when it is degenerate.
        /// </summary>
        static float3 SafeDirection(float3 direction, float length, float3 relativePosition) =>
            length > _MIN_DIRECTION_LENGTH
                ? direction / length
                : AnyPerpendicular(relativePosition);

        static float3 AnyPerpendicular(float3 v)
        {
            var p =
                math.abs(v.x) > math.abs(v.z)
                    ? new float3(-v.y, v.x, 0f)
                    : new float3(0f, -v.z, v.y);
            var lengthSq = math.lengthsq(p);

            return lengthSq > _MIN_DIRECTION_LENGTH * _MIN_DIRECTION_LENGTH
                ? p * math.rsqrt(lengthSq)
                : new float3(0f, 1f, 0f);
        }
    }
}
