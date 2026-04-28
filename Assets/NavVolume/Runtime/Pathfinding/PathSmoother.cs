using System.Collections.Generic;
using NavVolume.Runtime.Builder;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Post-processing smoother for paths.
    /// </summary>
    internal static class PathSmoother
    {
        const int _CATMULL_RESOLUTION = 8;

        /// <summary>
        /// Returns a new waypoint list with all redundant intermediate points removed.
        /// </summary>
        public static List<Vector3> GreedyShortcut(List<Vector3> input, in NavContext ctx)
        {
            var result = new List<Vector3>(input.Count) { input[0] };

            for (var i = 2; i < input.Count; i++)
            {
                if (!SVORaycast.HasLineOfSight(ctx, result[^1], input[i]))
                {
                    result.Add(input[i - 1]);
                }
            }

            result.Add(input[^1]);
            return result;
        }

        /// <summary>
        /// Fits a Catmull-Rom spline through every waypoint and samples it at a fixed resolution.
        /// </summary>
        /// <remarks>
        /// The spline interpolates every input point, so all hard obstacle-clearance corners are respected.
        /// </remarks>
        public static List<Vector3> CatmullRomSpline(List<Vector3> waypoints)
        {
            var result = new List<Vector3>(waypoints.Count * _CATMULL_RESOLUTION);

            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                var p0 = waypoints[Mathf.Max(i - 1, 0)];
                var p1 = waypoints[i];
                var p2 = waypoints[i + 1];
                var p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)];

                for (var j = 0; j < _CATMULL_RESOLUTION; j++)
                {
                    var t = j / (float)_CATMULL_RESOLUTION;
                    result.Add(EvaluateCatmullRom(p0, p1, p2, p3, t));
                }
            }

            result.Add(waypoints[^1]);
            return result;
        }

        static Vector3 EvaluateCatmullRom(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t
        ) =>
            0.5f
            * (
                2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (t * t)
                + (-p0 + 3f * p1 - 3f * p2 + p3) * (t * t * t)
            );
    }
}
