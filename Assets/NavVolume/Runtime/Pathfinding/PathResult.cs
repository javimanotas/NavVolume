using System.Collections.Generic;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Output of a completed (or failed) A* query.
    /// </summary>
    internal readonly struct PathResult
    {
        public static PathResult Success(List<Vector3> waypoints, PathStats stats) =>
            new(waypoints, null, stats);

        public static PathResult Success(
            List<Vector3> waypoints,
            List<Vector3> rawWaypoints,
            PathStats stats
        ) => new(waypoints, rawWaypoints, stats);

        public static PathResult Failure(PathResultStatus status) => new(status, default);

        public static PathResult Failure(PathResultStatus status, PathStats stats) =>
            new(status, stats);

        public readonly PathResultStatus Status;

        public readonly List<Vector3> Waypoints;

        public readonly List<Vector3> RawWaypoints;

        public readonly PathStats Stats;

        public bool Succeeded => Status == PathResultStatus.Sucess;

        PathResult(PathResultStatus rawStatus, PathStats stats)
        {
            Status = rawStatus;
            Waypoints = null;
            RawWaypoints = null;
            Stats = stats;
        }

        PathResult(List<Vector3> waypoints, List<Vector3> rawWaypoints, PathStats stats)
        {
            Status = PathResultStatus.Sucess;
            Waypoints = waypoints;
            RawWaypoints = rawWaypoints;
            Stats = stats;
        }
    }
}
