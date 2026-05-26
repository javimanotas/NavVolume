using System.Collections.Generic;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Output of a completed (or failed) A* query.
    /// </summary>
    internal readonly struct PathResult
    {
        public static PathResult Success(List<Vector3> waypoints) => new(waypoints, null);

        public static PathResult Success(List<Vector3> waypoints, List<Vector3> rawWaypoints) =>
            new(waypoints, rawWaypoints);

        public static PathResult Failure(PathResultStatus status) => new(status);

        public readonly PathResultStatus Status;

        public readonly List<Vector3> Waypoints;

        public readonly List<Vector3> RawWaypoints;

        public bool Succeeded => Status == PathResultStatus.Sucess;

        // TODO: implement stats

        PathResult(PathResultStatus rawStatus)
        {
            Status = rawStatus;
            Waypoints = null;
            RawWaypoints = null;
        }

        PathResult(List<Vector3> waypoints, List<Vector3> rawWaypoints)
        {
            Status = PathResultStatus.Sucess;
            Waypoints = waypoints;
            RawWaypoints = rawWaypoints;
        }
    }
}
