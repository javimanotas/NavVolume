using System;
using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Solver for the ORCA linear program in 3D velocity space.
    /// </summary>
    /// <remarks>
    /// Port of the linear programming routines of the RVO2-3D library (Apache 2.0).
    /// </remarks>
    internal static class OrcaSolver
    {
        const float _EPSILON = 1e-5f;

        /// <summary>
        /// Solves the ORCA program for one agent.
        /// </summary>
        /// <param name="planes">
        /// Constraint planes, the first <paramref name="staticPlaneCount"/> entries must be the obstacle planes, followed by the agent-agent planes.
        /// </param>
        /// <param name="scratch">
        /// Caller-provided buffer of at least <paramref name="planeCount"/> entries used by the infeasible fallback.
        /// </param>
        public static float3 Solve(
            ReadOnlySpan<OrcaPlane> planes,
            int planeCount,
            int staticPlaneCount,
            float maxSpeed,
            float3 prefVelocity,
            Span<OrcaPlane> scratch
        )
        {
            var result = float3.zero;
            var failedPlane = SolveFeasible(
                planes,
                planeCount,
                maxSpeed,
                prefVelocity,
                false,
                ref result
            );

            if (failedPlane < planeCount)
            {
                SolveInfeasible(
                    planes,
                    planeCount,
                    staticPlaneCount,
                    failedPlane,
                    maxSpeed,
                    scratch,
                    ref result
                );
            }

            return result;
        }

        /// <summary>
        /// Seeks the optimal velocity satisfying every plane, fixing violated constraints one at a time (linearProgram3 in RVO2-3D).
        /// </summary>
        /// <returns>
        /// <paramref name="planeCount"/> on success, or the index of the first plane for which no feasible velocity exists.
        /// </returns>
        static int SolveFeasible(
            ReadOnlySpan<OrcaPlane> planes,
            int planeCount,
            float maxSpeed,
            float3 optVelocity,
            bool optimizeDirection,
            ref float3 result
        )
        {
            if (optimizeDirection)
            {
                result = optVelocity * maxSpeed;
            }
            else if (math.lengthsq(optVelocity) > maxSpeed * maxSpeed)
            {
                result = math.normalize(optVelocity) * maxSpeed;
            }
            else
            {
                result = optVelocity;
            }

            for (var i = 0; i < planeCount; i++)
            {
                if (math.dot(planes[i].Normal, planes[i].Point - result) > 0f)
                {
                    var previousResult = result;

                    if (
                        !SolveOnPlane(
                            planes,
                            i,
                            maxSpeed,
                            optVelocity,
                            optimizeDirection,
                            ref result
                        )
                    )
                    {
                        result = previousResult;
                        return i;
                    }
                }
            }

            return planeCount;
        }

        /// <summary>
        /// Optimizes within the disc that plane <paramref name="planeIndex"/> cuts out of the max-speed sphere, subject to all previous planes.
        /// </summary>
        static bool SolveOnPlane(
            ReadOnlySpan<OrcaPlane> planes,
            int planeIndex,
            float maxSpeed,
            float3 optVelocity,
            bool optimizeDirection,
            ref float3 result
        )
        {
            var plane = planes[planeIndex];
            var planeDist = math.dot(plane.Point, plane.Normal);
            var planeDistSq = planeDist * planeDist;
            var radiusSq = maxSpeed * maxSpeed;

            if (planeDistSq > radiusSq)
            {
                return false;
            }

            var planeRadiusSq = radiusSq - planeDistSq;
            var planeCenter = planeDist * plane.Normal;

            if (optimizeDirection)
            {
                var planeOptVelocity =
                    optVelocity - math.dot(optVelocity, plane.Normal) * plane.Normal;
                var planeOptVelocityLengthSq = math.lengthsq(planeOptVelocity);

                result =
                    planeOptVelocityLengthSq <= _EPSILON
                        ? planeCenter
                        : planeCenter
                            + math.sqrt(planeRadiusSq / planeOptVelocityLengthSq)
                                * planeOptVelocity;
            }
            else
            {
                result =
                    optVelocity + math.dot(plane.Point - optVelocity, plane.Normal) * plane.Normal;

                if (math.lengthsq(result) > radiusSq)
                {
                    var planeResult = result - planeCenter;
                    var planeResultLengthSq = math.lengthsq(planeResult);
                    result =
                        planeCenter + math.sqrt(planeRadiusSq / planeResultLengthSq) * planeResult;
                }
            }

            for (var i = 0; i < planeIndex; i++)
            {
                if (math.dot(planes[i].Normal, planes[i].Point - result) > 0f)
                {
                    var crossProduct = math.cross(planes[i].Normal, plane.Normal);

                    if (math.lengthsq(crossProduct) <= _EPSILON)
                    {
                        return false;
                    }

                    var lineDirection = math.normalize(crossProduct);
                    var lineNormal = math.cross(lineDirection, plane.Normal);
                    var linePoint =
                        plane.Point
                        + (
                            math.dot(planes[i].Point - plane.Point, planes[i].Normal)
                            / math.dot(lineNormal, planes[i].Normal)
                        ) * lineNormal;

                    if (
                        !SolveOnLine(
                            planes,
                            i,
                            linePoint,
                            lineDirection,
                            maxSpeed,
                            optVelocity,
                            optimizeDirection,
                            ref result
                        )
                    )
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Optimizes along the segment that the max-speed sphere cuts out of a line, subject to the first <paramref name="planeCount"/> planes (linearProgram1 in RVO2-3D).
        /// </summary>
        static bool SolveOnLine(
            ReadOnlySpan<OrcaPlane> planes,
            int planeCount,
            float3 linePoint,
            float3 lineDirection,
            float maxSpeed,
            float3 optVelocity,
            bool optimizeDirection,
            ref float3 result
        )
        {
            var dotProduct = math.dot(linePoint, lineDirection);
            var discriminant =
                dotProduct * dotProduct + maxSpeed * maxSpeed - math.lengthsq(linePoint);

            if (discriminant < 0f)
            {
                return false;
            }

            var sqrtDiscriminant = math.sqrt(discriminant);
            var tLeft = -dotProduct - sqrtDiscriminant;
            var tRight = -dotProduct + sqrtDiscriminant;

            for (var i = 0; i < planeCount; i++)
            {
                var numerator = math.dot(planes[i].Point - linePoint, planes[i].Normal);
                var denominator = math.dot(lineDirection, planes[i].Normal);

                if (denominator * denominator <= _EPSILON)
                {
                    if (numerator > 0f)
                    {
                        return false;
                    }

                    continue;
                }

                var t = numerator / denominator;

                if (denominator >= 0f)
                {
                    tLeft = math.max(tLeft, t);
                }
                else
                {
                    tRight = math.min(tRight, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (optimizeDirection)
            {
                result =
                    math.dot(optVelocity, lineDirection) > 0f
                        ? linePoint + tRight * lineDirection
                        : linePoint + tLeft * lineDirection;
            }
            else
            {
                var t = math.dot(lineDirection, optVelocity - linePoint);
                result = linePoint + math.clamp(t, tLeft, tRight) * lineDirection;
            }

            return true;
        }

        /// <summary>
        /// Finds the velocity that least violates the relaxable planes when the program is infeasible (linearProgram4 in RVO2-3D).
        /// </summary>
        static void SolveInfeasible(
            ReadOnlySpan<OrcaPlane> planes,
            int planeCount,
            int staticPlaneCount,
            int beginPlane,
            float maxSpeed,
            Span<OrcaPlane> scratch,
            ref float3 result
        )
        {
            var distance = 0f;

            for (var i = beginPlane; i < planeCount; i++)
            {
                if (math.dot(planes[i].Normal, planes[i].Point - result) <= distance)
                {
                    continue;
                }

                var scratchCount = 0;
                var staticCopyCount = math.min(staticPlaneCount, i);

                for (var j = 0; j < staticCopyCount; j++)
                {
                    scratch[scratchCount++] = planes[j];
                }

                for (var j = staticPlaneCount; j < i; j++)
                {
                    OrcaPlane plane;
                    var crossProduct = math.cross(planes[j].Normal, planes[i].Normal);

                    if (math.lengthsq(crossProduct) <= _EPSILON)
                    {
                        if (math.dot(planes[i].Normal, planes[j].Normal) > 0f)
                        {
                            continue;
                        }

                        plane.Point = 0.5f * (planes[i].Point + planes[j].Point);
                    }
                    else
                    {
                        var lineNormal = math.cross(crossProduct, planes[i].Normal);
                        plane.Point =
                            planes[i].Point
                            + (
                                math.dot(planes[j].Point - planes[i].Point, planes[j].Normal)
                                / math.dot(lineNormal, planes[j].Normal)
                            ) * lineNormal;
                    }

                    plane.Normal = math.normalize(planes[i].Normal - planes[j].Normal);
                    scratch[scratchCount++] = plane;
                }

                var previousResult = result;

                if (
                    SolveFeasible(
                        scratch,
                        scratchCount,
                        maxSpeed,
                        planes[i].Normal,
                        true,
                        ref result
                    ) < scratchCount
                )
                {
                    // This should never happen
                    result = previousResult;
                }

                distance = math.dot(planes[i].Normal, planes[i].Point - result);
            }
        }
    }
}
